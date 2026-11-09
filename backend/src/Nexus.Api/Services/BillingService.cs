using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>Dữ liệu lập hóa đơn từ form của kế toán.</summary>
public sealed record GenerateBillRequest(
    string AccountId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal MinutesLocal,
    decimal MinutesStd,
    decimal MinutesSms,
    decimal UsageHours,
    decimal Adjustment,
    string? Note);

/// <summary>
/// Lập và sửa hóa đơn (FR-09). Công thức nằm trong <see cref="BillingCalculator"/>;
/// lớp này lo đọc dữ liệu, kiểm tra trạng thái và ghi trong transaction.
/// </summary>
public class BillingService(NexusDbContext db, ConfigService config)
{
    /// <summary>Tính thử hóa đơn, không ghi CSDL để kế toán xem trước.</summary>
    public async Task<BillingCalculator.Result> PreviewAsync(GenerateBillRequest req, CancellationToken ct = default)
    {
        var (input, _) = await BuildInputAsync(req, ct);
        return BillingCalculator.Calculate(input);
    }

    /// <summary>
    /// Lập hóa đơn thật: khóa kết nối, chặn kỳ trùng hoặc chồng lấn, gắn phí thay thế chưa lập,
    /// rồi lưu trong một transaction.
    /// </summary>
    public async Task<Bill> GenerateAsync(CurrentUser actor, GenerateBillRequest req, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var connection = await db.Connections
            .FirstOrDefaultAsync(c => c.AccountId == req.AccountId, ct)
            ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, $"Không tìm thấy kết nối {req.AccountId}.");

        if (connection.DepositRefundedAt is not null)
        {
            throw new BusinessException(ErrorCode.CONNECTION_STATE_INVALID,
                "Kết nối đã hoàn cọc, không lập hóa đơn mới.");
        }

        if (req.PeriodEnd < req.PeriodStart)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Ngày kết thúc kỳ phải sau ngày bắt đầu.");
        }

        if (req.PeriodEnd > req.PeriodStart.AddMonths(1))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Kỳ cước không được dài quá một tháng.");
        }

        if (req.PeriodEnd < connection.ActivationDate)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Kỳ cước kết thúc trước ngày kích hoạt.");
        }

        // Chặn cả trùng tuyệt đối lẫn chồng lấn: kỳ mới phải bắt đầu sau kỳ cuối đã lập.
        var lastPeriodEnd = await db.Bills
            .Where(b => b.AccountId == req.AccountId)
            .MaxAsync(b => (DateOnly?)b.PeriodEnd, ct);

        if (lastPeriodEnd is not null && req.PeriodStart <= lastPeriodEnd)
        {
            throw new BusinessException(ErrorCode.BILL_EXISTS,
                $"Đã có hóa đơn tới ngày {lastPeriodEnd:yyyy-MM-dd}; kỳ mới phải bắt đầu sau ngày đó.");
        }

        var (input, isFirstPeriod) = await BuildInputAsync(req, ct);
        var result = BillingCalculator.Calculate(input);

        var dueDays = await config.IntAsync("BILL_DUE_DAYS", 15, ct);
        var billDate = DateOnly.FromDateTime(DateTime.Now);

        var bill = new Bill
        {
            AccountId = req.AccountId,
            PeriodStart = req.PeriodStart,
            PeriodEnd = req.PeriodEnd,
            PlanFee = result.PlanFee,
            CallCharges = result.CallCharges,
            UsageHours = req.UsageHours,
            OverageCharges = result.OverageCharges,
            ReplacementCharges = result.ReplacementCharges,
            Adjustment = result.Adjustment,
            TaxableBase = result.TaxableBase,
            TaxRate = input.TaxRate,
            TaxAmount = result.TaxAmount,
            DepositAmount = result.DepositAmount,
            TotalAmount = result.TotalAmount,
            PaidAmount = 0m,
            DueAmount = result.TotalAmount,
            PreviousDue = result.PreviousDue,
            BillDate = billDate,
            DueDate = billDate.AddDays(dueDays),
            Status = "UNPAID",
            Note = req.Note,
            CreatedByEmployeeId = actor.EmployeeId
        };

        foreach (var call in input.Calls.Where(c => c.Minutes > 0))
        {
            bill.CallUsages.Add(new BillCallUsage
            {
                CallType = call.CallType,
                Minutes = call.Minutes,
                RatePerMin = call.RatePerMin,
                Amount = BillingCalculator.LineAmount(call)
            });
        }

        db.Bills.Add(bill);

        // Phí thay thế UNBILLED vào hóa đơn này, rồi đánh dấu BILLED trong cùng transaction.
        var unbilled = await db.ReplacementCharges
            .Where(c => c.AccountId == req.AccountId && c.Status == "UNBILLED")
            .ToListAsync(ct);
        foreach (var charge in unbilled)
        {
            charge.Status = "BILLED";
            charge.BillId = bill.BillId;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "GENERATE_BILL",
            EntityType = "Bill",
            EntityId = req.AccountId,
            Detail = $"{req.PeriodStart:yyyy-MM-dd}..{req.PeriodEnd:yyyy-MM-dd}, tổng {result.TotalAmount}"
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        _ = isFirstPeriod;
        return bill;
    }

    /// <summary>Sửa hóa đơn mới nhất khi chưa có thanh toán; kỳ cước giữ nguyên (FR-09).</summary>
    public async Task<Bill> UpdateAsync(CurrentUser actor, int billId, decimal minutesLocal,
        decimal minutesStd, decimal minutesSms, decimal usageHours, decimal adjustment, string? note,
        CancellationToken ct = default)
    {
        var bill = await db.Bills
            .Include(b => b.CallUsages)
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hóa đơn.");

        if (bill.PaidAmount > 0)
        {
            throw new BusinessException(ErrorCode.BILL_STATE_INVALID, "Hóa đơn đã có thanh toán, không sửa được.");
        }

        var lastBillId = await db.Bills.Where(b => b.AccountId == bill.AccountId)
            .MaxAsync(b => b.BillId, ct);
        if (lastBillId != bill.BillId)
        {
            throw new BusinessException(ErrorCode.BILL_STATE_INVALID, "Chỉ sửa được hóa đơn mới nhất.");
        }

        await GenerateAsync(actor, new GenerateBillRequest(bill.AccountId, bill.PeriodStart, bill.PeriodEnd,
            minutesLocal, minutesStd, minutesSms, usageHours, adjustment, note), ct);

        db.Bills.Remove(bill);
        await db.SaveChangesAsync(ct);
        return bill;
    }

    /// <summary>
    /// Dựng đầu vào tính toán: giá gói chụp trên đơn, ưu đãi, số giờ lũy kế trong chu kỳ,
    /// phí thay thế chưa lập, nợ kỳ trước và thuế suất.
    /// </summary>
    private async Task<(BillingCalculator.Input Input, bool FirstPeriod)> BuildInputAsync(
        GenerateBillRequest req, CancellationToken ct)
    {
        var connection = await db.Connections
            .Include(c => c.Plan)
            .Include(c => c.Order)
            .FirstOrDefaultAsync(c => c.AccountId == req.AccountId, ct)
            ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, $"Không tìm thấy kết nối {req.AccountId}.");

        var plan = connection.Plan!;
        var order = connection.Order!;

        var firstBill = await db.Bills.Where(b => b.AccountId == req.AccountId)
            .OrderBy(b => b.PeriodStart)
            .FirstOrDefaultAsync(ct);
        var lastBill = await db.Bills.Where(b => b.AccountId == req.AccountId)
            .OrderByDescending(b => b.PeriodStart)
            .FirstOrDefaultAsync(ct);

        // Giá gói và cọc lấy theo giá chụp trên đơn; đơn Dial-up có thể kèm landline riêng.
        var listPlanFee = connection.ConnType == "T" && order.LandlinePlanId == connection.PlanId
            ? order.LandlinePlanFee ?? plan.Price
            : order.PlanFee;

        var listDeposit = connection.ConnType == "T" && order.LandlinePlanId == connection.PlanId
            ? order.LandlineDepositAmount ?? 0m
            : order.DepositAmount;

        var firstPeriod = lastBill is null;
        var cycleStart = firstPeriod
            ? true
            : BillingCalculator.IsCycleStart(firstBill!.PeriodStart, req.PeriodStart, plan.ValidityMonths);

        // Số giờ lũy kế trong chu kỳ hiệu lực hiện tại (không tính kỳ đang lập).
        decimal priorHours = 0m;
        if (lastBill is not null && plan.HoursIncluded is not null)
        {
            var cycleStartDate = BillingCalculator.CycleStartDate(firstBill!.PeriodStart, req.PeriodStart,
                plan.ValidityMonths);
            priorHours = await db.Bills
                .Where(b => b.AccountId == req.AccountId && b.PeriodStart >= cycleStartDate
                            && b.PeriodStart < req.PeriodStart)
                .SumAsync(b => b.UsageHours, ct);
        }

        var rates = await db.PlanCallRates.AsNoTracking()
            .Where(r => r.PlanId == plan.PlanId)
            .ToListAsync(ct);
        var rateOf = (string type) => rates.FirstOrDefault(r => r.CallType == type)?.RatePerMin ?? 0m;

        var calls = new List<BillingCalculator.CallUsage>
        {
            new("LOCAL", req.MinutesLocal, rateOf("LOCAL")),
            new("STD", req.MinutesStd, rateOf("STD")),
            new("SMS", req.MinutesSms, rateOf("SMS"))
        };

        var replacement = await db.ReplacementCharges
            .Where(c => c.AccountId == req.AccountId && c.Status == "UNBILLED")
            .SumAsync(c => c.Amount, ct);

        var previousDue = await db.Bills
            .Where(b => b.AccountId == req.AccountId && b.DueAmount > 0)
            .SumAsync(b => b.DueAmount, ct);

        var taxRate = await config.DecimalAsync("TAX_RATE", 0.1224m, ct);

        var input = new BillingCalculator.Input(
            FirstPeriod: firstPeriod,
            CycleStart: cycleStart,
            ListPlanFee: listPlanFee,
            DiscountPercent: order.DiscountPercent,
            ListDeposit: listDeposit,
            Calls: calls,
            UsageHours: req.UsageHours,
            PriorHoursInCycle: priorHours,
            HoursIncluded: plan.HoursIncluded,
            OverageRatePerHour: plan.OverageRatePerHour,
            ReplacementCharges: replacement,
            Adjustment: req.Adjustment,
            TaxRate: taxRate,
            PreviousDue: previousDue);

        return (input, firstPeriod);
    }
}