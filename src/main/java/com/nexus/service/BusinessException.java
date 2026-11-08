package com.nexus.service;

/**
 * Lỗi nghiệp vụ có mã và thông báo hiển thị được cho người dùng.
 * Service ném lỗi này; DbUtil rollback transaction, BaseServlet đổi thành mã HTTP và trang lỗi.
 */
public class BusinessException extends RuntimeException {
    private final ErrorCode code;

    public BusinessException(ErrorCode code, String message) {
        super(message);
        this.code = code;
    }

    public ErrorCode getCode() {
        return code;
    }
}
