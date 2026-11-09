using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Common;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

public sealed record ResetEmployeePasswordRequest(string NewPassword);

/// <summary>Quản trị danh mục: thành phố, cửa hàng, nhân viên, gói cước, nhà cung cấp, thiết bị (FR-12).</summary>
[ApiController]
[Route("api/admin")]
public class AdminController(AdminService admin) : ApiControllerBase
{
    // ------------------------------ Thành phố ------------------------------
    [HttpGet("cities")]
    public async Task<IActionResult> Cities(CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.CitiesAsync(ct));
    }

    [HttpPost("cities")]
    public async Task<IActionResult> CreateCity([FromBody] SaveCityRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.CreateCityAsync(request, ct), "Đã thêm thành phố.");
    }

    [HttpPut("cities/{id:int}")]
    public async Task<IActionResult> UpdateCity(int id, [FromBody] SaveCityRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.UpdateCityAsync(id, request, ct), "Đã cập nhật thành phố.");
    }

    [HttpPost("cities/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateCity(int id, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        await admin.DeactivateCityAsync(id, ct);
        return OkResponse(true, "Đã ngưng thành phố.");
    }

    // ------------------------------ Cửa hàng ------------------------------
    [HttpGet("shops")]
    public async Task<IActionResult> Shops(CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.ShopsAsync(ct));
    }

    [HttpPost("shops")]
    public async Task<IActionResult> CreateShop([FromBody] SaveShopRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.CreateShopAsync(request, ct), "Đã thêm cửa hàng.");
    }

    [HttpPut("shops/{id:int}")]
    public async Task<IActionResult> UpdateShop(int id, [FromBody] SaveShopRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.UpdateShopAsync(id, request, ct), "Đã cập nhật cửa hàng.");
    }

    [HttpPost("shops/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateShop(int id, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        await admin.DeactivateShopAsync(id, ct);
        return OkResponse(true, "Đã ngưng cửa hàng.");
    }

    // ------------------------------ Nhân viên ------------------------------
    [HttpGet("employees")]
    public async Task<IActionResult> Employees(CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.EmployeesAsync(ct));
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] SaveEmployeeRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        var employee = await admin.CreateEmployeeAsync(Current, request, ct);
        return OkResponse(new { employee.EmployeeId, employee.FullName, employee.Role }, "Đã thêm nhân viên.");
    }

    [HttpPut("employees/{id:int}")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] SaveEmployeeRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.UpdateEmployeeAsync(id, request, ct), "Đã cập nhật nhân viên.");
    }

    [HttpPost("employees/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateEmployee(int id, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        await admin.DeactivateEmployeeAsync(id, ct);
        return OkResponse(true, "Đã ngưng nhân viên và tài khoản đăng nhập.");
    }

    // ------------------------------ Gói cước ------------------------------
    [HttpGet("plans")]
    public async Task<IActionResult> Plans([FromQuery] string? type, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.PlansAsync(type, ct));
    }

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] SavePlanRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        var plan = await admin.CreatePlanAsync(request, ct);
        return OkResponse(new { plan.PlanId, plan.PlanName }, "Đã thêm gói cước.");
    }

    [HttpPut("plans/{id:int}")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] SavePlanRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        var plan = await admin.UpdatePlanAsync(id, request, ct);
        return OkResponse(new { plan.PlanId, plan.PlanName }, "Đã cập nhật gói cước.");
    }

    [HttpPost("plans/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivatePlan(int id, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        await admin.DeactivatePlanAsync(id, ct);
        return OkResponse(true, "Đã ngưng gói cước.");
    }

    // ------------------------- Nhà cung cấp, thiết bị -------------------------
    [HttpGet("vendors")]
    public async Task<IActionResult> Vendors(CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.VendorsAsync(ct));
    }

    [HttpPost("vendors")]
    public async Task<IActionResult> CreateVendor([FromBody] SaveVendorRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.CreateVendorAsync(request, ct), "Đã thêm nhà cung cấp.");
    }

    [HttpPost("vendors/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateVendor(int id, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        await admin.DeactivateVendorAsync(id, ct);
        return OkResponse(true, "Đã ngưng nhà cung cấp.");
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products(CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        return OkResponse(await admin.ProductsAsync(ct));
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] SaveProductRequest request, CancellationToken ct)
    {
        RequireRoles(Roles.Admin);
        var product = await admin.CreateProductAsync(request, ct);
        return OkResponse(new { product.ProductId, product.ProductName }, "Đã thêm thiết bị.");
    }

    [HttpPost("products/{id:int}/stock")]
    public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustRequest request,
        CancellationToken ct)
    {
        RequireRoles(Roles.Admin, Roles.Tech);
        var product = await admin.AdjustStockAsync(id, request.Delta, request.Note, ct);
        return OkResponse(new { product.ProductId, product.StockQty }, "Đã điều chỉnh tồn kho.");
    }

    // ------------------------------ Phản hồi ------------------------------
    [HttpGet("feedback")]
    public async Task<IActionResult> Feedback(CancellationToken ct)
    {
        RequireRoles(Roles.Admin, Roles.Retail, Roles.Tech, Roles.Accounts);
        return OkResponse(await admin.FeedbacksAsync(ct));
    }
}

public sealed record StockAdjustRequest(int Delta, string? Note);