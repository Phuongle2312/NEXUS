using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>Người dùng đang đăng nhập, lấy từ JWT và dùng trong mọi Controller.</summary>
public sealed record CurrentUser(
    int UserId,
    string Username,
    string Role,
    int? EmployeeId,
    int? CustomerId)
{
    public bool IsCustomer => Role == Roles.Customer;
    public bool IsStaff => Role is Roles.Admin or Roles.Retail or Roles.Tech or Roles.Accounts;
    public bool IsAdmin => Role == Roles.Admin;
    public bool IsRetail => Role == Roles.Retail;
    public bool IsTech => Role == Roles.Tech;
    public bool IsAccounts => Role == Roles.Accounts;
}

/// <summary>Cấu hình JWT đọc một lần từ appsettings.</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "Nexus.Api";
    public string Audience { get; set; } = "Nexus.Client";
    public string Key { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 480;
}

/// <summary>Kết quả đăng nhập trả về client.</summary>
public sealed record LoginResult(string AccessToken, DateTime ExpiresAt, CurrentUser User, bool MustChangePassword);

/// <summary>
/// Đăng nhập, cấp JWT, đổi mật khẩu (FR-AUTH-01/03/06).
/// Mật khẩu băm bằng BCrypt, không bao giờ trả về client.
/// </summary>
public class AuthService(NexusDbContext db, JwtOptions jwtOptions)
{
    /// <summary>
    /// Tìm tài khoản và so mật khẩu. Sai tài khoản hay sai mật khẩu đều trả cùng một lỗi
    /// để không lộ username nào tồn tại; vẫn chạy BCrypt một lần khi không có tài khoản
    /// để thời gian phản hồi không khác biệt.
    /// </summary>
    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var account = await db.UserAccounts
            .Include(u => u.Employee)
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (account is null)
        {
            BCrypt.Net.BCrypt.Verify(password, "$2a$10$invalidinvalidinvalidinvalidinvalidinvalidinvalidinvalidinv");
            throw new BusinessException(ErrorCode.AUTH_INVALID, "Sai tên đăng nhập hoặc mật khẩu.");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
        {
            throw new BusinessException(ErrorCode.AUTH_INVALID, "Sai tên đăng nhập hoặc mật khẩu.");
        }

        if (account.Status != "ACTIVE")
        {
            throw new BusinessException(ErrorCode.AUTH_INVALID, "Tài khoản đã bị ngưng.");
        }

        account.LastLoginAt = DateTime.Now;
        await db.SaveChangesAsync(ct);

        var user = new CurrentUser(account.UserId, account.Username, account.Role,
            account.EmployeeId, account.CustomerId);

        return new LoginResult(CreateToken(user), DateTime.Now.AddMinutes(jwtOptions.ExpiryMinutes),
            user, account.MustChangePassword);
    }

    /// <summary>Đổi mật khẩu; xoá cờ MustChangePassword sau khi đổi thành công.</summary>
    public async Task ChangePasswordAsync(CurrentUser user, string oldPassword, string newPassword,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        var account = await db.UserAccounts.FirstOrDefaultAsync(u => u.UserId == user.UserId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy tài khoản.");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, account.PasswordHash))
        {
            throw new BusinessException(ErrorCode.AUTH_INVALID, "Mật khẩu hiện tại không đúng.");
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.MustChangePassword = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Tạo JWT ngắn hạn chứa vai trò và hồ sơ liên kết.</summary>
    private string CreateToken(CurrentUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.EmployeeId is not null)
        {
            claims.Add(new Claim("employeeId", user.EmployeeId.Value.ToString()));
        }

        if (user.CustomerId is not null)
        {
            claims.Add(new Claim("customerId", user.CustomerId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}