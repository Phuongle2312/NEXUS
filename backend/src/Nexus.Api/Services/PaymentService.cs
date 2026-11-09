using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>Kết quả hoàn cọc để hiển thị cho kế toán.</summary>
public sealed record RefundResult(decimal BilledDeposit, decimal Offset, decimal Refund);

/// <summary>
/// Ghi nhận thanh toán và hoàn cọc (FR-10).
/// Khóa dòng hóa đơn trước khi cộng tiền nên hai người ghi cùng lúc không thể làm số đã trả vượt tổng.
/// </summary>
public class PaymentService(NexusDbContext db)
{
    private static readonly string[] AllowedMethods = ["CASH", "CARD", "TRANSFER"];
    private const string OffsetMethod = "DEPOSIT_OFFSET";

    /// <summary>Ghi một thanh toán và cập nhật số đã trả, còn nợ, trạng thái hóa đơn.</summary>
    public async Task<Payment> RecordAsync(CurrentUser actor, int billId, decimal amount, string method,
        DateOnly paidDate, string? note, CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số tiền phải lớn hơn 0.");
        }

        if (!AllowedMethods.Contains(method))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Hình thức thanh toán không hợp lệ.");
        }

        if (paidDate > DateOnly.FromDateTime(DateTime.Now))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Ngày thanh toán không được ở tương lai.");
        }

        if (actor.EmployeeId is null)
        {
            throw new BusinessException(ErrorCode.FORBIDDEN, "Tài khoản không gắn với nhân viên.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var bill = await db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hóa đơn.");

        if (amount > bill.DueAmount)
        {
            throw new BusinessException(ErrorCode.PAYMENT_EXCEEDS_DUE,
                $"Số tiền {amount} vượt số còn nợ {bill.DueAmount}.");
        }

        var payment = await InsertAndApplyAsync(bill, amount, method, paidDate, note,
            actor.EmployeeId.Value, ct);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "RECORD_PAYMENT",
            EntityType = "Payment",
            EntityId = payment.PaymentId.ToString(),
            Detail = $"Hóa đơn {billId}: {amount}"
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return payment;
    }

    /// <summary>
    /// Hoàn cọc khi kết nối đã ngưng vĩnh viễn, mỗi kết nối một lần:
    /// hoàn = cọc đã lập hóa đơn − nợ còn lại − phí thay thế chưa lập hóa đơn, tối thiểu 0.
    ///
    /// Phần nợ đã trừ vào cọc được bù trừ thật trên sổ (thanh toán DEPOSIT_OFFSET, phí chuyển
    /// trạng thái), nếu không khách vừa bị trừ cọc vừa còn nợ, tức bị thu hai lần.
    /// </summary>
    public async Task<RefundResult> RefundDepositAsync(CurrentUser actor, string accountId,
        CancellationToken ct = default)
    {
        if (actor.EmployeeId is null)
        {
            throw new BusinessException(ErrorCode.FORBIDDEN, "Tài khoản không gắn với nhân viên.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var connection = await db.Connections.FirstOrDefaultAsync(c => c.AccountId == accountId, ct)
            ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, "Không tìm thấy Account ID.");

        if (connection.Status != ConnectionStatus.PermInactive)
        {
            throw new BusinessException(ErrorCode.CONNECTION_STATE_INVALID,
                "Chỉ hoàn cọc khi kết nối đã ngưng vĩnh viễn.");
        }

        if (connection.DepositRefundedAt is not null)
        {
            throw new BusinessException(ErrorCode.DEPOSIT_ALREADY_REFUNDED,
                "Tiền đặt cọc của kết nối đã được hoàn.");
        }

        var billedDeposit = await db.Bills
            .Where(b => b.AccountId == accountId)
            .SumAsync(b => b.DepositAmount, ct);

        var openBills = await db.Bills
            .Where(b => b.AccountId == accountId && b.DueAmount > 0)
            .OrderBy(b => b.PeriodStart)
            .ToListAsync(ct);

        var unbilled = await db.ReplacementCharges
            .Where(c => c.AccountId == accountId && c.Status == "UNBILLED")
            .OrderBy(c => c.ChargeDate)
            .ToListAsync(ct);

        var openDue = openBills.Sum(b => b.DueAmount);
        var unbilledTotal = unbilled.Sum(c => c.Amount);
        var refund = BillingCalculator.Refund(billedDeposit, openDue, unbilledTotal);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var remaining = billedDeposit;

        // Dùng cọc trả các hóa đơn còn nợ, cũ nhất trước.
        foreach (var bill in openBills)
        {
            if (remaining <= 0)
            {
                break;
            }

            var used = Math.Min(remaining, bill.DueAmount);
            await InsertAndApplyAsync(bill, used, OffsetMethod, today,
                "Bù trừ từ tiền cọc khi hoàn cọc", actor.EmployeeId.Value, ct);
            remaining -= used;
        }

        // Phần cọc còn lại trả phí thay thế chưa lập hóa đơn; không đủ thì giảm số tiền của khoản đó.
        foreach (var charge in unbilled)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (remaining >= charge.Amount)
            {
                charge.Status = "DEPOSIT_OFFSET";
                remaining -= charge.Amount;
            }
            else
            {
                charge.Amount -= remaining;
                charge.Note = ((charge.Note is null ? "" : charge.Note + "; ")
                               + $"Đã bù trừ {remaining} từ tiền cọc").Trim();
                remaining = 0m;
            }
        }

        connection.DepositRefundAmount = refund;
        connection.DepositRefundedAt = DateTime.Now;

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "REFUND_DEPOSIT",
            EntityType = "Connection",
            EntityId = accountId,
            Detail = $"Cọc {billedDeposit}, bù trừ {billedDeposit - remaining}, hoàn {refund}"
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new RefundResult(billedDeposit, billedDeposit - remaining, refund);
    }

    /// <summary>Thêm dòng thanh toán và cập nhật số đã trả, còn nợ, trạng thái của hóa đơn.</summary>
    private async Task<Payment> InsertAndApplyAsync(Bill bill, decimal amount, string method, DateOnly date,
        string? note, int employeeId, CancellationToken ct)
    {
        var payment = new Payment
        {
            BillId = bill.BillId,
            Amount = amount,
            Method = method,
            PaidDate = date,
            Note = note,
            ReceivedByEmployeeId = employeeId,
            CreatedAt = DateTime.Now
        };
        db.Payments.Add(payment);

        bill.PaidAmount += amount;
        bill.DueAmount -= amount;
        bill.Status = bill.DueAmount == 0 ? "PAID" : "PARTIAL";
        await Task.CompletedTask;
        return payment;
    }
}