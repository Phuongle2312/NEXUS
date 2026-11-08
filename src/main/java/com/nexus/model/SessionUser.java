package com.nexus.model;

import java.io.Serializable;

/**
 * Người dùng đang đăng nhập, lưu trong HttpSession với khóa {@link #SESSION_KEY}.
 * Quyền và phạm vi dữ liệu luôn lấy từ đây, không tin giá trị do client gửi.
 */
public class SessionUser implements Serializable {
    public static final String SESSION_KEY = "user";
    private static final long serialVersionUID = 1L;

    private final int userId;
    private final String username;
    private final String role;
    private final Integer employeeId;
    private final Integer customerId;
    private final String displayName;
    private boolean mustChangePassword;

    public SessionUser(int userId, String username, String role, Integer employeeId, Integer customerId,
                       String displayName, boolean mustChangePassword) {
        this.userId = userId;
        this.username = username;
        this.role = role;
        this.employeeId = employeeId;
        this.customerId = customerId;
        this.displayName = displayName;
        this.mustChangePassword = mustChangePassword;
    }

    public int getUserId() {
        return userId;
    }

    public String getUsername() {
        return username;
    }

    public String getRole() {
        return role;
    }

    public Integer getEmployeeId() {
        return employeeId;
    }

    public Integer getCustomerId() {
        return customerId;
    }

    public String getDisplayName() {
        return displayName;
    }

    public boolean isMustChangePassword() {
        return mustChangePassword;
    }

    public void setMustChangePassword(boolean mustChangePassword) {
        this.mustChangePassword = mustChangePassword;
    }
}
