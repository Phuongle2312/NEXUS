package com.nexus.dao;

import com.nexus.service.BusinessException;
import com.nexus.service.ErrorCode;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/**
 * Tiện ích JDBC dùng chung cho mọi DAO. Mọi truy vấn đều qua PreparedStatement,
 * nên giá trị luôn là tham số, không bao giờ nối chuỗi vào SQL (chống SQL Injection).
 *
 * DAO nhận Connection từ Service để nhiều thao tác nằm chung một transaction.
 */
public final class Sql {

    /** Đổi một dòng ResultSet thành đối tượng. */
    @FunctionalInterface
    public interface RowMapper<T> {
        T map(ResultSet rs) throws SQLException;
    }

    private static final int ERR_FK_OR_CHECK = 547;
    private static final int ERR_UNIQUE_CONSTRAINT = 2627;
    private static final int ERR_UNIQUE_INDEX = 2601;

    private Sql() {
    }

    /** Truy vấn nhiều dòng. */
    public static <T> List<T> list(Connection c, String sql, RowMapper<T> mapper, Object... params) throws SQLException {
        try (PreparedStatement ps = prepare(c, sql, params); ResultSet rs = ps.executeQuery()) {
            List<T> rows = new ArrayList<>();
            while (rs.next()) {
                rows.add(mapper.map(rs));
            }
            return rows;
        } catch (SQLException e) {
            throw translate(e);
        }
    }

    /** Truy vấn tối đa một dòng. */
    public static <T> Optional<T> one(Connection c, String sql, RowMapper<T> mapper, Object... params) throws SQLException {
        List<T> rows = list(c, sql, mapper, params);
        // ofNullable: hàm tổng như MIN trên tập rỗng trả một dòng có giá trị NULL.
        return rows.isEmpty() ? Optional.empty() : Optional.ofNullable(rows.get(0));
    }

    /** INSERT, UPDATE, DELETE; trả về số dòng bị ảnh hưởng. */
    public static int update(Connection c, String sql, Object... params) throws SQLException {
        try (PreparedStatement ps = prepare(c, sql, params)) {
            return ps.executeUpdate();
        } catch (SQLException e) {
            throw translate(e);
        }
    }

    /** INSERT vào bảng có cột IDENTITY; trả về khóa vừa sinh. */
    public static int insert(Connection c, String sql, Object... params) throws SQLException {
        try (PreparedStatement ps = c.prepareStatement(sql, Statement.RETURN_GENERATED_KEYS)) {
            bind(ps, params);
            ps.executeUpdate();
            try (ResultSet rs = ps.getGeneratedKeys()) {
                if (!rs.next()) {
                    throw new SQLException("Không lấy được khóa vừa sinh");
                }
                return rs.getInt(1);
            }
        } catch (SQLException e) {
            throw translate(e);
        }
    }

    /** Đếm dòng hoặc lấy giá trị số đầu tiên của truy vấn. */
    public static int count(Connection c, String sql, Object... params) throws SQLException {
        return one(c, sql, rs -> rs.getInt(1), params).orElse(0);
    }

    /** Mẫu LIKE "chứa chuỗi": escape ký tự đại diện để người dùng không tự chèn được %, _ hoặc [. */
    public static String contains(String q) {
        return "%" + q.replace("\\", "\\\\").replace("%", "\\%").replace("_", "\\_").replace("[", "\\[") + "%";
    }

    private static PreparedStatement prepare(Connection c, String sql, Object... params) throws SQLException {
        PreparedStatement ps = c.prepareStatement(sql);
        try {
            bind(ps, params);
            return ps;
        } catch (SQLException e) {
            ps.close();
            throw e;
        }
    }

    private static void bind(PreparedStatement ps, Object... params) throws SQLException {
        for (int i = 0; i < params.length; i++) {
            ps.setObject(i + 1, params[i]);
        }
    }

    /**
     * Đổi lỗi ràng buộc của SQL Server thành lỗi nghiệp vụ: trùng khóa thành DUPLICATE,
     * vướng khóa ngoại thành IN_USE. Lỗi khác giữ nguyên để không lộ chi tiết SQL ra giao diện.
     */
    private static SQLException translate(SQLException e) {
        int err = e.getErrorCode();
        if (err == ERR_UNIQUE_CONSTRAINT || err == ERR_UNIQUE_INDEX) {
            throw new BusinessException(ErrorCode.DUPLICATE, "Dữ liệu đã tồn tại (trùng khóa).");
        }
        if (err == ERR_FK_OR_CHECK && String.valueOf(e.getMessage()).contains("REFERENCE")) {
            throw new BusinessException(ErrorCode.IN_USE, "Dữ liệu đang được sử dụng ở nơi khác nên không thể thực hiện.");
        }
        return e;
    }
}
