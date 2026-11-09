using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Common;
using Nexus.Api.Domain;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

/// <summary>Gói cước, thành phố, cửa hàng và công cụ tính ưu đãi cho trang công khai.</summary>
[ApiController]
[Route("api/public")]
public class PublicController(CatalogService catalog, PortalService portal) : ApiControllerBase
{
    /// <summary>Danh sách gói cước đang áp dụng, lọc theo loại D/B/T.</summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> Plans([FromQuery] string? type, CancellationToken ct) =>
        OkResponse(await catalog.PlansAsync(type, ct));

    [HttpGet("cities")]
    [AllowAnonymous]
    public async Task<IActionResult> Cities(CancellationToken ct) =>
        OkResponse(await catalog.CitiesAsync(ct));

    [HttpGet("shops")]
    [AllowAnonymous]
    public async Task<IActionResult> Shops([FromQuery] int? cityId, CancellationToken ct) =>
        OkResponse(await catalog.ShopsAsync(cityId, ct));

    /// <summary>Mức ưu đãi theo số lượng kết nối, kèm toàn bộ bậc để vẽ bảng so sánh.</summary>
    [HttpGet("discount")]
    [AllowAnonymous]
    public async Task<IActionResult> Discount([FromQuery] int qty, CancellationToken ct)
    {
        if (qty < 1)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số lượng phải từ 1 trở lên.");
        }

        return OkResponse(await catalog.DiscountAsync(qty, ct));
    }

    /// <summary>Tra cứu trạng thái đơn theo Order ID (FR-01).</summary>
    [HttpGet("track-order")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackOrder([FromQuery] string orderId, CancellationToken ct) =>
        OkResponse(await portal.TrackOrderAsync(RequireId(orderId, "Order ID"), ct));

    /// <summary>Tra cứu trạng thái kết nối theo Account ID (FR-01).</summary>
    [HttpGet("track-account")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackAccount([FromQuery] string accountId, CancellationToken ct) =>
        OkResponse(await portal.TrackAccountAsync(RequireId(accountId, "Account ID"), ct));

    private static string RequireId(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, $"Vui lòng nhập {label}.");
        }

        return value.Trim();
    }
}