package com.nexus.service;

import com.nexus.dao.AuditDao;
import com.nexus.dao.CrudDao;
import com.nexus.model.Page;
import com.nexus.model.SessionUser;
import com.nexus.util.TxRunner;

import java.sql.Connection;
import java.sql.SQLException;

/**
 * Khung dùng chung cho danh mục Admin: transaction, ghi audit, báo không tìm thấy.
 * Lớp con chỉ cần viết phần kiểm tra dữ liệu. C dùng lại cho task 10, 11.
 */
public abstract class AbstractCrudService<T> implements CrudService<T> {
    protected final TxRunner tx;
    protected final CrudDao<T> dao;
    protected final AuditDao audit;
    private final String entityType;

    protected AbstractCrudService(TxRunner tx, CrudDao<T> dao, AuditDao audit, String entityType) {
        this.tx = tx;
        this.dao = dao;
        this.audit = audit;
        this.entityType = entityType;
    }

    /** Kiểm tra và chuẩn hóa dữ liệu trước khi lưu. */
    protected abstract void validate(T item);

    protected abstract int idOf(T item);

    /** Kiểm tra thêm khi cập nhật, cần đọc CSDL (ví dụ không đổi mã đã dùng). Mặc định không có. */
    protected void beforeUpdate(Connection c, T item) throws SQLException {
    }

    /** Sau khi insert, ví dụ tạo tài khoản đăng nhập kèm theo. Mặc định không có. */
    protected void afterInsert(Connection c, T item, int newId, SessionUser actor) throws SQLException {
    }

    /** Sau khi update, ví dụ đồng bộ vai trò sang tài khoản. Mặc định không có. */
    protected void afterUpdate(Connection c, T item) throws SQLException {
    }

    /** Trước khi xóa hoặc ngưng, ví dụ chặn tự khóa chính mình. Mặc định không có. */
    protected void beforeRemove(Connection c, SessionUser actor, int id) throws SQLException {
    }

    /** Sau khi đổi trạng thái sang INACTIVE. Mặc định không có. */
    protected void afterDeactivate(Connection c, int id) throws SQLException {
    }

    @Override
    public Page<T> search(String q, int page, int size) {
        return tx.read(c -> dao.search(c, q, page, size));
    }

    @Override
    public T get(int id) {
        return tx.read(c -> dao.findById(c, id)).orElseThrow(this::notFound);
    }

    @Override
    public int create(SessionUser actor, T item) {
        validate(item);
        return tx.inTransaction(c -> {
            int id = dao.insert(c, item);
            afterInsert(c, item, id, actor);
            audit.log(c, actor.getUserId(), "CREATE", entityType, String.valueOf(id), null);
            return id;
        });
    }

    @Override
    public void update(SessionUser actor, T item) {
        validate(item);
        tx.inTransaction(c -> {
            beforeUpdate(c, item);
            if (dao.update(c, item) == 0) {
                throw notFound();
            }
            afterUpdate(c, item);
            audit.log(c, actor.getUserId(), "UPDATE", entityType, String.valueOf(idOf(item)), null);
            return null;
        });
    }

    @Override
    public void delete(SessionUser actor, int id) {
        tx.inTransaction(c -> {
            beforeRemove(c, actor, id);
            removeRow(c, id);
            audit.log(c, actor.getUserId(), "DELETE", entityType, String.valueOf(id), null);
            return null;
        });
    }

    /** Xóa dòng; lớp con ghi đè khi cần xóa kèm dữ liệu phụ (tài khoản của nhân viên). */
    protected void removeRow(Connection c, int id) throws SQLException {
        if (dao.delete(c, id) == 0) {
            throw notFound();
        }
    }

    @Override
    public void deactivate(SessionUser actor, int id) {
        tx.inTransaction(c -> {
            beforeRemove(c, actor, id);
            if (dao.setStatus(c, id, "INACTIVE") == 0) {
                throw notFound();
            }
            afterDeactivate(c, id);
            audit.log(c, actor.getUserId(), "DEACTIVATE", entityType, String.valueOf(id), null);
            return null;
        });
    }

    protected BusinessException notFound() {
        return new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy dữ liệu.");
    }
}
