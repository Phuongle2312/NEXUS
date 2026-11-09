using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Common;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

/// <summary>Quản lý khách hàng của nhân viên cửa hàng (FR-03).</summary>
[ApiController]
[Route("api/customers")]
public class CustomersController(CustomerService customers) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int size = 20, CancellationToken ct = default)
    {
        RequireRoles(Roles.Retail, Roles.Admin, Roles.Tech, Roles.Accounts);
        return OkResponse(await customers.SearchAsync(q, page, size, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) =>
        OkResponse(await customers.GetAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCustomerRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Admin);
        return OkResponse(await customers.CreateAsync(Current, request, ct), "Đã thêm khách hàng.");
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCustomerRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Admin);
        return OkResponse(await customers.UpdateAsync(id, request, ct), "Đã cập nhật khách hàng.");
    }

    /// <summary>Cấp tài khoản portal; mật khẩu tạm chỉ trả về một lần.</summary>
    [HttpPost("{id:int}/issue-account")]
    public async Task<IActionResult> IssueAccount(int id, [FromBody] IssueAccountRequest? request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Admin);
        var result = await customers.IssueAccountAsync(Current, id, request?.Username, ct);
        return OkResponse(new
        {
            result.UserId,
            result.Username,
            result.TemporaryPassword,
            Note = "Mật khẩu tạm chỉ hiển thị một lần; khách phải đổi ở lần đăng nhập đầu."
        }, "Đã cấp tài khoản cho khách.");
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
    {
        RequireRoles(Roles.Retail, Roles.Admin);
        var result = await customers.ResetPasswordAsync(Current, id, ct);
        return OkResponse(new { result.Username, result.TemporaryPassword }, "Đã cấp lại mật khẩu tạm.");
    }
}

public sealed record IssueAccountRequest(string? Username);