package com.nexus.dao;

import java.sql.Connection;
import java.sql.SQLException;

/** Ghi nhật ký thao tác quan trọng (NFR-08). Gọi trong cùng transaction với thao tác đó. */
public interface AuditDao {
    void log(Connection c, Integer actorUserId, String action, String entityType, String entityId, String detail)
            throws SQLException;
}
