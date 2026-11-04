package com.nexus.dao;

import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.Optional;

/** Bản JDBC của {@link UserAccountDao}. */
public class JdbcUserAccountDao implements UserAccountDao {
    private static final String SELECT = """
            SELECT u.UserId, u.Username, u.PasswordHash, u.Role, u.EmployeeId, u.CustomerId,
                   u.MustChangePassword, u.Status, COALESCE(e.FullName, c.FullName) AS DisplayName
            FROM UserAccounts u
            LEFT JOIN Employees e ON e.EmployeeId = u.EmployeeId
            LEFT JOIN Customers c ON c.CustomerId = u.CustomerId
            """;

    private static Row map(ResultSet rs) throws SQLException {
        return new Row(rs.getInt("UserId"), rs.getString("Username"), rs.getString("PasswordHash"),
                rs.getString("Role"), (Integer) rs.getObject("EmployeeId"), (Integer) rs.getObject("CustomerId"),
                rs.getBoolean("MustChangePassword"), rs.getString("Status"), rs.getString("DisplayName"));
    }

    @Override
    public Optional<Row> findByUsername(Connection c, String username) throws SQLException {
        return Sql.one(c, SELECT + " WHERE u.Username = ?", JdbcUserAccountDao::map, username);
    }

    @Override
    public Optional<Row> findById(Connection c, int userId) throws SQLException {
        return Sql.one(c, SELECT + " WHERE u.UserId = ?", JdbcUserAccountDao::map, userId);
    }

    @Override
    public int insertForEmployee(Connection c, String username, String hash, String role, int employeeId, boolean mustChange)
            throws SQLException {
        return Sql.insert(c, "INSERT INTO UserAccounts (Username, PasswordHash, Role, EmployeeId, MustChangePassword) VALUES (?,?,?,?,?)",
                username, hash, role, employeeId, mustChange);
    }

    @Override
    public int updatePassword(Connection c, int userId, String hash, boolean mustChange) throws SQLException {
        return Sql.update(c, "UPDATE UserAccounts SET PasswordHash = ?, MustChangePassword = ? WHERE UserId = ?",
                hash, mustChange, userId);
    }

    @Override
    public void touchLastLogin(Connection c, int userId) throws SQLException {
        Sql.update(c, "UPDATE UserAccounts SET LastLoginAt = SYSDATETIME() WHERE UserId = ?", userId);
    }

    @Override
    public void syncEmployee(Connection c, int employeeId, String role, String status) throws SQLException {
        Sql.update(c, "UPDATE UserAccounts SET Role = ?, Status = ? WHERE EmployeeId = ?", role, status, employeeId);
    }

    @Override
    public Optional<Integer> findUserIdByEmployee(Connection c, int employeeId) throws SQLException {
        return Sql.one(c, "SELECT UserId FROM UserAccounts WHERE EmployeeId = ?", rs -> rs.getInt(1), employeeId);
    }

    @Override
    public void deleteByEmployee(Connection c, int employeeId) throws SQLException {
        Sql.update(c, "DELETE FROM UserAccounts WHERE EmployeeId = ?", employeeId);
    }
}
