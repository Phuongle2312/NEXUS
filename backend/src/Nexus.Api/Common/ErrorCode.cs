namespace Nexus.Api.Common;

/// <summary>
/// Mã lỗi nghiệp vụ dùng chung giữa Service và Controller.
/// Giữ nguyên bộ mã của bản thiết kế API để tài liệu và mã nguồn khớp nhau.
/// </summary>
public enum ErrorCode
{
    AUTH_INVALID,
    FORBIDDEN,
    VALIDATION_ERROR,
    NOT_FOUND,
    DUPLICATE,
    IN_USE,
    BILL_STATE_INVALID,
    ORDER_NOT_FOUND,
    ACCOUNT_NOT_FOUND,
    ORDER_STATE_INVALID,
    NOT_FEASIBLE,
    LANDLINE_REQUIRED,
    CONNECTION_STATE_INVALID,
    OUT_OF_STOCK,
    PAYMENT_EXCEEDS_DUE,
    BILL_EXISTS,
    LANDLINE_IN_USE,
    REQUEST_STATE_INVALID,
    DEPOSIT_ALREADY_REFUNDED,

    /// <summary>Lỗi ngoài dự kiến (CSDL không kết nối được, exception chưa xử lý).</summary>
    SYSTEM_ERROR
}