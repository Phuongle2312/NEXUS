package com.nexus.dao;

import com.nexus.model.Bill;
import com.nexus.model.Page;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.List;
import java.util.Optional;

/** Truy cập hóa đơn. */
public interface BillDao {

    /** Bộ lọc danh sách hóa đơn; trường null thì bỏ qua. */
    record Filter(String accountId, String status, LocalDate from, LocalDate to) {
    }

    Optional<Bill> findById(Connection c, int id) throws SQLException;

    /** Như findById nhưng khóa dòng hóa đơn đến hết transaction, chống ghi nhận thanh toán đồng thời. */
    Optional<Bill> findForUpdate(Connection c, int id) throws SQLException;

    Page<Bill> search(Connection c, Filter filter, int page, int size) throws SQLException;

    /** Hóa đơn còn nợ có hạn thanh toán trước ngày cutoff. */
    List<Bill> findOverdue(Connection c, LocalDate cutoff) throws SQLException;

    /** Kỳ đầu tiên của kết nối; rỗng nếu chưa có hóa đơn. */
    Optional<LocalDate> firstPeriodStart(Connection c, String accountId) throws SQLException;

    /** Ngày cuối của kỳ mới nhất; rỗng nếu chưa có hóa đơn. */
    Optional<LocalDate> lastPeriodEnd(Connection c, String accountId) throws SQLException;

    /** Tổng giờ đã dùng từ ngày from, không tính hóa đơn excludeBillId (0 = không loại trừ). */
    BigDecimal sumUsageHoursSince(Connection c, String accountId, LocalDate from, int excludeBillId) throws SQLException;

    /** Tổng số còn nợ của các hóa đơn, không tính hóa đơn excludeBillId (0 = không loại trừ). */
    BigDecimal sumOpenDue(Connection c, String accountId, int excludeBillId) throws SQLException;

    /** Tổng tiền cọc đã lập hóa đơn của kết nối. */
    BigDecimal sumBilledDeposit(Connection c, String accountId) throws SQLException;

    /** Các hóa đơn còn nợ, cũ nhất trước, đã khóa dòng; dùng khi bù trừ cọc. */
    List<Bill> findOpenForUpdate(Connection c, String accountId) throws SQLException;

    int insert(Connection c, Bill bill, Integer employeeId) throws SQLException;

    /** Cập nhật các khoản tiền của hóa đơn chưa thanh toán. */
    int update(Connection c, Bill bill) throws SQLException;

    /** Cộng tiền đã trả; số còn nợ và trạng thái do PaymentService tính. */
    int applyPayment(Connection c, int billId, BigDecimal paid, BigDecimal due, String status) throws SQLException;

    /** Thay toàn bộ dòng cước gọi của hóa đơn. */
    void replaceCallLines(Connection c, Bill bill) throws SQLException;
}
