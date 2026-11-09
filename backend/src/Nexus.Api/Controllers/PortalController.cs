using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

public sealed record UpdateProfileRequest(string Phone, string? Email, string Address);
public sealed record OrderRequestRequest(string ConnType, int PlanId, int ShopId, string? Note);
public sealed record FeedbackRequest(string RefType, string RefId, byte? Rating, string Comment);

/// <summary>
/// Cổng khách hàng (FR-11). Mọi truy vấn lọc theo customerId lấy từ token,
/// không bao giờ nhận customerId từ client.
/// </summary>
[ApiController]
[Route("api/my")]
public class PortalController(PortalService portal) : ApiControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        return OkResponse(await portal.ProfileAsync(Current, ct));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        var customer = await portal.UpdateProfileAsync(Current, request.Phone, request.Email, request.Address, ct);
        return OkResponse(new { customer.CustomerId, customer.FullName, customer.Phone, customer.Email, customer.Address },
            "Đã cập nhật hồ sơ.");
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders(CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        return OkResponse(await portal.MyOrdersAsync(Current, ct));
    }

    [HttpGet("connections")]
    public async Task<IActionResult> Connections(CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        return OkResponse(await portal.MyConnectionsAsync(Current, ct));
    }

    [HttpGet("bills")]
    public async Task<IActionResult> Bills(CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        return OkResponse(await portal.MyBillsAsync(Current, ct));
    }

    [HttpGet("order-requests")]
    public async Task<IActionResult> OrderRequests(CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        return OkResponse(await portal.MyOrderRequestsAsync(Current, ct));
    }

    /// <summary>Gửi yêu cầu đặt kết nối, chờ cửa hàng xác nhận thành đơn chính thức.</summary>
    [HttpPost("order-requests")]
    public async Task<IActionResult> CreateOrderRequest([FromBody] OrderRequestRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        var created = await portal.CreateOrderRequestAsync(Current, request.ConnType.ToUpper(),
            request.PlanId, request.ShopId, request.Note, ct);
        return OkResponse(new { created.RequestId, created.Status, created.CreatedAt },
            "Đã gửi yêu cầu, cửa hàng sẽ liên hệ xác nhận.");
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> Feedback([FromBody] FeedbackRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Customer);
        var feedback = await portal.CreateFeedbackAsync(Current, request.RefType, request.RefId,
            request.Rating, request.Comment, ct);
        return OkResponse(new { feedback.FeedbackId, feedback.CreatedAt },
            "Cảm ơn bạn đã gửi phản hồi.");
    }
}

/// <summary>Yêu cầu đặt đơn online cho nhân viên cửa hàng xử lý (FR-03).</summary>
[ApiController]
[Route("api/order-requests")]
public class OrderRequestsController(Data.NexusDbContext db) : ApiControllerBase
{
    /// <summary>Yêu cầu đang chờ của cửa hàng mình.</summary>
    [HttpGet]
    public async Task<IActionResult> Pending([FromQuery] string? status, CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Admin);

        var query = db.OrderRequests.AsNoTracking()
            .Include(r => r.Customer).Include(r => r.Plan).Include(r => r.Shop)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status.ToUpper());
        }

        var items = await query.OrderByDescending(r => r.CreatedAt).Select(r => new
            {
                r.RequestId,
                CustomerName = r.Customer!.FullName,
                CustomerId = r.CustomerId,
                r.ConnType,
                PlanName = r.Plan!.PlanName,
                ShopName = r.Shop!.ShopName,
                r.Note,
                r.Status,
                r.CreatedAt
            }).ToListAsync(ct);

        return OkResponse(items);
    }

    /// <summary>Từ chối yêu cầu kèm lý do.</summary>
    [HttpPost("{requestId:int}/reject")]
    public async Task<IActionResult> Reject(int requestId, [FromBody] ReasonRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Admin);

        var entity = await db.OrderRequests.FirstOrDefaultAsync(r => r.RequestId == requestId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy yêu cầu.");

        if (entity.Status != "PENDING")
        {
            throw new BusinessException(ErrorCode.REQUEST_STATE_INVALID, "Yêu cầu đã được xử lý.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Phải ghi lý do từ chối.");
        }

        entity.Status = "REJECTED";
        entity.RejectReason = request.Reason;
        entity.HandledByEmployeeId = Current.EmployeeId;
        await db.SaveChangesAsync(ct);

        return OkResponse(true, "Đã từ chối yêu cầu.");
    }
}