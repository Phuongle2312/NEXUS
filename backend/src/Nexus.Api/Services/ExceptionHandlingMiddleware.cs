using System.Text.Json;
using Nexus.Api.Common;

namespace Nexus.Api.Services;

/// <summary>
/// Bắt BusinessException ở một chỗ và trả JSON lỗi thống nhất
/// <c>{ success: false, error: { code, message } }</c>.
/// Lỗi không lường trước trả 500 nhưng không lộ stack trace ra client.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            // 401/403 do middleware xác thực và 404 do routing sinh ra không đi qua exception,
            // nên bổ sung vỏ JSON lỗi để client chỉ phải xử lý một định dạng duy nhất.
            await NormalizeFrameworkErrorAsync(context);
        }
        catch (BusinessException ex)
        {
            logger.LogInformation("Lỗi nghiệp vụ {Code}: {Message}", ex.Code, ex.Message);
            await WriteErrorAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi hệ thống khi xử lý {Path}", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                ErrorCode.SYSTEM_ERROR, "Lỗi hệ thống, vui lòng thử lại.");
        }
    }

    /// <summary>Đổi phản hồi lỗi rỗng của framework thành vỏ JSON chuẩn.</summary>
    private static async Task NormalizeFrameworkErrorAsync(HttpContext context)
    {
        if (context.Response.HasStarted || context.Response.ContentLength is > 0)
        {
            return;
        }

        var (code, message) = context.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized =>
                (ErrorCode.AUTH_INVALID, "Vui lòng đăng nhập để tiếp tục."),
            StatusCodes.Status403Forbidden =>
                (ErrorCode.FORBIDDEN, "Bạn không có quyền thực hiện thao tác này."),
            StatusCodes.Status404NotFound =>
                (ErrorCode.NOT_FOUND, "Không tìm thấy tài nguyên yêu cầu."),
            StatusCodes.Status405MethodNotAllowed =>
                (ErrorCode.VALIDATION_ERROR, "Phương thức HTTP không được hỗ trợ cho đường dẫn này."),
            _ => ((ErrorCode)(-1), string.Empty)
        };

        if (message.Length == 0)
        {
            return;
        }

        await WriteErrorAsync(context, context.Response.StatusCode, code, message);
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, ErrorCode code, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(ApiJson.Error(code, message));
    }
}