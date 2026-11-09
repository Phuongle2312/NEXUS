namespace Nexus.Api.Common;

/// <summary>
/// Lỗi nghiệp vụ có mã. Middleware đổi mã này thành HTTP status và JSON lỗi thống nhất.
/// Service ném lỗi này thay vì trả về chuỗi thông báo rời rạc.
/// </summary>
public class BusinessException : Exception
{
    public ErrorCode Code { get; }

    public BusinessException(ErrorCode code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>HTTP status tương ứng với từng mã lỗi.</summary>
    public int StatusCode => Code switch
    {
        ErrorCode.AUTH_INVALID => StatusCodes.Status401Unauthorized,
        ErrorCode.FORBIDDEN => StatusCodes.Status403Forbidden,
        ErrorCode.NOT_FOUND or ErrorCode.ORDER_NOT_FOUND or ErrorCode.ACCOUNT_NOT_FOUND
            => StatusCodes.Status404NotFound,
        ErrorCode.VALIDATION_ERROR => StatusCodes.Status400BadRequest,
        ErrorCode.DUPLICATE or ErrorCode.IN_USE or ErrorCode.BILL_EXISTS
            or ErrorCode.ORDER_STATE_INVALID or ErrorCode.CONNECTION_STATE_INVALID
            or ErrorCode.REQUEST_STATE_INVALID or ErrorCode.NOT_FEASIBLE
            or ErrorCode.LANDLINE_REQUIRED or ErrorCode.LANDLINE_IN_USE
            or ErrorCode.PAYMENT_EXCEEDS_DUE or ErrorCode.DEPOSIT_ALREADY_REFUNDED
            or ErrorCode.OUT_OF_STOCK or ErrorCode.BILL_STATE_INVALID
            => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}