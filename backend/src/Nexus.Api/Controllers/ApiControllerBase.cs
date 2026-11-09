using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Common;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

/// <summary>Lấy người dùng hiện tại từ JWT, dùng chung cho các controller.</summary>
[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Hồ sơ người đang đăng nhập, đọc từ claims của token.</summary>
    protected CurrentUser Current
    {
        get
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new BusinessException(ErrorCode.AUTH_INVALID, "Thiếu thông tin đăng nhập.");

            int? employeeId = int.TryParse(User.FindFirstValue("employeeId"), out var emp) ? emp : null;
            int? customerId = int.TryParse(User.FindFirstValue("customerId"), out var cus) ? cus : null;

            return new CurrentUser(
                int.Parse(id),
                User.FindFirstValue(ClaimTypes.Name) ?? "",
                User.FindFirstValue(ClaimTypes.Role) ?? "",
                employeeId,
                customerId);
        }
    }

    /// <summary>Chặn thao tác nếu người dùng không thuộc các vai trò được phép.</summary>
    protected void RequireRoles(params string[] roles)
    {
        if (!roles.Contains(Current.Role))
        {
            throw new BusinessException(ErrorCode.FORBIDDEN, "Bạn không có quyền thực hiện thao tác này.");
        }
    }

    protected ObjectResult OkResponse<T>(T data, string message = "OK") =>
        Ok(ApiResponse<T>.Ok(data, message));
}