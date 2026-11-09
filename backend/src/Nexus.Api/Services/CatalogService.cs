using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>
/// Truy vấn danh mục cho màn hình công khai và form chọn gói/cửa hàng.
/// Không chứa nghiệp vụ, chỉ đọc và chiếu ra DTO gọn cho client.
/// </summary>
public class CatalogService(NexusDbContext db, ConfigService config)
{
    /// <summary>Gói cước đang áp dụng, lọc theo loại D/B/T nếu có.</summary>
    public async Task<IReadOnlyList<PlanDto>> PlansAsync(string? connType, CancellationToken ct = default)
    {
        var query = db.Plans.AsNoTracking().Include(p => p.CallRates)
            .Where(p => p.Status == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(connType))
        {
            query = query.Where(p => p.ConnType == connType.ToUpperInvariant());
        }

        var plans = await query
            .OrderBy(p => p.ConnType).ThenBy(p => p.PlanId)
            .Select(p => new
            {
                p.PlanId, p.ConnType, p.GroupName, p.PlanName, p.Price, p.ValidityMonths,
                p.HoursIncluded, p.OverageRatePerHour, p.SpeedKbps, p.Description,
                CallRates = p.CallRates.Select(r => new PlanCallRateDto(r.CallType, r.RatePerMin)).ToList()
            })
            .ToListAsync(ct);

        // Tiền cọc theo loại đọc từ SystemConfig để web công khai hiển thị đúng cấu hình.
        var deposits = new Dictionary<string, decimal>
        {
            ["D"] = await config.DecimalAsync("DEPOSIT_D", 325m, ct),
            ["B"] = await config.DecimalAsync("DEPOSIT_B", 500m, ct),
            ["T"] = await config.DecimalAsync("DEPOSIT_T", 250m, ct)
        };

        return plans.Select(p => new PlanDto(p.PlanId, p.ConnType, p.GroupName, p.PlanName, p.Price,
            p.ValidityMonths, p.HoursIncluded, p.OverageRatePerHour, p.SpeedKbps, p.Description,
            deposits.GetValueOrDefault(p.ConnType), p.CallRates)).ToList();
    }

    /// <summary>Danh sách thành phố đang hoạt động.</summary>
    public async Task<IReadOnlyList<City>> CitiesAsync(CancellationToken ct = default) =>
        await db.Cities.AsNoTracking().Where(c => c.Status == "ACTIVE")
            .OrderBy(c => c.CityCode).ToListAsync(ct);

    /// <summary>Cửa hàng đang hoạt động; lọc theo thành phố nếu có.</summary>
    public async Task<IReadOnlyList<ShopDto>> ShopsAsync(int? cityId, CancellationToken ct = default)
    {
        var query = db.RetailShops.AsNoTracking().Include(s => s.City)
            .Where(s => s.Status == "ACTIVE");

        if (cityId is not null)
        {
            query = query.Where(s => s.CityId == cityId);
        }

        return await query.OrderBy(s => s.ShopName)
            .Select(s => new ShopDto(s.ShopId, s.ShopName, s.Address, s.CityId, s.City!.CityName,
                s.City.CityCode, s.Phone))
            .ToListAsync(ct);
    }

    /// <summary>Mức ưu đãi hiện tại của một số lượng kết nối (dùng cho công cụ tính trên web).</summary>
    public async Task<object> DiscountAsync(int quantity, CancellationToken ct = default)
    {
        var tiers = await db.BulkDiscounts.AsNoTracking()
            .OrderBy(d => d.MinQty)
            .Select(d => new { d.MinQty, d.MaxQty, d.DiscountPercent })
            .ToListAsync(ct);

        var tier = tiers.FirstOrDefault(t => quantity >= t.MinQty && (t.MaxQty is null || quantity <= t.MaxQty));
        return new { quantity, percent = tier?.DiscountPercent ?? 0m, tiers };
    }
}

public sealed record PlanDto(int PlanId, string ConnType, string GroupName, string PlanName, decimal Price,
    int ValidityMonths, int? HoursIncluded, decimal? OverageRatePerHour, int? SpeedKbps, string? Description,
    decimal Deposit, IReadOnlyList<PlanCallRateDto> CallRates);

public sealed record PlanCallRateDto(string CallType, decimal RatePerMin);

public sealed record ShopDto(int ShopId, string ShopName, string Address, int CityId, string CityName,
    string CityCode, string? Phone);