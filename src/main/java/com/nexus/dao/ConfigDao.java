package com.nexus.dao;

import java.sql.Connection;
import java.sql.SQLException;

/** Đọc tham số trong SystemConfig. Thiếu khóa là lỗi cấu hình nên ném lỗi hệ thống. */
public interface ConfigDao {
    String get(Connection c, String key) throws SQLException;
}
