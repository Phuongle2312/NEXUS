using Nexus.Api.Common;
using Nexus.Api.Services;
using Xunit;

namespace Nexus.Tests;

/// <summary>
/// Kiểm thử công thức hóa đơn theo đúng hai ví dụ đã ghi trong SRS/API Design:
/// Broadband 225$ cọc 500$ → tổng 752,54$; đơn 10 kết nối giảm 25% → tổng 564,41$.
/// </summary>
public class BillingCalculatorTests
{
    private const decimal TaxRate = 0.1224m;

    private static BillingCalculator.Input Input(
        decimal planFee = 225m,
        decimal deposit = 500m,
        decimal discount = 0m,
        bool firstPeriod = true,
        bool cycleStart = true,
        IReadOnlyList<BillingCalculator.CallUsage>? calls = null,
        decimal usageHours = 0m,
        decimal priorHours = 0m,
        int? hoursIncluded = null,
        decimal? overageRate = null,
        decimal replacement = 0m,
        decimal adjustment = 0m,
        decimal previousDue = 0m) =>
        new(firstPeriod, cycleStart, planFee, discount, deposit, calls ?? [], usageHours, priorHours,
            hoursIncluded, overageRate, replacement, adjustment, TaxRate, previousDue);

    [Fact]
    public void FirstPeriod_WithoutDiscount_MatchesSrsExample()
    {
        var result = BillingCalculator.Calculate(Input());

        Assert.Equal(225.00m, result.PlanFee);
        Assert.Equal(27.54m, result.TaxAmount);
        Assert.Equal(500.00m, result.DepositAmount);
        Assert.Equal(752.54m, result.TotalAmount);
        Assert.Equal(752.54m, result.AmountToPay);
    }

    [Fact]
    public void FirstPeriod_With25PercentBulkDiscount_MatchesSrsExample()
    {
        var result = BillingCalculator.Calculate(Input(discount: 25m));

        Assert.Equal(168.75m, result.PlanFee);
        Assert.Equal(20.66m, result.TaxAmount);
        Assert.Equal(375.00m, result.DepositAmount);
        Assert.Equal(564.41m, result.TotalAmount);
    }

    [Fact]
    public void LaterCycleStart_ChargesFullPriceWithoutDiscount()
    {
        var result = BillingCalculator.Calculate(Input(firstPeriod: false, cycleStart: true, discount: 25m));

        // Ưu đãi chỉ áp dụng cho advance và cọc ở kỳ đầu.
        Assert.Equal(225.00m, result.PlanFee);
        Assert.Equal(0m, result.DepositAmount);
        Assert.Equal(27.54m, result.TaxAmount);
    }

    [Fact]
    public void MidCycle_ChargesNoPlanFee()
    {
        var result = BillingCalculator.Calculate(Input(firstPeriod: false, cycleStart: false));

        Assert.Equal(0m, result.PlanFee);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public void CallCharges_RoundPerLine()
    {
        var calls = new List<BillingCalculator.CallUsage>
        {
            new("LOCAL", 100m, 0.70m),
            new("STD", 10m, 2.25m),
            new("SMS", 3m, 1.15m)
        };

        var result = BillingCalculator.Calculate(Input(calls: calls));

        // 70,00 + 22,50 + 3,45 = 95,95
        Assert.Equal(95.95m, result.CallCharges);
    }

    [Fact]
    public void Overage_IsCumulativeWithinCycle()
    {
        // Gói 30 giờ / 3 tháng. Kỳ trước đã dùng 20 giờ, kỳ này thêm 15 giờ.
        // Tổng 35 giờ, vượt 5 giờ, kỳ trước chưa vượt nên kỳ này thu 5 giờ × 4,33 = 21,65.
        var overage = BillingCalculator.OverageCharges(15m, 20m, 30, 4.33m);

        Assert.Equal(21.65m, overage);
    }

    [Fact]
    public void Overage_OnlyChargesNewExcess()
    {
        // Kỳ trước đã vượt 10 giờ (40 giờ), kỳ này thêm 5 giờ: chỉ thu 5 giờ mới.
        var overage = BillingCalculator.OverageCharges(5m, 40m, 30, 4.33m);

        Assert.Equal(21.65m, overage);
    }

    [Fact]
    public void Overage_UnlimitedPlanReturnsZero()
    {
        Assert.Equal(0m, BillingCalculator.OverageCharges(100m, 0m, null, null));
    }

    [Fact]
    public void PreviousDue_IsNotTaxedNorAddedToTotal()
    {
        var result = BillingCalculator.Calculate(Input(previousDue: 140.30m));

        Assert.Equal(752.54m, result.TotalAmount);
        Assert.Equal(140.30m, result.PreviousDue);
        Assert.Equal(892.84m, result.AmountToPay);
    }

    [Fact]
    public void ReplacementCharge_IsTaxable()
    {
        var result = BillingCalculator.Calculate(Input(replacement: 40m));

        Assert.Equal(265.00m, result.TaxableBase);
        // 265 × 0,1224 = 32,436 → 32,44
        Assert.Equal(32.44m, result.TaxAmount);
        Assert.Equal(797.44m, result.TotalAmount);
    }

    [Fact]
    public void NegativeTaxableBase_Throws()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            BillingCalculator.Calculate(Input(adjustment: -1000m)));

        Assert.Equal(ErrorCode.VALIDATION_ERROR, ex.Code);
    }

    [Fact]
    public void CycleStart_IsBasedOnValidityMonths()
    {
        var first = new DateOnly(2026, 7, 1);

        Assert.True(BillingCalculator.IsCycleStart(first, new DateOnly(2026, 7, 1), 3));
        Assert.False(BillingCalculator.IsCycleStart(first, new DateOnly(2026, 8, 1), 3));
        Assert.False(BillingCalculator.IsCycleStart(first, new DateOnly(2026, 9, 1), 3));
        Assert.True(BillingCalculator.IsCycleStart(first, new DateOnly(2026, 10, 1), 3));
    }

    [Fact]
    public void Refund_NeverNegative()
    {
        Assert.Equal(112.30m, BillingCalculator.Refund(250m, 137.70m, 0m));
        Assert.Equal(0m, BillingCalculator.Refund(250m, 400m, 0m));
        Assert.Equal(0m, BillingCalculator.Refund(250m, 0m, 300m));
    }
}