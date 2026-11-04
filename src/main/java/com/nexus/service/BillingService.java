package com.nexus.service;

import com.nexus.dao.AuditDao;
import com.nexus.dao.BillDao;
import com.nexus.dao.BillingContextDao;
import com.nexus.dao.ConfigDao;
import com.nexus.dao.ReplacementChargeDao;
import com.nexus.model.Bill;
import com.nexus.model.BillCallLine;
import com.nexus.model.BillingContext;
import com.nexus.model.Page;
import com.nexus.model.SessionUser;
import com.nexus.util.TxRunner;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

/**
 * Lập, xem trước, sửa hóa đơn và xử lý quá hạn (FR-ACC-01, 02, 04).
 * Preview, generate và update dùng chung hàm {@link #compute}, nên số xem trước luôn khớp số lưu.
 */
public class BillingService {
    private final TxRunner tx;
    private final BillDao bills;
    private final BillingContextDao contexts;
    private final ReplacementChargeDao charges;
    private final ConfigDao config;
    private final AuditDao audit;

    public BillingService(TxRunner tx, BillDao bills, BillingContextDao contexts, ReplacementChargeDao charges,
                          ConfigDao config, AuditDao audit) {
        this.tx = tx;
        this.bills = bills;
        this.contexts = contexts;
        this.charges = charges;
        this.config = config;
        this.audit = audit;
    }

    /** Thông tin kết nối và đơn giá cho form lập hóa đơn. */
    public BillingContext context(String accountId) {
        return tx.read(c -> contexts.find(c, accountId)).orElseThrow(BillingService::accountNotFound);
    }

    public Map<String, BigDecimal> callRates(int planId) {
        return tx.read(c -> contexts.callRates(c, planId));
    }

    /** Tính thử, không lưu gì. */
    public Bill preview(BillForm form) {
        return tx.read(c -> {
            BillingContext ctx = contexts.find(c, form.accountId()).orElseThrow(BillingService::accountNotFound);
            checkPeriod(c, ctx, form);
            // Kết nối đang auto-commit nên khóa dòng của câu đọc này nhả ngay sau câu lệnh.
            BigDecimal unbilled = sumAmounts(charges.findUnbilledForUpdate(c, form.accountId()));
            return compute(c, ctx, form, unbilled, 0);
        });
    }

    /**
     * Lập hóa đơn trong một transaction: khóa kết nối, kiểm tra kỳ, tính tiền, lưu,
     * rồi đánh dấu các khoản phí thay thế đã vào hóa đơn này.
     */
    public int generate(SessionUser actor, BillForm form) {
        return tx.inTransaction(c -> {
            BillingContext ctx = contexts.findForUpdate(c, form.accountId()).orElseThrow(BillingService::accountNotFound);
            checkPeriod(c, ctx, form);
            List<ReplacementChargeDao.Charge> unbilled = charges.findUnbilledForUpdate(c, form.accountId());
            Bill bill = compute(c, ctx, form, sumAmounts(unbilled), 0);
            int id = bills.insert(c, bill, actor.getEmployeeId());
            bill.setId(id);
            bills.replaceCallLines(c, bill);
            for (ReplacementChargeDao.Charge charge : unbilled) {
                charges.markBilled(c, charge.id(), id);
            }
            audit.log(c, actor.getUserId(), "GENERATE_BILL", "Bill", String.valueOf(id),
                    form.accountId() + " " + form.periodStart() + ".." + form.periodEnd());
            return id;
        });
    }

    /**
     * Sửa hóa đơn. Chỉ sửa được hóa đơn chưa có thanh toán và là hóa đơn mới nhất của kết nối,
     * vì nợ kỳ trước và giờ lũy kế của các kỳ sau được tính từ hóa đơn này.
     */
    public void update(SessionUser actor, int billId, BillForm form) {
        tx.inTransaction(c -> {
            Bill current = bills.findById(c, billId).orElseThrow(BillingService::billNotFound);
            BillingContext ctx = contexts.findForUpdate(c, current.getAccountId()).orElseThrow(BillingService::accountNotFound);
            // Đọc lại sau khi đã khóa kết nối để không sót thanh toán vừa ghi.
            current = bills.findForUpdate(c, billId).orElseThrow(BillingService::billNotFound);
            if (current.getPaidAmount().signum() != 0) {
                throw new BusinessException(ErrorCode.BILL_STATE_INVALID, "Hóa đơn đã có thanh toán nên không sửa được.");
            }
            LocalDate last = bills.lastPeriodEnd(c, current.getAccountId()).orElseThrow();
            if (!last.equals(current.getPeriodEnd())) {
                throw new BusinessException(ErrorCode.BILL_STATE_INVALID, "Chỉ sửa được hóa đơn mới nhất của kết nối.");
            }
            // Kỳ cước và ngày lập giữ nguyên; chỉ các số liệu nhập tay thay đổi.
            BillForm sameTerm = new BillForm(current.getAccountId(), current.getPeriodStart(), current.getPeriodEnd(),
                    form.minutes(), form.usageHours(), form.adjustment(), form.note());
            List<ReplacementChargeDao.Charge> unbilled = charges.findUnbilledForUpdate(c, current.getAccountId());
            BigDecimal replacement = charges.sumForBill(c, billId).add(sumAmounts(unbilled));
            Bill updated = compute(c, ctx, sameTerm, replacement, billId);
            updated.setId(billId);
            bills.update(c, updated);
            bills.replaceCallLines(c, updated);
            for (ReplacementChargeDao.Charge charge : unbilled) {
                charges.markBilled(c, charge.id(), billId);
            }
            audit.log(c, actor.getUserId(), "UPDATE_BILL", "Bill", String.valueOf(billId), null);
            return null;
        });
    }

    /** Xem hóa đơn; khách chỉ xem được của mình, hóa đơn người khác coi như không tồn tại. */
    public Bill view(SessionUser actor, int billId) {
        Bill bill = tx.read(c -> bills.findById(c, billId)).orElseThrow(BillingService::billNotFound);
        if ("CUSTOMER".equals(actor.getRole()) && !Integer.valueOf(bill.getCustomerId()).equals(actor.getCustomerId())) {
            throw billNotFound();
        }
        return bill;
    }

    public Page<Bill> search(BillDao.Filter filter, int page, int size) {
        return tx.read(c -> bills.search(c, filter, page, size));
    }

    /** Hóa đơn quá hạn: hạn thanh toán đã qua hơn số ngày cho trước; null thì lấy theo cấu hình. */
    public List<Bill> overdue(Integer days) {
        return tx.read(c -> {
            int d = days != null ? days : Integer.parseInt(config.get(c, "OVERDUE_DAYS"));
            return bills.findOverdue(c, LocalDate.now().minusDays(d));
        });
    }

    /** Kế toán đề nghị tạm ngưng; không đổi trạng thái kết nối, việc đó do Kỹ thuật làm (FR-ACC-04). */
    public void requestSuspend(SessionUser actor, String accountId, String reason) {
        tx.inTransaction(c -> {
            BillingContext ctx = contexts.findForUpdate(c, accountId).orElseThrow(BillingService::accountNotFound);
            if (!"ACTIVE".equals(ctx.getStatus())) {
                throw new BusinessException(ErrorCode.CONNECTION_STATE_INVALID, "Chỉ đề nghị tạm ngưng kết nối đang hoạt động.");
            }
            contexts.requestSuspend(c, accountId, reason);
            audit.log(c, actor.getUserId(), "REQUEST_SUSPEND", "Connection", accountId, reason);
            return null;
        });
    }

    /**
     * Kiểm tra kỳ cước: tối đa một tháng, không trước ngày kích hoạt, và phải sau kỳ mới nhất.
     * Điều kiện "sau kỳ mới nhất" chặn cả trùng lẫn chồng lấn, đồng thời giữ đúng thứ tự kỳ đầu.
     */
    private void checkPeriod(Connection c, BillingContext ctx, BillForm form) throws SQLException {
        LocalDate start = form.periodStart();
        LocalDate end = form.periodEnd();
        if (end.isBefore(start) || end.isAfter(start.plusMonths(1))) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Kỳ cước phải hợp lệ và không dài quá một tháng.");
        }
        if (end.isBefore(ctx.getActivationDate())) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Kỳ cước kết thúc trước ngày kích hoạt kết nối.");
        }
        if (ctx.isDepositRefunded()) {
            throw new BusinessException(ErrorCode.CONNECTION_STATE_INVALID, "Kết nối đã ngưng và đã hoàn cọc, không lập thêm hóa đơn.");
        }
        Optional<LocalDate> last = bills.lastPeriodEnd(c, ctx.getAccountId());
        if (last.isPresent() && !start.isAfter(last.get())) {
            throw new BusinessException(ErrorCode.BILL_EXISTS, "Đã có hóa đơn trùng hoặc chồng lấn kỳ cước này (kỳ gần nhất kết thúc " + last.get() + ").");
        }
    }

    /** Gom dữ liệu từ CSDL rồi gọi BillingCalculator. excludeBillId khác 0 khi đang sửa hóa đơn đó. */
    private Bill compute(Connection c, BillingContext ctx, BillForm form, BigDecimal replacement, int excludeBillId)
            throws SQLException {
        // Kỳ đầu là khi kết nối chưa có hóa đơn, hoặc chính hóa đơn đang sửa là hóa đơn đầu tiên.
        Optional<LocalDate> first = bills.firstPeriodStart(c, ctx.getAccountId());
        boolean firstPeriod = first.isEmpty() || first.get().equals(form.periodStart());
        LocalDate firstStart = first.orElse(form.periodStart());

        int validity = ctx.getValidityMonths();
        boolean cycleStart = BillingCalculator.isCycleStart(firstStart, form.periodStart(), validity);
        LocalDate cycleBegin = BillingCalculator.cycleStartDate(firstStart, form.periodStart(), validity);
        BigDecimal priorHours = bills.sumUsageHoursSince(c, ctx.getAccountId(), cycleBegin, excludeBillId);

        List<BillingCalculator.CallUsage> calls = callUsage(c, ctx, form);
        BigDecimal taxRate = new BigDecimal(config.get(c, "TAX_RATE"));
        BigDecimal previousDue = bills.sumOpenDue(c, ctx.getAccountId(), excludeBillId);

        BillingCalculator.Result r = BillingCalculator.calculate(new BillingCalculator.Input(
                firstPeriod, cycleStart, ctx.getListPlanFee(), ctx.getDiscountPercent(), ctx.getListDeposit(), calls,
                form.usageHours(), priorHours, ctx.getHoursIncluded(), ctx.getOverageRatePerHour(),
                replacement, form.adjustment(), taxRate, previousDue));

        LocalDate today = LocalDate.now();
        Bill bill = new Bill();
        bill.setAccountId(ctx.getAccountId());
        bill.setCustomerId(ctx.getCustomerId());
        bill.setCustomerName(ctx.getCustomerName());
        bill.setPeriodStart(form.periodStart());
        bill.setPeriodEnd(form.periodEnd());
        bill.setPlanFee(r.planFee());
        bill.setCallCharges(r.callCharges());
        bill.setUsageHours(form.usageHours());
        bill.setOverageCharges(r.overageCharges());
        bill.setReplacementCharges(r.replacementCharges());
        bill.setAdjustment(r.adjustment());
        bill.setTaxableBase(r.taxableBase());
        bill.setTaxRate(taxRate);
        bill.setTaxAmount(r.taxAmount());
        bill.setDepositAmount(r.depositAmount());
        bill.setTotalAmount(r.totalAmount());
        bill.setPaidAmount(BigDecimal.ZERO.setScale(2));
        bill.setDueAmount(r.totalAmount());
        bill.setPreviousDue(r.previousDue());
        bill.setBillDate(today);
        bill.setDueDate(today.plusDays(Integer.parseInt(config.get(c, "BILL_DUE_DAYS"))));
        // Đơn giảm 100% có thể ra hóa đơn 0 đồng, khi đó không còn gì phải trả.
        bill.setStatus(r.totalAmount().signum() == 0 ? "PAID" : "UNPAID");
        bill.setNote(form.note());
        bill.setCallLines(toLines(calls));
        return bill;
    }

    /** Ghép số phút nhập vào với đơn giá của gói. Nhập cước cho loại gói không có thì báo lỗi, không bỏ qua âm thầm. */
    private List<BillingCalculator.CallUsage> callUsage(Connection c, BillingContext ctx, BillForm form) throws SQLException {
        Map<String, BigDecimal> rates = contexts.callRates(c, ctx.getPlanId());
        List<BillingCalculator.CallUsage> calls = new ArrayList<>();
        for (Map.Entry<String, BigDecimal> entry : form.minutes().entrySet()) {
            if (entry.getValue().signum() == 0) {
                continue;
            }
            BigDecimal rate = rates.get(entry.getKey());
            if (rate == null) {
                throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Gói cước không có loại cuộc gọi " + entry.getKey() + ".");
            }
            calls.add(new BillingCalculator.CallUsage(entry.getKey(), entry.getValue(), rate));
        }
        return calls;
    }

    private static List<BillCallLine> toLines(List<BillingCalculator.CallUsage> calls) {
        List<BillCallLine> lines = new ArrayList<>();
        for (BillingCalculator.CallUsage call : calls) {
            lines.add(new BillCallLine(call.callType(), call.minutes(), call.ratePerMin(), BillingCalculator.lineAmount(call)));
        }
        return lines;
    }

    private static BigDecimal sumAmounts(List<ReplacementChargeDao.Charge> list) {
        BigDecimal sum = BigDecimal.ZERO;
        for (ReplacementChargeDao.Charge charge : list) {
            sum = sum.add(charge.amount());
        }
        return sum;
    }

    private static BusinessException accountNotFound() {
        return new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, "Không tìm thấy Account ID.");
    }

    private static BusinessException billNotFound() {
        return new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hóa đơn.");
    }
}
