package com.nexus.service;

import com.nexus.dao.AuditDao;
import com.nexus.dao.CrudDao;
import com.nexus.dao.UserAccountDao;
import com.nexus.model.Employee;
import com.nexus.model.SessionUser;
import com.nexus.util.PasswordUtil;
import com.nexus.util.TxRunner;
import com.nexus.util.Validator;

import java.sql.Connection;
import java.sql.SQLException;
import java.util.Set;

/**
 * Quản lý nhân viên và tài khoản đăng nhập của họ (FR-ADM-01, FR-AUTH-02).
 * Hồ sơ và tài khoản luôn đổi trong cùng transaction để không lệch vai trò, trạng thái.
 */
public class EmployeeService extends AbstractCrudService<Employee> {
    private static final Set<String> ROLES = Set.of("ADMIN", "ACCOUNTS", "TECH", "RETAIL");

    private final UserAccountDao users;

    public EmployeeService(TxRunner tx, CrudDao<Employee> employees, UserAccountDao users, AuditDao audit) {
        super(tx, employees, audit, "Employee");
        this.users = users;
    }

    @Override
    protected void validate(Employee x) {
        x.setFullName(Validator.required(x.getFullName(), "Họ tên", 150));
        x.setPhone(Validator.phone(x.getPhone()));
        x.setEmail(Validator.email(x.getEmail()));
        if (!ROLES.contains(x.getRole())) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Vai trò không hợp lệ.");
        }
        // Chỉ nhân viên cửa hàng gắn với cửa hàng.
        if ("RETAIL".equals(x.getRole())) {
            if (x.getShopId() == null) {
                throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Nhân viên cửa hàng phải chọn cửa hàng.");
            }
        } else {
            x.setShopId(null);
        }
    }

    @Override
    protected int idOf(Employee x) {
        return x.getId();
    }

    /** Tạo nhân viên kèm tài khoản; mật khẩu do Admin đặt là mật khẩu tạm, phải đổi ở lần đăng nhập đầu. */
    @Override
    public int create(SessionUser actor, Employee x) {
        x.setUsername(Validator.username(x.getUsername()));
        requireStrong(x.getPassword());
        return super.create(actor, x);
    }

    @Override
    protected void afterInsert(Connection c, Employee x, int newId, SessionUser actor) throws SQLException {
        users.insertForEmployee(c, x.getUsername(), PasswordUtil.hash(x.getPassword()), x.getRole(), newId, true);
    }

    /** Đổi vai trò thì tài khoản đổi theo; phiên đang mở sẽ hết hiệu lực ở request kế tiếp. */
    @Override
    protected void afterUpdate(Connection c, Employee x) throws SQLException {
        Employee current = dao.findById(c, x.getId()).orElseThrow(this::notFound);
        users.syncEmployee(c, x.getId(), x.getRole(), current.getStatus());
    }

    /** Admin không được tự xóa hoặc tự khóa mình, tránh hệ thống mất tài khoản quản trị. */
    @Override
    protected void beforeRemove(Connection c, SessionUser actor, int id) {
        if (actor.getEmployeeId() != null && actor.getEmployeeId() == id) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Không thể xóa hoặc ngưng tài khoản của chính mình.");
        }
    }

    /** Xóa kèm tài khoản; nhân viên đã có lịch sử thao tác thì khóa ngoại chặn lại (IN_USE). */
    @Override
    protected void removeRow(Connection c, int id) throws SQLException {
        users.deleteByEmployee(c, id);
        super.removeRow(c, id);
    }

    @Override
    protected void afterDeactivate(Connection c, int id) throws SQLException {
        Employee current = dao.findById(c, id).orElseThrow(this::notFound);
        users.syncEmployee(c, id, current.getRole(), "INACTIVE");
    }

    /** Đặt lại mật khẩu; trả về mật khẩu tạm để hiển thị một lần cho Admin. */
    public String resetPassword(SessionUser actor, int employeeId) {
        String temporary = PasswordUtil.generateTemporary();
        tx.inTransaction(c -> {
            int userId = users.findUserIdByEmployee(c, employeeId).orElseThrow(this::notFound);
            users.updatePassword(c, userId, PasswordUtil.hash(temporary), true);
            audit.log(c, actor.getUserId(), "RESET_PASSWORD", "UserAccount", String.valueOf(userId), null);
            return null;
        });
        return temporary;
    }

    private static void requireStrong(String password) {
        if (!PasswordUtil.isStrongEnough(password)) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Mật khẩu phải từ 8 đến 72 ký tự, có cả chữ và số.");
        }
    }
}
