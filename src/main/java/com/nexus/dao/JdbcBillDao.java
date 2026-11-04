package com.nexus.dao;

import com.nexus.model.Bill;
import com.nexus.model.BillCallLine;
import com.nexus.model.Page;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/** Bản JDBC của {@link BillDao}. */
public class JdbcBillDao implements BillDao {
    private static final String COLUMNS = "SELECT b.*, cn.CustomerId, cu.FullName";
    private static final String FROM = """
             FROM Bills b %s
             JOIN Connections cn ON cn.AccountId = b.AccountId
             JOIN Customers cu ON cu.CustomerId = cn.CustomerId
            """;

    private static Bill map(ResultSet rs) throws SQLException {
        Bill x = new Bill();
        x.setId(rs.getInt("BillId"));
        x.setAccountId(rs.getString("AccountId"));
        x.setCustomerId(rs.getInt("CustomerId"));
        x.setCustomerName(rs.getString("FullName"));
        x.setPeriodStart(rs.getObject("PeriodStart", LocalDate.class));
        x.setPeriodEnd(rs.getObject("PeriodEnd", LocalDate.class));
        x.setPlanFee(rs.getBigDecimal("PlanFee"));
        x.setCallCharges(rs.getBigDecimal("CallCharges"));
        x.setUsageHours(rs.getBigDecimal("UsageHours"));
        x.setOverageCharges(rs.getBigDecimal("OverageCharges"));
        x.setReplacementCharges(rs.getBigDecimal("ReplacementCharges"));
        x.setAdjustment(rs.getBigDecimal("Adjustment"));
        x.setTaxableBase(rs.getBigDecimal("TaxableBase"));
        x.setTaxRate(rs.getBigDecimal("TaxRate"));
        x.setTaxAmount(rs.getBigDecimal("TaxAmount"));
        x.setDepositAmount(rs.getBigDecimal("DepositAmount"));
        x.setTotalAmount(rs.getBigDecimal("TotalAmount"));
        x.setPaidAmount(rs.getBigDecimal("PaidAmount"));
        x.setDueAmount(rs.getBigDecimal("DueAmount"));
        x.setPreviousDue(rs.getBigDecimal("PreviousDue"));
        x.setBillDate(rs.getObject("BillDate", LocalDate.class));
        x.setDueDate(rs.getObject("DueDate", LocalDate.class));
        x.setStatus(rs.getString("Status"));
        x.setNote(rs.getString("Note"));
        return x;
    }

    /** Nạp thêm các dòng cước gọi cho hóa đơn. */
    private Bill withLines(Connection c, Bill bill) throws SQLException {
        bill.setCallLines(Sql.list(c, "SELECT * FROM BillCallUsage WHERE BillId = ? ORDER BY CallType",
                rs -> new BillCallLine(rs.getString("CallType"), rs.getBigDecimal("Minutes"),
                        rs.getBigDecimal("RatePerMin"), rs.getBigDecimal("Amount")), bill.getId()));
        return bill;
    }

    @Override
    public Optional<Bill> findById(Connection c, int id) throws SQLException {
        Optional<Bill> bill = Sql.one(c, COLUMNS + FROM.formatted("") + " WHERE b.BillId = ?", JdbcBillDao::map, id);
        if (bill.isPresent()) {
            withLines(c, bill.get());
        }
        return bill;
    }

    @Override
    public Optional<Bill> findForUpdate(Connection c, int id) throws SQLException {
        return Sql.one(c, COLUMNS + FROM.formatted("WITH (UPDLOCK, ROWLOCK)") + " WHERE b.BillId = ?", JdbcBillDao::map, id);
    }

    /** Ghép điều kiện động: chỉ tên cột cố định nằm trong SQL, giá trị luôn là tham số. */
    @Override
    public Page<Bill> search(Connection c, Filter f, int page, int size) throws SQLException {
        StringBuilder where = new StringBuilder(" WHERE 1 = 1");
        List<Object> params = new ArrayList<>();
        if (f.accountId() != null) {
            where.append(" AND b.AccountId = ?");
            params.add(f.accountId());
        }
        if (f.status() != null) {
            where.append(" AND b.Status = ?");
            params.add(f.status());
        }
        if (f.from() != null) {
            where.append(" AND b.PeriodStart >= ?");
            params.add(f.from());
        }
        if (f.to() != null) {
            where.append(" AND b.PeriodEnd <= ?");
            params.add(f.to());
        }
        String from = FROM.formatted("") + where;
        int total = Sql.count(c, "SELECT COUNT(*)" + from, params.toArray());
        params.add((page - 1) * size);
        params.add(size);
        List<Bill> items = Sql.list(c, COLUMNS + from + " ORDER BY b.PeriodStart DESC, b.BillId DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                JdbcBillDao::map, params.toArray());
        return new Page<>(items, page, size, total);
    }

    @Override
    public List<Bill> findOverdue(Connection c, LocalDate cutoff) throws SQLException {
        return Sql.list(c, COLUMNS + FROM.formatted("") + " WHERE b.Status <> 'PAID' AND b.DueDate < ? ORDER BY b.DueDate",
                JdbcBillDao::map, cutoff);
    }

    @Override
    public Optional<LocalDate> firstPeriodStart(Connection c, String accountId) throws SQLException {
        return Sql.one(c, "SELECT MIN(PeriodStart) FROM Bills WHERE AccountId = ?",
                rs -> rs.getObject(1, LocalDate.class), accountId);
    }

    @Override
    public Optional<LocalDate> lastPeriodEnd(Connection c, String accountId) throws SQLException {
        return Sql.one(c, "SELECT MAX(PeriodEnd) FROM Bills WHERE AccountId = ?",
                rs -> rs.getObject(1, LocalDate.class), accountId);
    }

    /** Lấy một giá trị tổng tiền; SUM trên tập rỗng trả NULL nên quy về 0. */
    private static BigDecimal sum(Connection c, String sql, Object... params) throws SQLException {
        return Sql.one(c, sql, rs -> rs.getBigDecimal(1), params).orElse(BigDecimal.ZERO);
    }

    @Override
    public BigDecimal sumUsageHoursSince(Connection c, String accountId, LocalDate from, int excludeBillId) throws SQLException {
        return sum(c, "SELECT COALESCE(SUM(UsageHours), 0) FROM Bills WHERE AccountId = ? AND PeriodStart >= ? AND BillId <> ?",
                accountId, from, excludeBillId);
    }

    @Override
    public BigDecimal sumOpenDue(Connection c, String accountId, int excludeBillId) throws SQLException {
        return sum(c, "SELECT COALESCE(SUM(DueAmount), 0) FROM Bills WHERE AccountId = ? AND BillId <> ?", accountId, excludeBillId);
    }

    @Override
    public BigDecimal sumBilledDeposit(Connection c, String accountId) throws SQLException {
        return sum(c, "SELECT COALESCE(SUM(DepositAmount), 0) FROM Bills WHERE AccountId = ?", accountId);
    }

    @Override
    public List<Bill> findOpenForUpdate(Connection c, String accountId) throws SQLException {
        return Sql.list(c, COLUMNS + FROM.formatted("WITH (UPDLOCK, ROWLOCK)")
                + " WHERE b.AccountId = ? AND b.Status <> 'PAID' ORDER BY b.PeriodStart", JdbcBillDao::map, accountId);
    }

    @Override
    public int insert(Connection c, Bill b, Integer employeeId) throws SQLException {
        return Sql.insert(c, """
                INSERT INTO Bills (AccountId, PeriodStart, PeriodEnd, PlanFee, CallCharges, UsageHours, OverageCharges,
                    ReplacementCharges, Adjustment, TaxableBase, TaxRate, TaxAmount, DepositAmount, TotalAmount,
                    PaidAmount, DueAmount, PreviousDue, BillDate, DueDate, Status, Note, CreatedByEmployeeId)
                VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
                """,
                b.getAccountId(), b.getPeriodStart(), b.getPeriodEnd(), b.getPlanFee(), b.getCallCharges(), b.getUsageHours(),
                b.getOverageCharges(), b.getReplacementCharges(), b.getAdjustment(), b.getTaxableBase(), b.getTaxRate(),
                b.getTaxAmount(), b.getDepositAmount(), b.getTotalAmount(), b.getPaidAmount(), b.getDueAmount(),
                b.getPreviousDue(), b.getBillDate(), b.getDueDate(), b.getStatus(), b.getNote(), employeeId);
    }

    @Override
    public int update(Connection c, Bill b) throws SQLException {
        return Sql.update(c, """
                UPDATE Bills SET PlanFee = ?, CallCharges = ?, UsageHours = ?, OverageCharges = ?, ReplacementCharges = ?,
                    Adjustment = ?, TaxableBase = ?, TaxRate = ?, TaxAmount = ?, DepositAmount = ?, TotalAmount = ?,
                    DueAmount = ?, PreviousDue = ?, Status = ?, Note = ?
                WHERE BillId = ?
                """,
                b.getPlanFee(), b.getCallCharges(), b.getUsageHours(), b.getOverageCharges(), b.getReplacementCharges(),
                b.getAdjustment(), b.getTaxableBase(), b.getTaxRate(), b.getTaxAmount(), b.getDepositAmount(), b.getTotalAmount(),
                b.getDueAmount(), b.getPreviousDue(), b.getStatus(), b.getNote(), b.getId());
    }

    @Override
    public int applyPayment(Connection c, int billId, BigDecimal paid, BigDecimal due, String status) throws SQLException {
        return Sql.update(c, "UPDATE Bills SET PaidAmount = ?, DueAmount = ?, Status = ? WHERE BillId = ?", paid, due, status, billId);
    }

    @Override
    public void replaceCallLines(Connection c, Bill b) throws SQLException {
        Sql.update(c, "DELETE FROM BillCallUsage WHERE BillId = ?", b.getId());
        for (BillCallLine line : b.getCallLines()) {
            Sql.update(c, "INSERT INTO BillCallUsage (BillId, CallType, Minutes, RatePerMin, Amount) VALUES (?,?,?,?,?)",
                    b.getId(), line.getCallType(), line.getMinutes(), line.getRatePerMin(), line.getAmount());
        }
    }
}
