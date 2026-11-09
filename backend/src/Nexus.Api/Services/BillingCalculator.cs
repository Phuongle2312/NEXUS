using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>
/// Tính hóa đơn (SRS BR-07) dưới dạng hàm thuần, không đụng CSDL,
/// để preview và generate dùng chung và unit test trực tiếp được.
///
///   Phí gói        = kỳ đầu: phí gói × (1 − % ưu đãi); đầu chu kỳ sau: phí gói; còn lại: 0
///   Phần chịu thuế = phí gói + cước gọi + vượt gói + phí thay thế + điều chỉnh
///   Thuế           = làm tròn 2 số (phần chịu thuế × thuế suất), HALF_UP
///   Đặt cọc        = đặt cọc × (1 − % ưu đãi), chỉ kỳ đầu, không chịu thuế
///   Tổng kỳ này    = phần chịu thuế + thuế + đặt cọc
///   Cần thanh toán = tổng kỳ này + nợ kỳ trước (nợ kỳ trước không cộng vào tổng)
/// </summary>
public static class BillingCalculator
{
    private const decimal Hundred = 100m;

    /// <summary>Số phút và đơn giá của một loại cuộc gọi.</summary>
    public readonly record struct CallUsage(string CallType, decimal Minutes, decimal RatePerMin);

    /// <summary>Dữ liệu đầu vào của một kỳ cước. Giá gói và cọc là giá TRƯỚC ưu đãi, chụp trên đơn.</summary>
    public sealed record Input(
        bool FirstPeriod,
        bool CycleStart,
        decimal ListPlanFee,
        decimal DiscountPercent,
        decimal ListDeposit,
        IReadOnlyList<CallUsage> Calls,
        decimal UsageHours,
        decimal PriorHoursInCycle,
        int? HoursIncluded,
        decimal? OverageRatePerHour,
        decimal ReplacementCharges,
        decimal Adjustment,
        decimal TaxRate,
        decimal PreviousDue);

    /// <summary>Kết quả tính; tất cả đã làm tròn 2 chữ số.</summary>
    public sealed record Result(
        decimal PlanFee,
        decimal CallCharges,
        decimal OverageCharges,
        decimal ReplacementCharges,
        decimal Adjustment,
        decimal TaxableBase,
        decimal TaxAmount,
        decimal DepositAmount,
        decimal TotalAmount,
        decimal PreviousDue,
        decimal AmountToPay);

    /// <summary>Tính toàn bộ các khoản của một hóa đơn.</summary>
    public static Result Calculate(Input input)
    {
        var planFee = PlanFee(input);
        var callCharges = CallCharges(input.Calls);
        var overage = OverageCharges(
            input.UsageHours, input.PriorHoursInCycle, input.HoursIncluded, input.OverageRatePerHour);
        var replacement = Round(input.ReplacementCharges);
        var adjustment = Round(input.Adjustment);

        var taxable = planFee + callCharges + overage + replacement + adjustment;
        if (taxable < 0)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Điều chỉnh làm phần chịu thuế bị âm.");
        }

        var tax = Round(taxable * input.TaxRate);
        var deposit = input.FirstPeriod ? AfterDiscount(input.ListDeposit, input.DiscountPercent) : Zero();
        var total = taxable + tax + deposit;
        var previousDue = Round(input.PreviousDue);

        return new Result(planFee, callCharges, overage, replacement, adjustment,
            taxable, tax, deposit, total, previousDue, total + previousDue);
    }

    /// <summary>Kỳ đầu thu advance (đã giảm); chu kỳ sau thu đủ giá, ưu đãi chỉ áp dụng cho advance và cọc.</summary>
    private static decimal PlanFee(Input input)
    {
        if (input.FirstPeriod)
        {
            return AfterDiscount(input.ListPlanFee, input.DiscountPercent);
        }

        return input.CycleStart ? Round(input.ListPlanFee) : Zero();
    }

    /// <summary>Cước gọi: mỗi loại làm tròn riêng để tổng khớp với các dòng in trên hóa đơn.</summary>
    private static decimal CallCharges(IReadOnlyList<CallUsage> calls) =>
        calls.Aggregate(Zero(), (sum, call) => sum + LineAmount(call));

    /// <summary>Thành tiền một dòng cước gọi.</summary>
    public static decimal LineAmount(CallUsage call) => Round(call.Minutes * call.RatePerMin);

    /// <summary>
    /// Vượt gói tính lũy kế trong chu kỳ hiệu lực, vì số giờ của gói áp dụng cho cả chu kỳ
    /// (gói 30 giờ / 3 tháng, không phải 30 giờ mỗi tháng). Chỉ tính phần vượt mới phát sinh
    /// ở kỳ này; phần đã vượt ở các kỳ trước đã được thu trên hóa đơn trước.
    /// </summary>
    public static decimal OverageCharges(decimal usageHours, decimal priorHours, int? hoursIncluded, decimal? rate)
    {
        if (hoursIncluded is null || rate is null)
        {
            return Zero();
        }

        var included = hoursIncluded.Value;
        var overBefore = Math.Max(0m, priorHours - included);
        var overNow = Math.Max(0m, priorHours + usageHours - included);
        return Round((overNow - overBefore) * rate.Value);
    }

    /// <summary>Kỳ này có bắt đầu một chu kỳ hiệu lực mới không, tính theo số tháng từ kỳ đầu tiên.</summary>
    public static bool IsCycleStart(DateOnly firstPeriodStart, DateOnly periodStart, int validityMonths) =>
        MonthsBetween(firstPeriodStart, periodStart) % validityMonths == 0;

    /// <summary>Ngày bắt đầu của chu kỳ hiệu lực chứa kỳ này.</summary>
    public static DateOnly CycleStartDate(DateOnly firstPeriodStart, DateOnly periodStart, int validityMonths)
    {
        var cycles = MonthsBetween(firstPeriodStart, periodStart) / validityMonths;
        return firstPeriodStart.AddMonths((int)(cycles * validityMonths));
    }

    private static long MonthsBetween(DateOnly from, DateOnly to) =>
        ((to.Year - from.Year) * 12L) + (to.Month - from.Month);

    /// <summary>Hoàn cọc = cọc đã lập hóa đơn − nợ còn lại − phí thay thế chưa lập hóa đơn, tối thiểu 0.</summary>
    public static decimal Refund(decimal billedDeposit, decimal openDue, decimal unbilledCharges) =>
        Round(Math.Max(0m, billedDeposit - openDue - unbilledCharges));

    private static decimal AfterDiscount(decimal amount, decimal discountPercent) =>
        Round(amount * ((Hundred - discountPercent) / Hundred));

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal Zero() => 0.00m;
}

/// <summary>
/// Ưu đãi số lượng lớn (BR-05): đọc bậc từ bảng BulkDiscounts, không hard-code.
/// Đúng giá trị biên (15, 25, 50) thuộc bậc dưới theo dữ liệu seed.
/// </summary>
public class DiscountService(NexusDbContext db)
{
    /// <summary>Phần trăm giảm cho số kết nối; dưới bậc thấp nhất trả 0.</summary>
    public async Task<decimal> PercentForAsync(int quantity, CancellationToken ct = default)
    {
        if (quantity < 1)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số lượng kết nối phải từ 1 trở lên.");
        }

        var tier = await db.BulkDiscounts
            .AsNoTracking()
            .Where(d => d.MinQty <= quantity && (d.MaxQty == null || d.MaxQty >= quantity))
            .OrderByDescending(d => d.MinQty)
            .FirstOrDefaultAsync(ct);

        return tier?.DiscountPercent ?? 0m;
    }
}

/// <summary>
/// Sinh Order ID / Account ID tuần tự trong transaction (API Design mục 2).
/// Khóa dòng bộ đếm bằng UPDLOCK để hai người tạo cùng lúc không trùng số.
/// </summary>
public class IdGenerator(NexusDbContext db)
{
    /// <summary>Order ID 11 ký tự: [D|T|B] + 10 chữ số. Bộ đếm riêng cho từng loại.</summary>
    public async Task<string> NextOrderIdAsync(string connType, CancellationToken ct = default)
    {
        var seqName = "ORDER_" + connType.ToUpperInvariant();
        var value = await NextAsync(seqName, ct);
        return connType.ToUpperInvariant() + value.ToString("D10", CultureInfo.InvariantCulture);
    }

    /// <summary>Account ID 16 ký tự: [D|B|T] + mã thành phố 3 số + 12 chữ số, dùng chung một bộ đếm.</summary>
    public async Task<string> NextAccountIdAsync(string connType, string cityCode, CancellationToken ct = default)
    {
        if (cityCode.Length != 3 || cityCode.Any(c => c is < '0' or > '9'))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Mã thành phố phải gồm đúng 3 chữ số.");
        }

        var value = await NextAsync("ACCOUNT", ct);
        return connType.ToUpperInvariant() + cityCode + value.ToString("D12", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tăng bộ đếm trong transaction hiện tại. UPDLOCK giữ dòng cho tới khi commit,
    /// nên các request đồng thời xếp hàng thay vì nhận cùng một số.
    /// </summary>
    private async Task<long> NextAsync(string seqName, CancellationToken ct)
    {
        var seq = await db.Sequences
            .FromSqlInterpolated(
                $"SELECT * FROM Sequences WITH (UPDLOCK, ROWLOCK) WHERE SeqName = {seqName}")
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, $"Không tìm thấy bộ đếm {seqName}.");

        seq.CurrentValue += 1;
        await db.SaveChangesAsync(ct);
        return seq.CurrentValue;
    }
}

/// <summary>Đọc tham số cấu hình trong SystemConfig, có giá trị mặc định an toàn.</summary>
public class ConfigService(NexusDbContext db)
{
    public async Task<decimal> DecimalAsync(string key, decimal fallback, CancellationToken ct = default)
    {
        var value = await db.SystemConfigs.AsNoTracking()
            .Where(c => c.ConfigKey == key)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync(ct);

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    public async Task<int> IntAsync(string key, int fallback, CancellationToken ct = default)
    {
        var value = await db.SystemConfigs.AsNoTracking()
            .Where(c => c.ConfigKey == key)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync(ct);

        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}