package com.nexus.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.nexus.service.BillingCalculator.CallUsage;
import com.nexus.service.BillingCalculator.Input;
import com.nexus.service.BillingCalculator.Result;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;

/** Kiểm tra công thức hóa đơn theo các ví dụ trong SRS 5.7 và API Design 4.7. */
class BillingCalculatorTest {
    private static final BigDecimal TAX = new BigDecimal("0.1224");

    private static BigDecimal d(String v) {
        return new BigDecimal(v);
    }

    /** Hóa đơn kỳ đầu không có phát sinh nào khác. */
    private static Input firstBill(String plan, String discount, String deposit) {
        return new Input(true, true, d(plan), d(discount), d(deposit), List.of(),
                d("0"), d("0"), null, null, d("0"), d("0"), TAX, d("0"));
    }

    @Test
    void firstBillNoDiscountMatchesSrsExample() {
        Result r = BillingCalculator.calculate(firstBill("225", "0", "500"));
        assertEquals(d("225.00"), r.planFee());
        assertEquals(d("27.54"), r.taxAmount());
        assertEquals(d("500.00"), r.depositAmount());
        assertEquals(d("752.54"), r.totalAmount());
    }

    @Test
    void firstBillWith25PercentDiscountMatchesSrsExample() {
        Result r = BillingCalculator.calculate(firstBill("225", "25", "500"));
        assertEquals(d("168.75"), r.planFee());
        assertEquals(d("20.66"), r.taxAmount(), "20,655 phải làm tròn HALF_UP lên 20,66");
        assertEquals(d("375.00"), r.depositAmount());
        assertEquals(d("564.41"), r.totalAmount());
    }

    @Test
    void firstBillWith100PercentDiscountIsZero() {
        Result r = BillingCalculator.calculate(firstBill("225", "100", "500"));
        assertEquals(d("0.00"), r.totalAmount());
    }

    @Test
    void depositIsNotTaxed() {
        Result r = BillingCalculator.calculate(firstBill("100", "0", "325"));
        assertEquals(d("12.24"), r.taxAmount());
        assertEquals(d("437.24"), r.totalAmount());
    }

    @Test
    void regularPeriodCallChargesAndPreviousDueNotInTotal() {
        Input in = new Input(false, true, d("125"), d("25"), d("250"),
                List.of(new CallUsage("LOCAL", d("100"), d("0.70")), new CallUsage("STD", d("10"), d("2.25"))),
                d("0"), d("0"), null, null, d("0"), d("0"), TAX, d("140.30"));
        Result r = BillingCalculator.calculate(in);
        assertEquals(d("125.00"), r.planFee(), "Chu kỳ sau thu đủ giá, ưu đãi chỉ áp dụng cho advance");
        assertEquals(d("92.50"), r.callCharges());
        assertEquals(d("217.50"), r.taxableBase());
        assertEquals(d("26.62"), r.taxAmount());
        assertEquals(d("0.00"), r.depositAmount(), "Cọc chỉ thu ở kỳ đầu");
        assertEquals(d("244.12"), r.totalAmount(), "Nợ kỳ trước không cộng vào tổng");
        assertEquals(d("140.30"), r.previousDue());
        assertEquals(d("384.42"), r.amountToPay());
    }

    @Test
    void midCyclePeriodHasNoPlanFee() {
        Input in = new Input(false, false, d("400"), d("0"), d("500"), List.of(),
                d("0"), d("0"), null, null, d("0"), d("0"), TAX, d("0"));
        assertEquals(d("0.00"), BillingCalculator.calculate(in).totalAmount());
    }

    @Test
    void replacementChargeIsTaxed() {
        Input in = new Input(false, false, d("225"), d("0"), d("500"), List.of(),
                d("0"), d("0"), null, null, d("40"), d("0"), TAX, d("0"));
        Result r = BillingCalculator.calculate(in);
        assertEquals(d("40.00"), r.taxableBase());
        assertEquals(d("4.90"), r.taxAmount());
        assertEquals(d("44.90"), r.totalAmount());
    }

    @Test
    void negativeTaxableIsRejected() {
        Input in = new Input(false, false, d("225"), d("0"), d("500"), List.of(),
                d("0"), d("0"), null, null, d("0"), d("-1"), TAX, d("0"));
        assertThrows(BusinessException.class, () -> BillingCalculator.calculate(in));
    }

    @Test
    void overageIsCumulativeAcrossValidityCycle() {
        BigDecimal rate = d("4.33");
        // Gói 30 giờ / 3 tháng: tháng 1 dùng 20 giờ, chưa vượt.
        assertEquals(d("0.00"), BillingCalculator.overageCharges(d("20"), d("0"), 30, rate));
        // Tháng 2 dùng 15 giờ: lũy kế 35, vượt 5 giờ.
        assertEquals(d("21.65"), BillingCalculator.overageCharges(d("15"), d("20"), 30, rate));
        // Tháng 3 dùng 2 giờ: lũy kế 37, chỉ thu thêm 2 giờ vượt mới.
        assertEquals(d("8.66"), BillingCalculator.overageCharges(d("2"), d("35"), 30, rate));
    }

    @Test
    void unlimitedPlanHasNoOverage() {
        assertEquals(d("0.00"), BillingCalculator.overageCharges(d("500"), d("0"), null, null));
    }

    @Test
    void cycleStartFollowsValidityMonths() {
        LocalDate first = LocalDate.of(2026, 11, 1);
        assertTrue(BillingCalculator.isCycleStart(first, first, 3));
        assertFalse(BillingCalculator.isCycleStart(first, LocalDate.of(2026, 12, 1), 3));
        assertFalse(BillingCalculator.isCycleStart(first, LocalDate.of(2027, 1, 1), 3));
        assertTrue(BillingCalculator.isCycleStart(first, LocalDate.of(2027, 2, 1), 3));
        assertTrue(BillingCalculator.isCycleStart(first, LocalDate.of(2026, 12, 1), 1));
        assertEquals(LocalDate.of(2027, 2, 1),
                BillingCalculator.cycleStartDate(first, LocalDate.of(2027, 3, 1), 3));
    }

    @Test
    void refundDeductsDueAndUnbilledButNeverNegative() {
        assertEquals(d("500.00"), BillingCalculator.refund(d("500"), d("0"), d("0")));
        assertEquals(d("319.70"), BillingCalculator.refund(d("500"), d("140.30"), d("40")));
        assertEquals(d("0.00"), BillingCalculator.refund(d("250"), d("300"), d("0")));
    }
}
