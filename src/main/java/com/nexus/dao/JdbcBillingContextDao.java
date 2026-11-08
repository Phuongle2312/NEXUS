package com.nexus.dao;

import com.nexus.model.BillingContext;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Optional;

/** Bản JDBC của {@link BillingContextDao}. */
public class JdbcBillingContextDao implements BillingContextDao {
    // Landline thuộc đơn Dial-up lấy giá ở cột Landline* của đơn; còn lại lấy giá chính của đơn.
    private static final String SELECT = """
            SELECT cn.AccountId, cn.ConnType, cn.Status, cn.CustomerId, cu.FullName, cn.PlanId, p.PlanName,
                   p.ValidityMonths, p.HoursIncluded, p.OverageRatePerHour, o.DiscountPercent, cn.ActivationDate,
                   cn.DepositRefundedAt, cn.SuspendRequested,
                   CASE WHEN cn.ConnType = 'T' AND o.ConnType = 'D' THEN o.LandlinePlanFee ELSE o.PlanFee END AS ListPlanFee,
                   CASE WHEN cn.ConnType = 'T' AND o.ConnType = 'D' THEN o.LandlineDepositAmount ELSE o.DepositAmount END AS ListDeposit
            FROM Connections cn %s
            JOIN Orders o ON o.OrderId = cn.OrderId
            JOIN Plans p ON p.PlanId = cn.PlanId
            JOIN Customers cu ON cu.CustomerId = cn.CustomerId
            WHERE cn.AccountId = ?
            """;

    private static BillingContext map(ResultSet rs) throws SQLException {
        BillingContext x = new BillingContext();
        x.setAccountId(rs.getString("AccountId"));
        x.setConnType(rs.getString("ConnType"));
        x.setStatus(rs.getString("Status"));
        x.setCustomerId(rs.getInt("CustomerId"));
        x.setCustomerName(rs.getString("FullName"));
        x.setPlanId(rs.getInt("PlanId"));
        x.setPlanName(rs.getString("PlanName"));
        x.setValidityMonths(rs.getInt("ValidityMonths"));
        x.setHoursIncluded((Integer) rs.getObject("HoursIncluded"));
        x.setOverageRatePerHour(rs.getBigDecimal("OverageRatePerHour"));
        x.setDiscountPercent(rs.getBigDecimal("DiscountPercent"));
        x.setListPlanFee(rs.getBigDecimal("ListPlanFee"));
        x.setListDeposit(rs.getBigDecimal("ListDeposit"));
        x.setActivationDate(rs.getObject("ActivationDate", LocalDate.class));
        x.setDepositRefunded(rs.getObject("DepositRefundedAt") != null);
        x.setSuspendRequested(rs.getBoolean("SuspendRequested"));
        return x;
    }

    @Override
    public Optional<BillingContext> find(Connection c, String accountId) throws SQLException {
        return Sql.one(c, SELECT.formatted(""), JdbcBillingContextDao::map, accountId);
    }

    @Override
    public Optional<BillingContext> findForUpdate(Connection c, String accountId) throws SQLException {
        return Sql.one(c, SELECT.formatted("WITH (UPDLOCK, ROWLOCK)"), JdbcBillingContextDao::map, accountId);
    }

    @Override
    public Map<String, BigDecimal> callRates(Connection c, int planId) throws SQLException {
        Map<String, BigDecimal> rates = new LinkedHashMap<>();
        Sql.list(c, "SELECT CallType, RatePerMin FROM PlanCallRates WHERE PlanId = ? ORDER BY CallType", rs -> {
            rates.put(rs.getString(1), rs.getBigDecimal(2));
            return null;
        }, planId);
        return rates;
    }

    @Override
    public int requestSuspend(Connection c, String accountId, String reason) throws SQLException {
        return Sql.update(c, "UPDATE Connections SET SuspendRequested = 1, SuspendRequestReason = ?, SuspendRequestedAt = SYSDATETIME() WHERE AccountId = ?",
                reason, accountId);
    }

    @Override
    public int markDepositRefunded(Connection c, String accountId, BigDecimal amount, LocalDate date) throws SQLException {
        return Sql.update(c, "UPDATE Connections SET DepositRefundAmount = ?, DepositRefundedAt = ? WHERE AccountId = ?",
                amount, date, accountId);
    }
}
