using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

/// <summary>Ghi nhận thanh toán, biên lai và hoàn cọc (FR-10).</summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController(PaymentService payments, NexusDbContext db) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Accounts);
        var payment = await payments.RecordAsync(Current, request.BillId, request.Amount,
            request.Method.ToUpper(), request.PaidDate, request.Note, ct);

        return OkResponse(new
        {
            payment.PaymentId, payment.BillId, payment.Amount, payment.Method, payment.PaidDate
        }, "Đã ghi nhận thanh toán.");
    }

    /// <summary>Lịch sử thanh toán theo hóa đơn hoặc kết nối.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? billId, [FromQuery] string? accountId,
        CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Accounts, Roles.Admin);

        var query = db.Payments.AsNoTracking()
            .Include(p => p.Bill).ThenInclude(b => b!.Connection).ThenInclude(c => c!.Customer)
            .AsQueryable();

        if (billId is not null)
        {
            query = query.Where(p => p.BillId == billId);
        }

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(p => p.Bill!.AccountId == accountId.ToUpper());
        }

        var items = await query.OrderByDescending(p => p.PaymentId).Take(300)
            .Select(p => new
            {
                p.PaymentId,
                p.BillId,
                AccountId = p.Bill!.AccountId,
                CustomerName = p.Bill.Connection!.Customer!.FullName,
                p.Amount,
                p.Method,
                p.PaidDate,
                p.Note
            })
            .ToListAsync(ct);

        return OkResponse(items);
    }

    /// <summary>Biên lai để in.</summary>
    [HttpGet("{paymentId:int}/receipt")]
    public async Task<IActionResult> Receipt(int paymentId, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking()
            .Include(p => p.Bill).ThenInclude(b => b!.Connection).ThenInclude(c => c!.Customer)
            .Include(p => p.ReceivedByEmployee)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy biên lai.");

        if (Current.IsCustomer && payment.Bill!.Connection!.CustomerId != Current.CustomerId)
        {
            throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy biên lai của bạn.");
        }

        return OkResponse(new
        {
            payment.PaymentId,
            payment.BillId,
            payment.Bill!.AccountId,
            CustomerName = payment.Bill.Connection?.Customer?.FullName,
            payment.Amount,
            payment.Method,
            payment.PaidDate,
            payment.Note,
            ReceivedBy = payment.ReceivedByEmployee?.FullName,
            BillTotal = payment.Bill.TotalAmount,
            BillDue = payment.Bill.DueAmount
        });
    }

    /// <summary>Hoàn tiền đặt cọc khi kết nối đã ngưng vĩnh viễn.</summary>
    [HttpPost("refund-deposit/{accountId}")]
    public async Task<IActionResult> RefundDeposit(string accountId, CancellationToken ct)
    {
        RequireRoles(Roles.Accounts);
        var result = await payments.RefundDepositAsync(Current, accountId.ToUpper(), ct);
        return OkResponse(result, "Đã hoàn tiền đặt cọc.");
    }
}