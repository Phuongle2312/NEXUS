package com.nexus.dao;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.Optional;

/** Truy cập tài khoản đăng nhập, dùng chung cho nhân viên và khách hàng. */
public interface UserAccountDao {

    /** Dòng tài khoản kèm tên hiển thị lấy từ Employees hoặc Customers. */
    record Row(int userId, String username, String passwordHash, String role, Integer employeeId,
               Integer customerId, boolean mustChangePassword, String status, String displayName) {
    }

    Optional<Row> findByUsername(Connection c, String username) throws SQLException;

    Optional<Row> findById(Connection c, int userId) throws SQLException;

    /** Tạo tài khoản cho nhân viên; trả về UserId. */
    int insertForEmployee(Connection c, String username, String hash, String role, int employeeId, boolean mustChange)
            throws SQLException;

    /** Đặt mật khẩu mới; mustChange = true khi là mật khẩu tạm. */
    int updatePassword(Connection c, int userId, String hash, boolean mustChange) throws SQLException;

    void touchLastLogin(Connection c, int userId) throws SQLException;

    /** Đồng bộ vai trò và trạng thái khi nhân viên được sửa hoặc ngưng. */
    void syncEmployee(Connection c, int employeeId, String role, String status) throws SQLException;

    Optional<Integer> findUserIdByEmployee(Connection c, int employeeId) throws SQLException;

    void deleteByEmployee(Connection c, int employeeId) throws SQLException;
}
