namespace Nexus.Api.Common;

/// <summary>
/// Vỏ phản hồi JSON thống nhất cho toàn bộ API:
/// thành công <c>{ success, data, message }</c>, lỗi <c>{ success, error: { code, message } }</c>.
/// </summary>
public sealed record ApiResponse<T>(bool Success, T? Data, string? Message)
{
    public static ApiResponse<T> Ok(T data, string message = "OK") => new(true, data, message);
}

public sealed record ApiError(string Code, string Message);

public sealed record ApiErrorResponse(bool Success, ApiError Error)
{
    public static ApiErrorResponse From(ErrorCode code, string message) =>
        new(false, new ApiError(code.ToString(), message));
}

/// <summary>Kết quả phân trang dùng chung.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Size, int Total)
{
    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Size);
}