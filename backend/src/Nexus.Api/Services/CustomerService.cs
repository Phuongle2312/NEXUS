using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>Dữ liệu tạo/sửa hồ sơ khách hàng (FR-03).</summary>
public sealed record SaveCustomerRequest(
    string FullName, string Address, int CityId, string Phone, string? Email, string CustomerType);

/// <summary>Quản lý hồ sơ khách và tài khoản portal của khách (FR-03).</summary>
public class CustomerService(NexusDbContext db)
{
    /// <summary>Tìm khách theo tên hoặc số điện thoại, có phân trang.</summary>
    public async Task<PagedResult<Customer>> SearchAsync(string? q, int page, int size, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var query = db.Customers.AsNoTracking().Include(c => c.City).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c => c.FullName.Contains(term) || c.Phone.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.CustomerId)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        return new PagedResult<Customer>(items, page, size, total);
    }

    /// <summary>Chi tiết khách kèm kết nối và đơn gần đây.</summary>
    public async Task<Customer> GetAsync(int id, CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().Include(c => c.City)
            .FirstOrDefaultAsync(c => c.CustomerId == id, ct)
        ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy khách hàng.");

    /// <summary>Thêm khách mới, ghi nhận nhân viên tạo.</summary>
    public async Task<Customer> CreateAsync(CurrentUser actor, SaveCustomerRequest req, CancellationToken ct = default)
    {
        Validate(req);
        var customer = new Customer
        {
            FullName = req.FullName.Trim(),
            Address = req.Address.Trim(),
            CityId = req.CityId,
            Phone = req.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            CustomerType = req.CustomerType,
            CreatedByEmployeeId = actor.EmployeeId,
            CreatedAt = DateTime.Now
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    /// <summary>Cập nhật hồ sơ khách.</summary>
    public async Task<Customer> UpdateAsync(int id, SaveCustomerRequest req, CancellationToken ct = default)
    {
        Validate(req);
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy khách hàng.");

        customer.FullName = req.FullName.Trim();
        customer.Address = req.Address.Trim();
        customer.CityId = req.CityId;
        customer.Phone = req.Phone.Trim();
        customer.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        customer.CustomerType = req.CustomerType;

        await db.SaveChangesAsync(ct);
        return customer;
    }

    /// <summary>
    /// Cấp tài khoản portal cho khách với mật khẩu tạm; bắt đổi ở lần đăng nhập đầu.
    /// Mật khẩu tạm chỉ trả về đúng một lần trong kết quả gọi này.
    /// </summary>
    public async Task<(int UserId, string Username, string TemporaryPassword)> IssueAccountAsync(
        CurrentUser actor, int customerId, string? username, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy khách hàng.");

        var exists = await db.UserAccounts.AnyAsync(u => u.CustomerId == customerId, ct);
        if (exists)
        {
            throw new BusinessException(ErrorCode.DUPLICATE, "Khách hàng đã có tài khoản đăng nhập.");
        }

        var finalUsername = string.IsNullOrWhiteSpace(username)
            ? $"customer{customerId}"
            : username.Trim();

        if (await db.UserAccounts.AnyAsync(u => u.Username == finalUsername, ct))
        {
            throw new BusinessException(ErrorCode.DUPLICATE, $"Tên đăng nhập {finalUsername} đã tồn tại.");
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var account = new UserAccount
        {
            Username = finalUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            Role = Roles.Customer,
            CustomerId = customer.CustomerId,
            MustChangePassword = true,
            Status = "ACTIVE"
        };

        db.UserAccounts.Add(account);
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "ISSUE_CUSTOMER_ACCOUNT",
            EntityType = "Customer",
            EntityId = customerId.ToString(),
            Detail = finalUsername
        });

        await db.SaveChangesAsync(ct);
        return (account.UserId, finalUsername, temporaryPassword);
    }

    /// <summary>Đặt lại mật khẩu tạm cho khách; mật khẩu mới trả về một lần.</summary>
    public async Task<(string Username, string TemporaryPassword)> ResetPasswordAsync(
        CurrentUser actor, int customerId, CancellationToken ct = default)
    {
        var account = await db.UserAccounts.FirstOrDefaultAsync(u => u.CustomerId == customerId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Khách hàng chưa có tài khoản.");

        var temporaryPassword = GenerateTemporaryPassword();
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        account.MustChangePassword = true;

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "RESET_CUSTOMER_PASSWORD",
            EntityType = "Customer",
            EntityId = customerId.ToString(),
            Detail = account.Username
        });

        await db.SaveChangesAsync(ct);
        return (account.Username, temporaryPassword);
    }

    /// <summary>Sinh mật khẩu tạm dễ đọc, đủ mạnh cho lần đăng nhập đầu.</summary>
    private static string GenerateTemporaryPassword() =>
        "Nx" + Guid.NewGuid().ToString("N")[..8] + "!9";

    private static void Validate(SaveCustomerRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Tên khách hàng là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(req.Phone))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số điện thoại là bắt buộc.");
        }

        if (req.CustomerType is not ("INDIVIDUAL" or "CORPORATE"))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Loại khách phải là INDIVIDUAL hoặc CORPORATE.");
        }
    }
}