package com.nexus.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import com.nexus.dao.BillDao;
import com.nexus.dao.BillingContextDao;
import com.nexus.dao.PaymentDao;
import com.nexus.dao.ReplacementChargeDao;
import com.nexus.model.Bill;
import com.nexus.model.BillingContext;
import com.nexus.model.Page;
import com.nexus.model.Payment;
import com.nexus.model.SessionUser;
import com.nexus.util.DbUtil;
import com.nexus.util.TxRunner;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

/**
 * Kiểm tra PaymentService với DAO giả, không cần CSDL.
 * Làm được vì Service chỉ phụ thuộc vào interface (nguyên tắc D trong SOLID).
 */
class PaymentServiceTest {
    private static final SessionUser ACCOUNTS = new SessionUser(1, "acc", "ACCOUNTS", 2, null, "Ke toan", false);
    private static final String ACCOUNT = "T001000000000005";

    /** Chạy thẳng công việc, không có transaction thật. */
    private static final TxRunner DIRECT_TX = new TxRunner() {
        @Override
        public <T> T inTransaction(DbUtil.TxWork<T> work) {
            return read(work);
        }

        @Override
        public <T> T read(DbUtil.TxWork<T> work) {
            try {
                return work.run(null);
            } catch (SQLException e) {
                throw new IllegalStateException(e);
            }
        }
    };

    private final List<Bill> billStore = new ArrayList<>();
    private final List<Payment> paymentStore = new ArrayList<>();
    private final List<ReplacementChargeDao.Charge> chargeStore = new ArrayList<>();
    private final List<Integer> offsetCharges = new ArrayList<>();
    private BillingContext context;
    private BigDecimal refundedAmount;
    private BigDecimal reducedChargeTo;
    private PaymentService service;

    private static BigDecimal d(String v) {
        return new BigDecimal(v);
    }

    private Bill addBill(int id, String total, String paid, String deposit) {
        Bill b = new Bill();
        b.setId(id);
        b.setTotalAmount(d(total));
        b.setPaidAmount(d(paid));
        b.setDueAmount(d(total).subtract(d(paid)));
        b.setDepositAmount(d(deposit));
        billStore.add(b);
        return b;
    }

    @BeforeEach
    void setUp() {
        context = new BillingContext();
        context.setAccountId(ACCOUNT);
        context.setStatus("PERM_INACTIVE");
        service = new PaymentService(DIRECT_TX, new FakePayments(), new FakeBills(), new FakeContexts(), new FakeCharges(),
                (c, actor, action, type, id, detail) -> { });
    }

    @Test
    void partialThenFullPaymentUpdatesStatus() {
        Bill bill = addBill(1, "100.00", "0.00", "0.00");
        service.record(ACCOUNTS, 1, d("30.00"), "CASH", LocalDate.now(), null);
        assertEquals(d("70.00"), bill.getDueAmount());
        assertEquals("PARTIAL", bill.getStatus());
        service.record(ACCOUNTS, 1, d("70.00"), "CARD", LocalDate.now(), null);
        assertEquals(d("0.00"), bill.getDueAmount());
        assertEquals("PAID", bill.getStatus());
    }

    @Test
    void paymentAboveDueIsRejected() {
        addBill(1, "100.00", "90.00", "0.00");
        BusinessException e = assertThrows(BusinessException.class,
                () -> service.record(ACCOUNTS, 1, d("10.01"), "CASH", LocalDate.now(), null));
        assertEquals(ErrorCode.PAYMENT_EXCEEDS_DUE, e.getCode());
        assertEquals(0, paymentStore.size());
    }

    @Test
    void invalidPaymentInputIsRejected() {
        addBill(1, "100.00", "0.00", "0.00");
        LocalDate today = LocalDate.now();
        assertThrows(BusinessException.class, () -> service.record(ACCOUNTS, 1, d("0.00"), "CASH", today, null));
        assertThrows(BusinessException.class, () -> service.record(ACCOUNTS, 1, d("1.00"), "DEPOSIT_OFFSET", today, null),
                "Người dùng không được tự chọn hình thức bù trừ cọc");
        assertThrows(BusinessException.class, () -> service.record(ACCOUNTS, 1, d("1.00"), "CASH", today.plusDays(1), null));
    }

    @Test
    void refundNetsDueAndChargesAndSettlesThemSoCustomerIsNotChargedTwice() {
        addBill(1, "752.54", "752.54", "500.00");
        Bill open = addBill(2, "140.30", "0.00", "0.00");
        chargeStore.add(new ReplacementChargeDao.Charge(7, d("40.00")));

        PaymentService.RefundResult r = service.refundDeposit(ACCOUNTS, ACCOUNT);

        assertEquals(d("319.70"), r.refund());
        assertEquals(d("319.70"), refundedAmount);
        assertEquals(d("0.00"), open.getDueAmount(), "Nợ đã trừ vào cọc thì hóa đơn phải hết nợ");
        assertEquals("PAID", open.getStatus());
        assertEquals("DEPOSIT_OFFSET", paymentStore.get(0).getMethod());
        assertEquals(List.of(7), offsetCharges);
    }

    @Test
    void refundIsZeroAndDebtRemainsWhenDueExceedsDeposit() {
        Bill first = addBill(1, "300.00", "0.00", "250.00");
        Bill second = addBill(2, "100.00", "0.00", "0.00");
        chargeStore.add(new ReplacementChargeDao.Charge(7, d("40.00")));

        PaymentService.RefundResult r = service.refundDeposit(ACCOUNTS, ACCOUNT);

        assertEquals(d("0.00"), r.refund());
        assertEquals(d("50.00"), first.getDueAmount(), "Cọc 250 trả cho hóa đơn cũ nhất trước");
        assertEquals(d("100.00"), second.getDueAmount());
        assertEquals(List.of(), offsetCharges);
    }

    @Test
    void chargePartlyCoveredByDepositIsReduced() {
        addBill(1, "250.00", "250.00", "30.00");
        chargeStore.add(new ReplacementChargeDao.Charge(7, d("40.00")));

        PaymentService.RefundResult r = service.refundDeposit(ACCOUNTS, ACCOUNT);

        assertEquals(d("0.00"), r.refund());
        assertEquals(d("10.00"), reducedChargeTo);
    }

    @Test
    void refundRequiresPermanentlyInactiveAndOnlyOnce() {
        context.setStatus("ACTIVE");
        assertEquals(ErrorCode.CONNECTION_STATE_INVALID,
                assertThrows(BusinessException.class, () -> service.refundDeposit(ACCOUNTS, ACCOUNT)).getCode());

        context.setStatus("PERM_INACTIVE");
        context.setDepositRefunded(true);
        assertEquals(ErrorCode.DEPOSIT_ALREADY_REFUNDED,
                assertThrows(BusinessException.class, () -> service.refundDeposit(ACCOUNTS, ACCOUNT)).getCode());
    }

    private class FakePayments implements PaymentDao {
        @Override
        public int insert(Connection c, Payment payment, int employeeId) {
            paymentStore.add(payment);
            return paymentStore.size();
        }

        @Override
        public Optional<Payment> findById(Connection c, int id) {
            return Optional.empty();
        }

        @Override
        public Page<Payment> search(Connection c, Filter filter, int page, int size) {
            throw new UnsupportedOperationException();
        }
    }

    private class FakeBills implements BillDao {
        private Optional<Bill> byId(int id) {
            return billStore.stream().filter(b -> b.getId() == id).findFirst();
        }

        @Override
        public Optional<Bill> findById(Connection c, int id) {
            return byId(id);
        }

        @Override
        public Optional<Bill> findForUpdate(Connection c, int id) {
            return byId(id);
        }

        @Override
        public Page<Bill> search(Connection c, Filter filter, int page, int size) {
            throw new UnsupportedOperationException();
        }

        @Override
        public List<Bill> findOverdue(Connection c, LocalDate cutoff) {
            throw new UnsupportedOperationException();
        }

        @Override
        public Optional<LocalDate> firstPeriodStart(Connection c, String accountId) {
            throw new UnsupportedOperationException();
        }

        @Override
        public Optional<LocalDate> lastPeriodEnd(Connection c, String accountId) {
            throw new UnsupportedOperationException();
        }

        @Override
        public BigDecimal sumUsageHoursSince(Connection c, String accountId, LocalDate from, int excludeBillId) {
            throw new UnsupportedOperationException();
        }

        @Override
        public BigDecimal sumOpenDue(Connection c, String accountId, int excludeBillId) {
            throw new UnsupportedOperationException();
        }

        @Override
        public BigDecimal sumBilledDeposit(Connection c, String accountId) {
            return billStore.stream().map(Bill::getDepositAmount).reduce(BigDecimal.ZERO, BigDecimal::add);
        }

        @Override
        public List<Bill> findOpenForUpdate(Connection c, String accountId) {
            return billStore.stream().filter(b -> b.getDueAmount().signum() > 0).toList();
        }

        @Override
        public int insert(Connection c, Bill bill, Integer employeeId) {
            throw new UnsupportedOperationException();
        }

        @Override
        public int update(Connection c, Bill bill) {
            throw new UnsupportedOperationException();
        }

        @Override
        public int applyPayment(Connection c, int billId, BigDecimal paid, BigDecimal due, String status) {
            Bill bill = byId(billId).orElseThrow();
            bill.setPaidAmount(paid);
            bill.setDueAmount(due);
            bill.setStatus(status);
            return 1;
        }

        @Override
        public void replaceCallLines(Connection c, Bill bill) {
            throw new UnsupportedOperationException();
        }
    }

    private class FakeContexts implements BillingContextDao {
        @Override
        public Optional<BillingContext> find(Connection c, String accountId) {
            return Optional.of(context);
        }

        @Override
        public Optional<BillingContext> findForUpdate(Connection c, String accountId) {
            return Optional.of(context);
        }

        @Override
        public Map<String, BigDecimal> callRates(Connection c, int planId) {
            return Map.of();
        }

        @Override
        public int requestSuspend(Connection c, String accountId, String reason) {
            return 1;
        }

        @Override
        public int markDepositRefunded(Connection c, String accountId, BigDecimal amount, LocalDate date) {
            refundedAmount = amount;
            return 1;
        }
    }

    private class FakeCharges implements ReplacementChargeDao {
        @Override
        public List<Charge> findUnbilledForUpdate(Connection c, String accountId) {
            return chargeStore;
        }

        @Override
        public BigDecimal sumForBill(Connection c, int billId) {
            return BigDecimal.ZERO;
        }

        @Override
        public void markBilled(Connection c, int chargeId, int billId) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void markDepositOffset(Connection c, int chargeId) {
            offsetCharges.add(chargeId);
        }

        @Override
        public void reduceAmount(Connection c, int chargeId, BigDecimal newAmount, String note) {
            reducedChargeTo = newAmount;
        }
    }
}
