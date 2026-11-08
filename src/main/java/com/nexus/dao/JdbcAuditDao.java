package com.nexus.dao;

import java.sql.Connection;
import java.sql.SQLException;

/** Bản JDBC của {@link AuditDao}. */
public class JdbcAuditDao implements AuditDao {
    @Override
    public void log(Connection c, Integer actorUserId, String action, String entityType, String entityId, String detail)
            throws SQLException {
        Sql.update(c, "INSERT INTO AuditLog (ActorUserId, Action, EntityType, EntityId, Detail) VALUES (?,?,?,?,?)",
                actorUserId, action, entityType, entityId, detail);
    }
}
