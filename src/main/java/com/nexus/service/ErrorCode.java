package com.nexus.service;

/**
 * Bảng mã lỗi nghiệp vụ dùng chung (API Design, mục 1.7), kèm mã HTTP tương ứng (mục 1.5).
 */
public enum ErrorCode {
    AUTH_INVALID(401),
    FORBIDDEN(403),
    VALIDATION_ERROR(400),
    NOT_FOUND(404),
    DUPLICATE(409),
    /** Xóa bị chặn vì đã có dữ liệu liên quan; khi đó dùng deactivate. */
    IN_USE(409),
    ORDER_NOT_FOUND(404),
    ACCOUNT_NOT_FOUND(404),
    ORDER_STATE_INVALID(409),
    NOT_FEASIBLE(409),
    LANDLINE_REQUIRED(400),
    CONNECTION_STATE_INVALID(409),
    OUT_OF_STOCK(409),
    PAYMENT_EXCEEDS_DUE(409),
    BILL_EXISTS(409),
    /** Hóa đơn đã có thanh toán hoặc không phải hóa đơn mới nhất nên không sửa được. */
    BILL_STATE_INVALID(409),
    LANDLINE_IN_USE(409),
    REQUEST_STATE_INVALID(409),
    DEPOSIT_ALREADY_REFUNDED(409);

    private final int httpStatus;

    ErrorCode(int httpStatus) {
        this.httpStatus = httpStatus;
    }

    public int httpStatus() {
        return httpStatus;
    }
}
