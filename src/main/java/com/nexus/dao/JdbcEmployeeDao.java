package com.nexus.dao;

import com.nexus.model.Employee;
import com.nexus.model.Page;

import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.List;
import java.util.Optional;

/** Nhân viên dùng trực tiếp {@link CrudDao}; bản JDBC ở đây. */
public class JdbcEmployeeDao implements CrudDao<Employee> {
    private static final String COLUMNS = "SELECT e.*, s.ShopName, u.Username";
    private static final String FROM = """
             FROM Employees e
             LEFT JOIN RetailShops s ON s.ShopId = e.ShopId
             LEFT JOIN UserAccounts u ON u.EmployeeId = e.EmployeeId
            """;

    private static Employee map(ResultSet rs) throws SQLException {
        Employee x = new Employee();
        x.setId(rs.getInt("EmployeeId"));
        x.setFullName(rs.getString("FullName"));
        x.setRole(rs.getString("Role"));
        x.setPhone(rs.getString("Phone"));
        x.setEmail(rs.getString("Email"));
        x.setShopId((Integer) rs.getObject("ShopId"));
        x.setShopName(rs.getString("ShopName"));
        x.setStatus(rs.getString("Status"));
        x.setUsername(rs.getString("Username"));
        return x;
    }

    /** Tìm theo họ tên, tên đăng nhập, số điện thoại. */
    @Override
    public Page<Employee> search(Connection c, String q, int page, int size) throws SQLException {
        String p = q == null ? "%" : Sql.contains(q);
        String where = FROM + " WHERE e.FullName LIKE ? ESCAPE '\\' OR u.Username LIKE ? ESCAPE '\\' OR e.Phone LIKE ? ESCAPE '\\'";
        int total = Sql.count(c, "SELECT COUNT(*)" + where, p, p, p);
        List<Employee> items = Sql.list(c, COLUMNS + where + " ORDER BY e.FullName OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                JdbcEmployeeDao::map, p, p, p, (page - 1) * size, size);
        return new Page<>(items, page, size, total);
    }

    @Override
    public Optional<Employee> findById(Connection c, int id) throws SQLException {
        return Sql.one(c, COLUMNS + FROM + " WHERE e.EmployeeId = ?", JdbcEmployeeDao::map, id);
    }

    @Override
    public int insert(Connection c, Employee x) throws SQLException {
        return Sql.insert(c, "INSERT INTO Employees (FullName, Role, Phone, Email, ShopId) VALUES (?,?,?,?,?)",
                x.getFullName(), x.getRole(), x.getPhone(), x.getEmail(), x.getShopId());
    }

    @Override
    public int update(Connection c, Employee x) throws SQLException {
        return Sql.update(c, "UPDATE Employees SET FullName = ?, Role = ?, Phone = ?, Email = ?, ShopId = ? WHERE EmployeeId = ?",
                x.getFullName(), x.getRole(), x.getPhone(), x.getEmail(), x.getShopId(), x.getId());
    }

    @Override
    public int delete(Connection c, int id) throws SQLException {
        return Sql.update(c, "DELETE FROM Employees WHERE EmployeeId = ?", id);
    }

    @Override
    public int setStatus(Connection c, int id, String status) throws SQLException {
        return Sql.update(c, "UPDATE Employees SET Status = ? WHERE EmployeeId = ?", status, id);
    }
}
