package com.nexus.dao;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.SQLException;
import java.util.List;

/** Phí thay thiết bị. C tạo bản ghi UNBILLED ở task 19; Billing và Payment xử lý tiếp. */
public interface ReplacementChargeDao {

    /** Một khoản phí chưa lập hóa đơn. */
    record Charge(int id, BigDecimal amount) {
    }

    /** Các khoản UNBILLED của kết nối, cũ nhất trước, đã khóa dòng. */
    List<Charge> findUnbilledForUpdate(Connection c, String accountId) throws SQLException;

    /** Tổng phí đã gắn với hóa đơn này. */
    BigDecimal sumForBill(Connection c, int billId) throws SQLException;

    void markBilled(Connection c, int chargeId, int billId) throws SQLException;

    void markDepositOffset(Connection c, int chargeId) throws SQLException;

    /** Giảm phần còn phải thu khi tiền cọc chỉ bù được một phần khoản phí. */
    void reduceAmount(Connection c, int chargeId, BigDecimal newAmount, String note) throws SQLException;
}
