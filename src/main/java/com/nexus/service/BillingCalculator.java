package com.nexus.service;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.LocalDate;
import java.time.temporal.ChronoUnit;
import java.util.List;

/**
 * Công thức hóa đơn (SRS BR-07, API Design 4.7) dưới dạng hàm thuần, không đụng CSDL,
 * để preview và generate dùng chung và để unit test được trực tiếp.
 *
 *   Phí gói        = kỳ đầu: phí gói x (1 - % ưu đãi); đầu chu kỳ sau: phí gói; còn lại: 0
 *   Phần chịu thuế = phí gói + cước gọi + vượt gói + phí thay thế + điều chỉnh
 *   Thuế           = làm tròn 2 số (phần chịu thuế x thuế suất), HALF_UP
 *   Đặt cọc        = đặt cọc x (1 - % ưu đãi), chỉ kỳ đầu, không chịu thuế
 *   Tổng kỳ này    = phần chịu thuế + thuế + đặt cọc
 *   Cần thanh toán = tổng kỳ này + nợ kỳ trước (nợ kỳ trước không cộng vào tổng)
 */
public final class BillingCalculator {
    private static final BigDecimal HUNDRED = new BigDecimal("100");

    /** Số phút và đơn giá của một loại cuộc gọi. */
    public record CallUsage(String callType, BigDecimal minutes, BigDecimal ratePerMin) {
    }

    /** Dữ liệu đầu vào của một kỳ cước. Giá gói và cọc là giá TRƯỚC ưu đãi, chụp lại trên đơn. */
    public record Input(
            boolean firstPeriod,
            boolean cycleStart,
            BigDecimal listPlanFee,
            BigDecimal discountPercent,
            BigDecimal listDeposit,
            List<CallUsage> calls,
            BigDecimal usageHours,
            BigDecimal priorHoursInCycle,
            Integer hoursIncluded,
            BigDecimal overageRatePerHour,
            BigDecimal replacementCharges,
            BigDecimal adjustment,
            BigDecimal taxRate,
            BigDecimal previousDue) {
    }

    /** Kết quả tính; tất cả đã làm tròn 2 chữ số. */
    public record Result(
            BigDecimal planFee,
            BigDecimal callCharges,
            BigDecimal overageCharges,
            BigDecimal replacementCharges,
            BigDecimal adjustment,
            BigDecimal taxableBase,
            BigDecimal taxAmount,
            BigDecimal depositAmount,
            BigDecimal totalAmount,
            BigDecimal previousDue,
            BigDecimal amountToPay) {
    }

    private BillingCalculator() {
    }

    /** Tính toàn bộ các khoản của một hóa đơn. */
    public static Result calculate(Input in) {
        BigDecimal planFee = planFee(in);
        BigDecimal callCharges = callCharges(in.calls());
        BigDecimal overage = overageCharges(in.usageHours(), in.priorHoursInCycle(), in.hoursIncluded(), in.overageRatePerHour());
        BigDecimal replacement = round(in.replacementCharges());
        BigDecimal adjustment = round(in.adjustment());

        BigDecimal taxable = planFee.add(callCharges).add(overage).add(replacement).add(adjustment);
        if (taxable.signum() < 0) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Điều chỉnh làm phần chịu thuế bị âm.");
        }
        BigDecimal tax = round(taxable.multiply(in.taxRate()));
        BigDecimal deposit = in.firstPeriod() ? afterDiscount(in.listDeposit(), in.discountPercent()) : zero();
        BigDecimal total = taxable.add(tax).add(deposit);
        BigDecimal previousDue = round(in.previousDue());

        return new Result(planFee, callCharges, overage, replacement, adjustment,
                taxable, tax, deposit, total, previousDue, total.add(previousDue));
    }

    /** Kỳ đầu thu advance (đã giảm); các chu kỳ sau thu đủ giá, ưu đãi chỉ áp dụng cho advance và cọc. */
    private static BigDecimal planFee(Input in) {
        if (in.firstPeriod()) {
            return afterDiscount(in.listPlanFee(), in.discountPercent());
        }
        return in.cycleStart() ? round(in.listPlanFee()) : zero();
    }

    /** Cước gọi: mỗi loại làm tròn riêng để tổng khớp với các dòng in trên hóa đơn. */
    private static BigDecimal callCharges(List<CallUsage> calls) {
        BigDecimal sum = zero();
        for (CallUsage c : calls) {
            sum = sum.add(lineAmount(c));
        }
        return sum;
    }

    /** Thành tiền một dòng cước gọi. */
    public static BigDecimal lineAmount(CallUsage c) {
        return round(c.minutes().multiply(c.ratePerMin()));
    }

    /**
     * Vượt gói tính lũy kế trong chu kỳ hiệu lực, vì số giờ của gói áp dụng cho cả chu kỳ
     * (gói 30 giờ / 3 tháng, không phải 30 giờ mỗi tháng). Chỉ tính phần vượt mới phát sinh
     * ở kỳ này; phần đã vượt ở các kỳ trước đã được thu trên hóa đơn trước.
     */
    public static BigDecimal overageCharges(BigDecimal usageHours, BigDecimal priorHours, Integer hoursIncluded, BigDecimal rate) {
        if (hoursIncluded == null || rate == null) {
            return zero();
        }
        BigDecimal included = BigDecimal.valueOf(hoursIncluded);
        BigDecimal overBefore = priorHours.subtract(included).max(BigDecimal.ZERO);
        BigDecimal overNow = priorHours.add(usageHours).subtract(included).max(BigDecimal.ZERO);
        return round(overNow.subtract(overBefore).multiply(rate));
    }

    /** Kỳ này có bắt đầu một chu kỳ hiệu lực mới không, tính theo số tháng từ kỳ đầu tiên. */
    public static boolean isCycleStart(LocalDate firstPeriodStart, LocalDate periodStart, int validityMonths) {
        return monthsBetween(firstPeriodStart, periodStart) % validityMonths == 0;
    }

    /** Ngày bắt đầu của chu kỳ hiệu lực chứa kỳ này. */
    public static LocalDate cycleStartDate(LocalDate firstPeriodStart, LocalDate periodStart, int validityMonths) {
        long cycles = monthsBetween(firstPeriodStart, periodStart) / validityMonths;
        return firstPeriodStart.plusMonths(cycles * validityMonths);
    }

    private static long monthsBetween(LocalDate from, LocalDate to) {
        return ChronoUnit.MONTHS.between(from.withDayOfMonth(1), to.withDayOfMonth(1));
    }

    /**
     * Hoàn cọc = cọc đã lập hóa đơn - nợ còn lại - phí thay thế chưa lập hóa đơn, tối thiểu 0.
     */
    public static BigDecimal refund(BigDecimal billedDeposit, BigDecimal openDue, BigDecimal unbilledCharges) {
        return round(billedDeposit.subtract(openDue).subtract(unbilledCharges).max(BigDecimal.ZERO));
    }

    private static BigDecimal afterDiscount(BigDecimal amount, BigDecimal discountPercent) {
        BigDecimal factor = HUNDRED.subtract(discountPercent).divide(HUNDRED);
        return round(amount.multiply(factor));
    }

    private static BigDecimal round(BigDecimal v) {
        return v.setScale(2, RoundingMode.HALF_UP);
    }

    private static BigDecimal zero() {
        return BigDecimal.ZERO.setScale(2);
    }
}
