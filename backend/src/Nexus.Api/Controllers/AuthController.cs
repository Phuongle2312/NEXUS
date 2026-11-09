using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Common;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

public sealed record LoginRequest(string Username, string Password);
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);

/// <summary>Đăng nhập, đăng xuất, đổi mật khẩu (FR-02).</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ApiControllerBase
{
    /// <summary>Đăng nhập, trả JWT và hồ sơ người dùng.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Vui lòng nhập tên đăng nhập và mật khẩu.");
        }

        var result = await auth.LoginAsync(request.Username.Trim(), request.Password, ct);
        return OkResponse(new
        {
            result.AccessToken,
            result.ExpiresAt,
            result.MustChangePassword,
            User = new
            {
                result.User.UserId,
                result.User.Username,
                result.User.Role,
                result.User.EmployeeId,
                result.User.CustomerId
            }
        }, "Đăng nhập thành công.");
    }

    /// <summary>Đổi mật khẩu của chính mình.</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(Current, request.OldPassword, request.NewPassword, ct);
        return OkResponse(true, "Đã đổi mật khẩu.");
    }

    /// <summary>Đăng xuất. JWT là stateless nên client chỉ cần xoá token đã lưu.</summary>
    [HttpPost("logout")]
    public IActionResult Logout() => OkResponse(true, "Đã đăng xuất.");
}