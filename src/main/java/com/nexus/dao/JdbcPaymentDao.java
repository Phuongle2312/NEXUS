package com.nexus.dao;

import com.nexus.model.Page;
import com.nexus.model.Payment;

import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/** Bản JDBC của {@link PaymentDao}. */
public class JdbcPaymentDao implements PaymentDao {
    private static final String COLUMNS = "SELECT p.*, b.AccountId, cu.FullName AS CustomerName, e.FullName AS ReceivedBy";
    private static final String FROM = """
             FROM Payments p
             JOIN Bills b ON b.BillId = p.BillId
             JOIN Connections cn ON cn.AccountId = b.AccountId
             JOIN Customers cu ON cu.CustomerId = cn.CustomerId
             JOIN Employees e ON e.EmployeeId = p.ReceivedByEmployeeId
            """;

    private static Payment map(ResultSet rs) throws SQLException {
        Payment x = new Payment();
        x.setId(rs.getInt("PaymentId"));
        x.setBillId(rs.getInt("BillId"));
        x.setAccountId(rs.getString("AccountId"));
        x.setCustomerName(rs.getString("CustomerName"));
        x.setAmount(rs.getBigDecimal("Amount"));
        x.setMethod(rs.getString("Method"));
        x.setPaidDate(rs.getObject("PaidDate", LocalDate.class));
        x.setNote(rs.getString("Note"));
        x.setReceivedBy(rs.getString("ReceivedBy"));
        return x;
    }

    @Override
    public int insert(Connection c, Payment p, int employeeId) throws SQLException {
        return Sql.insert(c, "INSERT INTO Payments (BillId, Amount, Method, PaidDate, Note, ReceivedByEmployeeId) VALUES (?,?,?,?,?,?)",
                p.getBillId(), p.getAmount(), p.getMethod(), p.getPaidDate(), p.getNote(), employeeId);
    }

    @Override
    public Optional<Payment> findById(Connection c, int id) throws SQLException {
        return Sql.one(c, COLUMNS + FROM + " WHERE p.PaymentId = ?", JdbcPaymentDao::map, id);
    }

    @Override
    public Page<Payment> search(Connection c, Filter f, int page, int size) throws SQLException {
        StringBuilder where = new StringBuilder(" WHERE 1 = 1");
        List<Object> params = new ArrayList<>();
        if (f.accountId() != null) {
            where.append(" AND b.AccountId = ?");
            params.add(f.accountId());
        }
        if (f.billId() != null) {
            where.append(" AND p.BillId = ?");
            params.add(f.billId());
        }
        if (f.from() != null) {
            where.append(" AND p.PaidDate >= ?");
            params.add(f.from());
        }
        if (f.to() != null) {
            where.append(" AND p.PaidDate <= ?");
            params.add(f.to());
        }
        String from = FROM + where;
        int total = Sql.count(c, "SELECT COUNT(*)" + from, params.toArray());
        params.add((page - 1) * size);
        params.add(size);
        List<Payment> items = Sql.list(c, COLUMNS + from + " ORDER BY p.PaidDate DESC, p.PaymentId DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                JdbcPaymentDao::map, params.toArray());
        return new Page<>(items, page, size, total);
    }
}
