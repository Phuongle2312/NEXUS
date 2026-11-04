package com.nexus.dao;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.SQLException;
import java.util.List;

/** Bản JDBC của {@link ReplacementChargeDao}. */
public class JdbcReplacementChargeDao implements ReplacementChargeDao {
    @Override
    public List<Charge> findUnbilledForUpdate(Connection c, String accountId) throws SQLException {
        return Sql.list(c, "SELECT ChargeId, Amount FROM ReplacementCharges WITH (UPDLOCK, ROWLOCK) WHERE AccountId = ? AND Status = 'UNBILLED' ORDER BY ChargeDate, ChargeId",
                rs -> new Charge(rs.getInt(1), rs.getBigDecimal(2)), accountId);
    }

    @Override
    public BigDecimal sumForBill(Connection c, int billId) throws SQLException {
        return Sql.one(c, "SELECT COALESCE(SUM(Amount), 0) FROM ReplacementCharges WHERE BillId = ?",
                rs -> rs.getBigDecimal(1), billId).orElse(BigDecimal.ZERO);
    }

    @Override
    public void markBilled(Connection c, int chargeId, int billId) throws SQLException {
        Sql.update(c, "UPDATE ReplacementCharges SET Status = 'BILLED', BillId = ? WHERE ChargeId = ?", billId, chargeId);
    }

    @Override
    public void markDepositOffset(Connection c, int chargeId) throws SQLException {
        Sql.update(c, "UPDATE ReplacementCharges SET Status = 'DEPOSIT_OFFSET' WHERE ChargeId = ?", chargeId);
    }

    @Override
    public void reduceAmount(Connection c, int chargeId, BigDecimal newAmount, String note) throws SQLException {
        Sql.update(c, "UPDATE ReplacementCharges SET Amount = ?, Note = ? WHERE ChargeId = ?", newAmount, note, chargeId);
    }
}
