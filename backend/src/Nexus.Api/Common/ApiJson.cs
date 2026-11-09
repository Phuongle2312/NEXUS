using System.Text.Json;

namespace Nexus.Api.Common;

/// <summary>
/// Cấu hình JSON dùng chung cho mọi phản hồi lỗi viết tay (middleware, health check)
/// để toàn bộ API có cùng định dạng và cùng cách escape ký tự tiếng Việt.
/// </summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Chuỗi JSON của vỏ lỗi chuẩn; dùng khi không đi qua MVC.</summary>
    public static string Error(ErrorCode code, string message) =>
        JsonSerializer.Serialize(ApiErrorResponse.From(code, message), Options);
}