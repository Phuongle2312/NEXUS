package com.nexus.service;

import com.nexus.dao.AuditDao;
import com.nexus.dao.BillDao;
import com.nexus.dao.BillingContextDao;
import com.nexus.dao.PaymentDao;
import com.nexus.dao.ReplacementChargeDao;
import com.nexus.model.Bill;
import com.nexus.model.BillingContext;
import com.nexus.model.Page;
import com.nexus.model.Payment;
import com.nexus.model.SessionUser;
import com.nexus.util.TxRunner;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.List;
import java.util.Set;

/** Ghi nhận thanh toán và hoàn cọc (FR-RET-06, FR-ACC-03, FR-ACC-06). */
public class PaymentService {
    private static final Set<String> METHODS = Set.of("CASH", "CARD", "TRANSFER");
    private static final String OFFSET_METHOD = "DEPOSIT_OFFSET";

    private final TxRunner tx;
    private final PaymentDao payments;
    private final BillDao bills;
    private final BillingContextDao contexts;
    private final ReplacementChargeDao charges;
    private final AuditDao audit;

    public PaymentService(TxRunner tx, PaymentDao payments, BillDao bills, BillingContextDao contexts,
                          ReplacementChargeDao charges, AuditDao audit) {
        this.tx = tx;
        this.payments = payments;
        this.bills = bills;
        this.contexts = contexts;
        this.charges = charges;
        this.audit = audit;
    }

    /** Kết quả hoàn cọc để hiển thị. */
    public record RefundResult(BigDecimal billedDeposit, BigDecimal offset, BigDecimal refund) {
    }

    /**
     * Ghi một thanh toán: khóa dòng hóa đơn trước khi cộng tiền, nên hai người ghi cùng lúc
     * không thể làm số đã trả vượt tổng hóa đơn.
     */
    public int record(SessionUser actor, int billId, BigDecimal amount, String method, LocalDate paidDate, String note) {
        if (amount.signum() <= 0) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số tiền phải lớn hơn 0.");
        }
        if (!METHODS.contains(method)) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Hình thức thanh toán không hợp lệ.");
        }
        if (paidDate.isAfter(LocalDate.now())) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Ngày thanh toán không được ở tương lai.");
        }
        return tx.inTransaction(c -> {
            Bill bill = bills.findForUpdate(c, billId)
                    .orElseThrow(() -> new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hóa đơn."));
            if (amount.compareTo(bill.getDueAmount()) > 0) {
                throw new BusinessException(ErrorCode.PAYMENT_EXCEEDS_DUE,
                        "Số tiền " + amount + " vượt số còn nợ " + bill.getDueAmount() + ".");
            }
            int id = insertAndApply(c, bill, amount, method, paidDate, note, actor.getEmployeeId());
            audit.log(c, actor.getUserId(), "RECORD_PAYMENT", "Payment", String.valueOf(id), "Bill " + billId + ": " + amount);
            return id;
        });
    }

    /** Thêm dòng thanh toán và cập nhật số đã trả, còn nợ, trạng thái của hóa đơn. */
    private int insertAndApply(Connection c, Bill bill, BigDecimal amount, String method, LocalDate date, String note, int employeeId)
            throws SQLException {
        Payment payment = new Payment();
        payment.setBillId(bill.getId());
        payment.setAmount(amount);
        payment.setMethod(method);
        payment.setPaidDate(date);
        payment.setNote(note);
        int id = payments.insert(c, payment, employeeId);
        BigDecimal paid = bill.getPaidAmount().add(amount);
        BigDecimal due = bill.getDueAmount().subtract(amount);
        bills.applyPayment(c, bill.getId(), paid, due, due.signum() == 0 ? "PAID" : "PARTIAL");
        return id;
    }

    /**
     * Hoàn cọc khi kết nối đã ngưng vĩnh viễn, mỗi kết nối một lần:
     * hoàn = cọc đã lập hóa đơn - nợ còn lại - phí thay thế chưa lập hóa đơn, tối thiểu 0.
     *
     * Phần nợ đã trừ vào cọc được bù trừ thật trên sổ (thanh toán DEPOSIT_OFFSET, phí chuyển
     * trạng thái), nếu không khách vừa bị trừ cọc vừa còn nợ, tức bị thu hai lần.
     */
    public RefundResult refundDeposit(SessionUser actor, String accountId) {
        return tx.inTransaction(c -> {
            BillingContext ctx = contexts.findForUpdate(c, accountId)
                    .orElseThrow(() -> new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, "Không tìm thấy Account ID."));
            if (!"PERM_INACTIVE".equals(ctx.getStatus())) {
                throw new BusinessException(ErrorCode.CONNECTION_STATE_INVALID, "Chỉ hoàn cọc khi kết nối đã ngưng vĩnh viễn.");
            }
            if (ctx.isDepositRefunded()) {
                throw new BusinessException(ErrorCode.DEPOSIT_ALREADY_REFUNDED, "Tiền đặt cọc của kết nối đã được hoàn.");
            }

            BigDecimal billedDeposit = bills.sumBilledDeposit(c, accountId);
            List<Bill> openBills = bills.findOpenForUpdate(c, accountId);
            List<ReplacementChargeDao.Charge> unbilled = charges.findUnbilledForUpdate(c, accountId);
            BigDecimal openDue = sumDue(openBills);
            BigDecimal unbilledTotal = sumCharges(unbilled);
            BigDecimal refund = BillingCalculator.refund(billedDeposit, openDue, unbilledTotal);

            LocalDate today = LocalDate.now();
            BigDecimal remaining = billedDeposit;
            remaining = offsetBills(c, openBills, remaining, today, actor.getEmployeeId());
            remaining = offsetCharges(c, unbilled, remaining);

            contexts.markDepositRefunded(c, accountId, refund, today);
            audit.log(c, actor.getUserId(), "REFUND_DEPOSIT", "Connection", accountId,
                    "Coc " + billedDeposit + ", bu tru " + billedDeposit.subtract(remaining) + ", hoan " + refund);
            return new RefundResult(billedDeposit, billedDeposit.subtract(remaining), refund);
        });
    }

    /** Dùng cọc trả các hóa đơn còn nợ, cũ nhất trước; trả về phần cọc còn lại. */
    private BigDecimal offsetBills(Connection c, List<Bill> openBills, BigDecimal deposit, LocalDate date, int employeeId)
            throws SQLException {
        BigDecimal remaining = deposit;
        for (Bill bill : openBills) {
            if (remaining.signum() == 0) {
                break;
            }
            BigDecimal used = remaining.min(bill.getDueAmount());
            insertAndApply(c, bill, used, OFFSET_METHOD, date, "Bu tru tu tien coc khi hoan coc", employeeId);
            remaining = remaining.subtract(used);
        }
        return remaining;
    }

    /** Dùng phần cọc còn lại trả phí thay thế chưa lập hóa đơn; không đủ thì giảm số tiền của khoản đó. */
    private BigDecimal offsetCharges(Connection c, List<ReplacementChargeDao.Charge> unbilled, BigDecimal deposit)
            throws SQLException {
        BigDecimal remaining = deposit;
        for (ReplacementChargeDao.Charge charge : unbilled) {
            if (remaining.signum() == 0) {
                break;
            }
            if (remaining.compareTo(charge.amount()) >= 0) {
                charges.markDepositOffset(c, charge.id());
                remaining = remaining.subtract(charge.amount());
            } else {
                charges.reduceAmount(c, charge.id(), charge.amount().subtract(remaining),
                        "Da bu tru " + remaining + " tu tien coc");
                remaining = BigDecimal.ZERO;
            }
        }
        return remaining;
    }

    /** Hóa đơn để hiển thị trên form ghi nhận. */
    public Bill billForPayment(int billId) {
        return tx.read(c -> bills.findById(c, billId))
                .orElseThrow(() -> new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hóa đơn."));
    }

    public Payment receipt(int paymentId) {
        return tx.read(c -> payments.findById(c, paymentId))
                .orElseThrow(() -> new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy biên lai."));
    }

    public Page<Payment> search(PaymentDao.Filter filter, int page, int size) {
        return tx.read(c -> payments.search(c, filter, page, size));
    }

    private static BigDecimal sumDue(List<Bill> list) {
        BigDecimal sum = BigDecimal.ZERO;
        for (Bill bill : list) {
            sum = sum.add(bill.getDueAmount());
        }
        return sum;
    }

    private static BigDecimal sumCharges(List<ReplacementChargeDao.Charge> list) {
        BigDecimal sum = BigDecimal.ZERO;
        for (ReplacementChargeDao.Charge charge : list) {
            sum = sum.add(charge.amount());
        }
        return sum;
    }
}
