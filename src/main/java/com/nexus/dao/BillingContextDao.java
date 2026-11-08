package com.nexus.dao;

import com.nexus.model.BillingContext;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.Map;
import java.util.Optional;

/** Phần dữ liệu kết nối mà Billing và Payment cần. Việc tạo, ngưng kết nối thuộc ConnectionDao của C. */
public interface BillingContextDao {
    Optional<BillingContext> find(Connection c, String accountId) throws SQLException;

    /** Như find nhưng khóa dòng kết nối (UPDLOCK) đến hết transaction, chống lập trùng hóa đơn, hoàn cọc hai lần. */
    Optional<BillingContext> findForUpdate(Connection c, String accountId) throws SQLException;

    /** Đơn giá theo loại cuộc gọi của gói. */
    Map<String, BigDecimal> callRates(Connection c, int planId) throws SQLException;

    /** Đánh dấu kế toán đề nghị tạm ngưng; Kỹ thuật thực hiện và xóa cờ này. */
    int requestSuspend(Connection c, String accountId, String reason) throws SQLException;

    /** Ghi nhận đã hoàn cọc. */
    int markDepositRefunded(Connection c, String accountId, BigDecimal amount, LocalDate date) throws SQLException;
}
