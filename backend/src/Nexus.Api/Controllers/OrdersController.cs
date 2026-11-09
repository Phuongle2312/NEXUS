using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

public sealed record FeasibilityRequest(string OrderId, bool Feasible, decimal? DistanceKm,
    bool? ServerAvailable, bool? LandlineFeasible, bool? InternetFeasible, string? Reason);

public sealed record CreateConnectionRequest(string OrderId, DateOnly ActivationDate,
    int? EquipmentProductId, string? EquipmentSerial);

public sealed record ConnectionStatusRequest(string AccountId, string Status, string? Reason);

public sealed record ReplaceEquipmentRequest(string AccountId, string OldSerial, int NewProductId,
    decimal ReplacementFee);

/// <summary>Đơn đăng ký: tạo đơn (cửa hàng), danh sách, chi tiết (FR-04).</summary>
[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orders, NexusDbContext db) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Retail);
        var order = await orders.CreateAsync(Current, request, ct);
        return OkResponse(new
        {
            order.OrderId, order.ConnType, order.Quantity, order.DiscountPercent,
            order.PlanFee, order.DepositAmount, order.Status
        }, "Đã tạo đơn.");
    }

    /// <summary>Danh sách đơn, lọc theo trạng thái, loại và khoảng ngày.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? type,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1,
        [FromQuery] int size = 20, CancellationToken ct = default)
    {
        RequireRoles(Roles.Retail, Roles.Tech, Roles.Admin, Roles.Accounts);

        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var query = db.Orders.AsNoTracking()
            .Include(o => o.Customer).Include(o => o.Plan).Include(o => o.Shop)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status.ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(o => o.ConnType == type.ToUpper());
        }

        if (from is not null)
        {
            query = query.Where(o => o.OrderDate >= from.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (to is not null)
        {
            query = query.Where(o => o.OrderDate < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * size).Take(size)
            .Select(o => new
            {
                o.OrderId,
                CustomerName = o.Customer!.FullName,
                o.ConnType,
                PlanName = o.Plan!.PlanName,
                ShopName = o.Shop!.ShopName,
                o.Quantity,
                o.ConnectedQty,
                o.DiscountPercent,
                o.OrderDate,
                o.Status
            })
            .ToListAsync(ct);

        return OkResponse(new PagedResult<object>(items.Cast<object>().ToList(), page, size, total));
    }

    /// <summary>Chi tiết đơn kèm lịch sử trạng thái; khách chỉ xem đơn của mình.</summary>
    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(string orderId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Customer).Include(o => o.Plan).Include(o => o.Shop)
            .Include(o => o.StatusHistory)
            .Include(o => o.Connections)
            .FirstOrDefaultAsync(o => o.OrderId == orderId.ToUpper(), ct)
            ?? throw new BusinessException(ErrorCode.ORDER_NOT_FOUND, $"Không tìm thấy đơn {orderId}.");

        if (Current.IsCustomer && order.CustomerId != Current.CustomerId)
        {
            throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy đơn của bạn.");
        }

        return OkResponse(new
        {
            order.OrderId,
            CustomerName = order.Customer!.FullName,
            CustomerId = order.CustomerId,
            order.ConnType,
            PlanName = order.Plan!.PlanName,
            PlanFee = order.PlanFee,
            DepositAmount = order.DepositAmount,
            ShopName = order.Shop!.ShopName,
            order.Quantity,
            order.ConnectedQty,
            order.DiscountPercent,
            order.OrderDate,
            order.Status,
            order.DistanceKm,
            order.ServerAvailable,
            order.LandlineFeasible,
            order.InternetFeasible,
            order.FeasibilityReason,
            History = order.StatusHistory.OrderBy(h => h.ChangedAt)
                .Select(h => new { h.Status, h.ChangedAt, h.Note }).ToList(),
            Connections = order.Connections
                .Select(c => new { c.AccountId, c.ConnType, c.Status, c.ActivationDate }).ToList()
        });
    }
}

/// <summary>Khu vực kỹ thuật: khảo sát khả thi, cấp kết nối, đổi trạng thái, thay thiết bị (FR-05..08).</summary>
[ApiController]
[Route("api/tech")]
public class TechController(OrderService orders, NexusDbContext db) : ApiControllerBase
{
    [HttpPost("start-check")]
    public async Task<IActionResult> StartCheck([FromBody] OrderIdRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Tech);
        await orders.StartFeasibilityCheckAsync(Current, request.OrderId, ct);
        return OkResponse(true, "Đã nhận đơn để khảo sát.");
    }

    [HttpPost("feasibility")]
    public async Task<IActionResult> Feasibility([FromBody] FeasibilityRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Tech);
        await orders.RecordFeasibilityAsync(Current, request.OrderId, request.Feasible, request.DistanceKm,
            request.ServerAvailable, request.LandlineFeasible, request.InternetFeasible, request.Reason, ct);
        return OkResponse(true, request.Feasible ? "Đơn khả thi." : "Đã ghi nhận đơn không khả thi.");
    }

    [HttpPost("connections")]
    public async Task<IActionResult> CreateConnection([FromBody] CreateConnectionRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Tech);
        var connection = await orders.CreateConnectionAsync(Current, request.OrderId, request.ActivationDate,
            request.EquipmentProductId, request.EquipmentSerial, ct);
        return OkResponse(new
        {
            connection.AccountId,
            connection.ConnType,
            connection.Status,
            connection.ActivationDate,
            connection.LandlineAccountId
        }, "Đã cấp kết nối.");
    }

    /// <summary>Danh sách kết nối, gồm các đề nghị tạm ngưng của kế toán.</summary>
    [HttpGet("connections")]
    public async Task<IActionResult> Connections([FromQuery] string? status, [FromQuery] string? type,
        [FromQuery] bool? suspendRequested, CancellationToken ct)
    {
        RequireRoles(Roles.Tech, Roles.Admin, Roles.Accounts, Roles.Retail);

        var query = db.Connections.AsNoTracking()
            .Include(c => c.Customer).Include(c => c.Plan).Include(c => c.City)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status.ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(c => c.ConnType == type.ToUpper());
        }

        if (suspendRequested is true)
        {
            query = query.Where(c => c.SuspendRequested);
        }

        var items = await query.OrderBy(c => c.AccountId).Take(500)
            .Select(c => new
            {
                c.AccountId,
                CustomerName = c.Customer!.FullName,
                c.ConnType,
                PlanName = c.Plan!.PlanName,
                CityName = c.City!.CityName,
                c.Status,
                c.ActivationDate,
                c.LandlineAccountId,
                c.SuspendRequested
            })
            .ToListAsync(ct);

        return OkResponse(items);
    }

    /// <summary>Chi tiết một kết nối kèm thiết bị và hóa đơn.</summary>
    [HttpGet("connections/{accountId}")]
    public async Task<IActionResult> Connection(string accountId, CancellationToken ct)
    {
        var connection = await db.Connections.AsNoTracking()
            .Include(c => c.Customer).Include(c => c.Plan).Include(c => c.City)
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.AccountId == accountId.ToUpper(), ct)
            ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, $"Không tìm thấy kết nối {accountId}.");

        if (Current.IsCustomer && connection.CustomerId != Current.CustomerId)
        {
            throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy kết nối của bạn.");
        }

        var bills = await db.Bills.AsNoTracking()
            .Where(b => b.AccountId == connection.AccountId)
            .OrderByDescending(b => b.BillDate)
            .Select(b => new { b.BillId, b.PeriodStart, b.PeriodEnd, b.TotalAmount, b.DueAmount, b.Status })
            .ToListAsync(ct);

        return OkResponse(new
        {
            connection.AccountId,
            CustomerName = connection.Customer!.FullName,
            connection.ConnType,
            PlanName = connection.Plan!.PlanName,
            CityName = connection.City!.CityName,
            connection.Status,
            connection.ActivationDate,
            connection.LandlineAccountId,
            connection.DepositRefundAmount,
            connection.SuspendRequested,
            Equipment = connection.Equipment
                .Select(e => new { e.SerialNo, e.ProductId, e.IssuedDate, e.Status }).ToList(),
            Bills = bills
        });
    }

    [HttpPost("connections/status")]
    public async Task<IActionResult> ChangeStatus([FromBody] ConnectionStatusRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Tech);
        await orders.ChangeConnectionStatusAsync(Current, request.AccountId, request.Status.ToUpper(),
            request.Reason, ct);
        return OkResponse(true, "Đã đổi trạng thái kết nối.");
    }

    [HttpPost("equipment/replace")]
    public async Task<IActionResult> ReplaceEquipment([FromBody] ReplaceEquipmentRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Tech);
        await orders.ReplaceEquipmentAsync(Current, request.AccountId, request.OldSerial,
            request.NewProductId, request.ReplacementFee, ct);
        return OkResponse(true, "Đã thay thiết bị và ghi phí.");
    }

    /// <summary>Thiết bị trong kho để chọn khi cấp phát.</summary>
    [HttpGet("equipment")]
    public async Task<IActionResult> Equipment(CancellationToken ct)
    {
        RequireRoles(Roles.Tech, Roles.Admin);
        var items = await db.Products.AsNoTracking()
            .Where(p => p.Status == "ACTIVE")
            .OrderBy(p => p.ProductName)
            .Select(p => new { p.ProductId, p.ProductName, p.ProductType, p.Price, p.StockQty })
            .ToListAsync(ct);
        return OkResponse(items);
    }
}

public sealed record OrderIdRequest(string OrderId);