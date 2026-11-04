package com.nexus.dao;

import java.sql.Connection;
import java.sql.SQLException;

/** Bản JDBC của {@link ConfigDao}. */
public class JdbcConfigDao implements ConfigDao {
    @Override
    public String get(Connection c, String key) throws SQLException {
        return Sql.one(c, "SELECT ConfigValue FROM SystemConfig WHERE ConfigKey = ?", rs -> rs.getString(1), key)
                .orElseThrow(() -> new IllegalStateException("Thiếu cấu hình " + key));
    }
}
