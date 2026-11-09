using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

public sealed record PaymentRequest(int BillId, decimal Amount, string Method, DateOnly PaidDate, string? Note);

/// <summary>Lập hóa đơn, xem hóa đơn, đề nghị tạm ngưng (FR-09).</summary>
[ApiController]
[Route("api/billing")]
public class BillingController(BillingService billing, NexusDbContext db) : ApiControllerBase
{
    /// <summary>Tính thử hóa đơn, không lưu.</summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] GenerateBillRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Accounts, Roles.Admin);
        return OkResponse(await billing.PreviewAsync(request, ct));
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateBillRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Accounts);
        var bill = await billing.GenerateAsync(Current, request, ct);
        return OkResponse(new
        {
            bill.BillId, bill.AccountId, bill.PeriodStart, bill.PeriodEnd,
            bill.TaxableBase, bill.TaxAmount, bill.DepositAmount, bill.TotalAmount, bill.DueDate
        }, "Đã lập hóa đơn.");
    }

    [HttpPut("{billId:int}")]
    public async Task<IActionResult> Update(int billId, [FromBody] GenerateBillRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Accounts);
        var bill = await billing.UpdateAsync(Current, billId, request.MinutesLocal, request.MinutesStd,
            request.MinutesSms, request.UsageHours, request.Adjustment, request.Note, ct);
        return OkResponse(new { bill.BillId, bill.TotalAmount }, "Đã cập nhật hóa đơn.");
    }

    /// <summary>Danh sách hóa đơn; khách chỉ thấy hóa đơn của mình.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? accountId, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1,
        [FromQuery] int size = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var query = db.Bills.AsNoTracking()
            .Include(b => b.Connection).ThenInclude(c => c!.Customer)
            .AsQueryable();

        if (Current.IsCustomer)
        {
            query = query.Where(b => b.Connection!.CustomerId == Current.CustomerId);
        }
        else
        {
            RequireRoles(Roles.Accounts, Roles.Admin, Roles.Retail);
        }

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(b => b.AccountId == accountId.ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status.ToUpper());
        }

        if (from is not null)
        {
            query = query.Where(b => b.BillDate >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(b => b.BillDate <= to.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.BillId)
            .Skip((page - 1) * size).Take(size)
            .Select(b => new
            {
                b.BillId,
                b.AccountId,
                CustomerName = b.Connection!.Customer!.FullName,
                b.PeriodStart,
                b.PeriodEnd,
                b.PlanFee,
                b.CallCharges,
                b.OverageCharges,
                b.ReplacementCharges,
                b.TaxAmount,
                b.DepositAmount,
                b.TotalAmount,
                b.PaidAmount,
                b.DueAmount,
                b.PreviousDue,
                b.BillDate,
                b.DueDate,
                b.Status
            })
            .ToListAsync(ct);

        return OkResponse(new PagedResult<object>(items.Cast<object>().ToList(), page, size, total));
    }

    /// <summary>Chi tiết hóa đơn kèm dòng cước gọi và thanh toán.</summary>
    [HttpGet("{billId:int}")]
    public async Task<IActionResult> Get(int billId, CancellationToken ct)
    {
        var bill = await db.Bills.AsNoTracking()
            .Include(b => b.Connection).ThenInclude(c => c!.Customer)
            .Include(b => b.Connection).ThenInclude(c => c!.Plan)
            .Include(b => b.CallUsages)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hóa đơn.");

        if (Current.IsCustomer && bill.Connection!.CustomerId != Current.CustomerId)
        {
            throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hóa đơn của bạn.");
        }

        return OkResponse(new
        {
            bill.BillId,
            bill.AccountId,
            CustomerName = bill.Connection!.Customer!.FullName,
            PlanName = bill.Connection.Plan!.PlanName,
            bill.PeriodStart,
            bill.PeriodEnd,
            bill.PlanFee,
            bill.CallCharges,
            bill.UsageHours,
            bill.OverageCharges,
            bill.ReplacementCharges,
            bill.Adjustment,
            bill.TaxableBase,
            bill.TaxRate,
            bill.TaxAmount,
            bill.DepositAmount,
            bill.TotalAmount,
            bill.PaidAmount,
            bill.DueAmount,
            bill.PreviousDue,
            bill.BillDate,
            bill.DueDate,
            bill.Status,
            bill.Note,
            CallUsages = bill.CallUsages.Select(c => new { c.CallType, c.Minutes, c.RatePerMin, c.Amount }).ToList(),
            Payments = bill.Payments.Select(p => new { p.PaymentId, p.Amount, p.Method, p.PaidDate }).ToList()
        });
    }

    /// <summary>Hóa đơn quá hạn thanh toán theo số ngày cấu hình.</summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> Overdue([FromQuery] int? days, CancellationToken ct)
    {
        RequireRoles(Roles.Accounts, Roles.Admin);
        var limit = DateOnly.FromDateTime(DateTime.Now).AddDays(-(days ?? 30));

        var items = await db.Bills.AsNoTracking()
            .Include(b => b.Connection).ThenInclude(c => c!.Customer)
            .Where(b => b.DueAmount > 0 && b.DueDate < limit)
            .OrderBy(b => b.DueDate)
            .Select(b => new
            {
                b.BillId, b.AccountId, CustomerName = b.Connection!.Customer!.FullName,
                b.DueDate, b.DueAmount, b.PreviousDue
            })
            .ToListAsync(ct);

        return OkResponse(items);
    }

    /// <summary>Kế toán đề nghị kỹ thuật tạm ngưng kết nối quá hạn.</summary>
    [HttpPost("{accountId}/request-suspend")]
    public async Task<IActionResult> RequestSuspend(string accountId, [FromBody] ReasonRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Accounts);

        var connection = await db.Connections.FirstOrDefaultAsync(c => c.AccountId == accountId.ToUpper(), ct)
            ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, "Không tìm thấy kết nối.");

        if (connection.Status == ConnectionStatus.PermInactive)
        {
            throw new BusinessException(ErrorCode.CONNECTION_STATE_INVALID, "Kết nối đã ngưng vĩnh viễn.");
        }

        connection.SuspendRequested = true;
        connection.SuspendRequestReason = request.Reason;
        connection.SuspendRequestedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);

        return OkResponse(true, "Đã gửi đề nghị tạm ngưng tới bộ phận kỹ thuật.");
    }
}

public sealed record ReasonRequest(string? Reason);