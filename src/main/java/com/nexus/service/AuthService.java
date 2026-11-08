package com.nexus.service;

import com.nexus.dao.AuditDao;
import com.nexus.dao.UserAccountDao;
import com.nexus.model.SessionUser;
import com.nexus.util.PasswordUtil;
import com.nexus.util.TxRunner;

import java.util.Optional;

/** Đăng nhập, đổi mật khẩu, kiểm tra phiên (FR-AUTH-01..06). */
public class AuthService {
    private final TxRunner tx;
    private final UserAccountDao users;
    private final AuditDao audit;

    public AuthService(TxRunner tx, UserAccountDao users, AuditDao audit) {
        this.tx = tx;
        this.users = users;
        this.audit = audit;
    }

    /**
     * Xác thực tên đăng nhập và mật khẩu. Mọi trường hợp sai đều cùng một thông báo,
     * và vẫn tốn thời gian băm khi không có tài khoản, để không lộ tên đăng nhập có tồn tại không.
     */
    public SessionUser login(String username, String password) {
        Optional<UserAccountDao.Row> found = username == null ? Optional.empty()
                : tx.read(c -> users.findByUsername(c, username.trim()));
        if (found.isEmpty()) {
            PasswordUtil.burnVerifyTime(password);
            throw invalidLogin();
        }
        UserAccountDao.Row row = found.get();
        if (!PasswordUtil.verify(password, row.passwordHash()) || !"ACTIVE".equals(row.status())) {
            throw invalidLogin();
        }
        tx.inTransaction(c -> {
            users.touchLastLogin(c, row.userId());
            audit.log(c, row.userId(), "LOGIN", "UserAccount", String.valueOf(row.userId()), null);
            return null;
        });
        return new SessionUser(row.userId(), row.username(), row.role(), row.employeeId(), row.customerId(),
                row.displayName(), row.mustChangePassword());
    }

    private static BusinessException invalidLogin() {
        return new BusinessException(ErrorCode.AUTH_INVALID, "Sai tên đăng nhập hoặc mật khẩu.");
    }

    /** Đổi mật khẩu: phải nhập đúng mật khẩu cũ, mật khẩu mới đạt chính sách và khác mật khẩu cũ. */
    public void changePassword(SessionUser user, String oldPassword, String newPassword) {
        if (!PasswordUtil.isStrongEnough(newPassword)) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Mật khẩu mới phải từ 8 đến 72 ký tự, có cả chữ và số.");
        }
        if (newPassword.equals(oldPassword)) {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Mật khẩu mới phải khác mật khẩu cũ.");
        }
        tx.inTransaction(c -> {
            UserAccountDao.Row row = users.findById(c, user.getUserId())
                    .orElseThrow(AuthService::invalidLogin);
            if (!PasswordUtil.verify(oldPassword, row.passwordHash())) {
                throw new BusinessException(ErrorCode.AUTH_INVALID, "Mật khẩu hiện tại không đúng.");
            }
            users.updatePassword(c, user.getUserId(), PasswordUtil.hash(newPassword), false);
            audit.log(c, user.getUserId(), "CHANGE_PASSWORD", "UserAccount", String.valueOf(user.getUserId()), null);
            return null;
        });
        user.setMustChangePassword(false);
    }

    /** Phiên còn hiệu lực khi tài khoản vẫn hoạt động và không đổi vai trò kể từ lúc đăng nhập. */
    public boolean isSessionStillValid(SessionUser user) {
        return tx.read(c -> users.findById(c, user.getUserId()))
                .filter(row -> "ACTIVE".equals(row.status()) && row.role().equals(user.getRole()))
                .isPresent();
    }
}
