package com.nexus.util;

import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;

import java.io.IOException;
import java.io.InputStream;
import java.sql.Connection;
import java.sql.SQLException;
import java.util.Properties;

/**
 * Connection pool và transaction dùng chung cho toàn bộ tầng Service.
 *
 * Cấu hình đọc từ db.properties; file db.local.properties (không commit) ghi đè
 * cho từng máy, ví dụ khi dùng SQL login thay cho Windows Authentication.
 */
public final class DbUtil {

    /** Một đơn vị công việc chạy trong transaction, trả về kết quả. */
    @FunctionalInterface
    public interface TxWork<T> {
        T run(Connection conn) throws SQLException;
    }

    private static volatile HikariDataSource dataSource;

    private DbUtil() {
    }

    /** Khởi tạo pool ở lần gọi đầu tiên (double-checked locking). */
    private static HikariDataSource pool() {
        if (dataSource == null) {
            synchronized (DbUtil.class) {
                if (dataSource == null) {
                    Properties p = loadProperties();
                    HikariConfig cfg = new HikariConfig();
                    cfg.setJdbcUrl(p.getProperty("db.url"));
                    String user = p.getProperty("db.user", "");
                    if (!user.isBlank()) {
                        cfg.setUsername(user);
                        cfg.setPassword(p.getProperty("db.password", ""));
                    }
                    cfg.setMaximumPoolSize(Integer.parseInt(p.getProperty("db.poolSize", "10")));
                    cfg.setPoolName("nexus");
                    dataSource = new HikariDataSource(cfg);
                }
            }
        }
        return dataSource;
    }

    /** Đọc file mặc định rồi ghi đè bằng file cấu hình riêng của máy nếu có. */
    private static Properties loadProperties() {
        Properties p = new Properties();
        for (String name : new String[] {"db.properties", "db.local.properties"}) {
            try (InputStream in = DbUtil.class.getClassLoader().getResourceAsStream(name)) {
                if (in != null) {
                    p.load(in);
                }
            } catch (IOException e) {
                throw new IllegalStateException("Không đọc được " + name, e);
            }
        }
        return p;
    }

    /** Lấy kết nối để đọc đơn lẻ, người gọi tự đóng bằng try-with-resources. */
    public static Connection getConnection() throws SQLException {
        return pool().getConnection();
    }

    /**
     * Chạy công việc trong một transaction: commit khi xong, rollback khi có bất kỳ lỗi nào.
     * BusinessException được ném lại nguyên vẹn để tầng servlet hiển thị.
     */
    public static <T> T inTransaction(TxWork<T> work) {
        try (Connection conn = getConnection()) {
            conn.setAutoCommit(false);
            try {
                T result = work.run(conn);
                conn.commit();
                return result;
            } catch (SQLException | RuntimeException e) {
                conn.rollback();
                throw e;
            } finally {
                conn.setAutoCommit(true);
            }
        } catch (SQLException e) {
            throw new IllegalStateException("Lỗi cơ sở dữ liệu", e);
        }
    }

    /** Đọc dữ liệu không cần transaction. */
    public static <T> T read(TxWork<T> work) {
        try (Connection conn = getConnection()) {
            return work.run(conn);
        } catch (SQLException e) {
            throw new IllegalStateException("Lỗi cơ sở dữ liệu", e);
        }
    }

    /** Đóng pool khi ứng dụng dừng, tránh rò rỉ kết nối khi redeploy. */
    public static synchronized void shutdown() {
        if (dataSource != null) {
            dataSource.close();
            dataSource = null;
        }
    }
}
