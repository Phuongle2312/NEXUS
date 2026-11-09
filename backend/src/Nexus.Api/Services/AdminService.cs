using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>Dữ liệu danh mục quản trị (FR-12).</summary>
public sealed record SaveCityRequest(string CityName, string CityCode);
public sealed record SaveShopRequest(string ShopName, string Address, int CityId, string? Phone);
public sealed record SaveEmployeeRequest(string FullName, string Role, string? Phone, string? Email,
    int? ShopId, string? Username, string? Password);
public sealed record SavePlanRequest(string ConnType, string GroupName, string PlanName, decimal Price,
    int ValidityMonths, int? HoursIncluded, decimal? OverageRatePerHour, int? SpeedKbps, string? Description,
    IReadOnlyList<SaveCallRate>? CallRates);
public sealed record SaveCallRate(string CallType, decimal RatePerMin);
public sealed record SaveVendorRequest(string VendorName, string? Contact, string? Phone, string? Email);
public sealed record SaveProductRequest(string ProductName, string ProductType, int? VendorId, decimal Price,
    int StockQty);

/// <summary>
/// Danh mục quản trị: thành phố, cửa hàng, nhân viên, gói cước, nhà cung cấp, thiết bị.
/// Xoá mềm bằng cột Status khi dữ liệu đã được tham chiếu.
/// </summary>
public class AdminService(NexusDbContext db)
{
    // ---------------------------------------------------------------------
    // Thành phố
    // ---------------------------------------------------------------------
    public async Task<IReadOnlyList<City>> CitiesAsync(CancellationToken ct = default) =>
        await db.Cities.AsNoTracking().OrderBy(c => c.CityCode).ToListAsync(ct);

    public async Task<City> CreateCityAsync(SaveCityRequest req, CancellationToken ct = default)
    {
        ValidateCityCode(req.CityCode);
        if (await db.Cities.AnyAsync(c => c.CityCode == req.CityCode, ct))
        {
            throw new BusinessException(ErrorCode.DUPLICATE, "Mã thành phố đã tồn tại.");
        }

        var city = new City { CityName = req.CityName.Trim(), CityCode = req.CityCode, Status = "ACTIVE" };
        db.Cities.Add(city);
        await db.SaveChangesAsync(ct);
        return city;
    }

    public async Task<City> UpdateCityAsync(int id, SaveCityRequest req, CancellationToken ct = default)
    {
        ValidateCityCode(req.CityCode);
        var city = await db.Cities.FirstOrDefaultAsync(c => c.CityId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy thành phố.");

        if (await db.Cities.AnyAsync(c => c.CityCode == req.CityCode && c.CityId != id, ct))
        {
            throw new BusinessException(ErrorCode.DUPLICATE, "Mã thành phố đã tồn tại.");
        }

        // Mã thành phố nằm trong Account ID đã cấp nên không cho đổi khi đã có kết nối.
        if (city.CityCode != req.CityCode &&
            await db.Connections.AnyAsync(c => c.CityId == id, ct))
        {
            throw new BusinessException(ErrorCode.IN_USE,
                "Thành phố đã có kết nối cấp theo mã này, không đổi mã được.");
        }

        city.CityName = req.CityName.Trim();
        city.CityCode = req.CityCode;
        await db.SaveChangesAsync(ct);
        return city;
    }

    public async Task DeactivateCityAsync(int id, CancellationToken ct = default)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.CityId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy thành phố.");
        city.Status = "INACTIVE";
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateCityCode(string code)
    {
        if (code.Length != 3 || code.Any(c => c is < '0' or > '9'))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Mã thành phố phải gồm đúng 3 chữ số.");
        }
    }

    // ---------------------------------------------------------------------
    // Cửa hàng
    // ---------------------------------------------------------------------
    public async Task<IReadOnlyList<object>> ShopsAsync(CancellationToken ct = default) =>
        await db.RetailShops.AsNoTracking().Include(s => s.City)
            .OrderBy(s => s.ShopName)
            .Select(s => (object)new { s.ShopId, s.ShopName, s.Address, s.CityId, CityName = s.City!.CityName, s.Phone, s.Status })
            .ToListAsync(ct);

    public async Task<RetailShop> CreateShopAsync(SaveShopRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.ShopName) || string.IsNullOrWhiteSpace(req.Address))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Tên cửa hàng và địa chỉ là bắt buộc.");
        }

        if (!await db.Cities.AnyAsync(c => c.CityId == req.CityId, ct))
        {
            throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy thành phố.");
        }

        var shop = new RetailShop
        {
            ShopName = req.ShopName.Trim(),
            Address = req.Address.Trim(),
            CityId = req.CityId,
            Phone = req.Phone,
            Status = "ACTIVE"
        };
        db.RetailShops.Add(shop);
        await db.SaveChangesAsync(ct);
        return shop;
    }

    public async Task<RetailShop> UpdateShopAsync(int id, SaveShopRequest req, CancellationToken ct = default)
    {
        var shop = await db.RetailShops.FirstOrDefaultAsync(s => s.ShopId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy cửa hàng.");

        shop.ShopName = req.ShopName.Trim();
        shop.Address = req.Address.Trim();
        shop.CityId = req.CityId;
        shop.Phone = req.Phone;
        await db.SaveChangesAsync(ct);
        return shop;
    }

    public async Task DeactivateShopAsync(int id, CancellationToken ct = default)
    {
        var shop = await db.RetailShops.FirstOrDefaultAsync(s => s.ShopId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy cửa hàng.");
        shop.Status = "INACTIVE";
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Nhân viên
    // ---------------------------------------------------------------------
    public async Task<IReadOnlyList<object>> EmployeesAsync(CancellationToken ct = default) =>
        await db.Employees.AsNoTracking().Include(e => e.Shop)
            .OrderBy(e => e.EmployeeId)
            .Select(e => (object)new
            {
                e.EmployeeId, e.FullName, e.Role, e.Phone, e.Email, e.ShopId,
                ShopName = e.Shop == null ? null : e.Shop.ShopName, e.Status
            })
            .ToListAsync(ct);

    /// <summary>
    /// Thêm nhân viên và tài khoản đăng nhập trong cùng transaction.
    /// RETAIL bắt buộc gắn cửa hàng để biết đơn thuộc về đâu.
    /// </summary>
    public async Task<Employee> CreateEmployeeAsync(CurrentUser actor, SaveEmployeeRequest req,
        CancellationToken ct = default)
    {
        if (!Roles.All.Contains(req.Role))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Vai trò không hợp lệ.");
        }

        if (req.Role == Roles.Retail && req.ShopId is null)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Nhân viên cửa hàng phải được gán cửa hàng.");
        }

        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Tên đăng nhập và mật khẩu là bắt buộc.");
        }

        if (await db.UserAccounts.AnyAsync(u => u.Username == req.Username, ct))
        {
            throw new BusinessException(ErrorCode.DUPLICATE, "Tên đăng nhập đã tồn tại.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var employee = new Employee
        {
            FullName = req.FullName.Trim(),
            Role = req.Role,
            Phone = req.Phone,
            Email = req.Email,
            ShopId = req.ShopId,
            Status = "ACTIVE"
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);

        db.UserAccounts.Add(new UserAccount
        {
            Username = req.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role,
            EmployeeId = employee.EmployeeId,
            MustChangePassword = true,
            Status = "ACTIVE"
        });

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "CREATE_EMPLOYEE",
            EntityType = "Employee",
            EntityId = employee.EmployeeId.ToString(),
            Detail = req.Role
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return employee;
    }

    public async Task<Employee> UpdateEmployeeAsync(int id, SaveEmployeeRequest req, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy nhân viên.");

        if (!Roles.All.Contains(req.Role))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Vai trò không hợp lệ.");
        }

        if (req.Role == Roles.Retail && req.ShopId is null)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Nhân viên cửa hàng phải được gán cửa hàng.");
        }

        employee.FullName = req.FullName.Trim();
        employee.Role = req.Role;
        employee.Phone = req.Phone;
        employee.Email = req.Email;
        employee.ShopId = req.ShopId;

        var account = await db.UserAccounts.FirstOrDefaultAsync(u => u.EmployeeId == id, ct);
        if (account is not null)
        {
            account.Role = req.Role;
        }

        await db.SaveChangesAsync(ct);
        return employee;
    }

    /// <summary>Ngưng nhân viên và tài khoản để họ mất quyền đăng nhập ngay.</summary>
    public async Task DeactivateEmployeeAsync(int id, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy nhân viên.");
        employee.Status = "INACTIVE";

        var account = await db.UserAccounts.FirstOrDefaultAsync(u => u.EmployeeId == id, ct);
        if (account is not null)
        {
            account.Status = "INACTIVE";
        }

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------
    // Gói cước
    // ---------------------------------------------------------------------
    public async Task<IReadOnlyList<Plan>> PlansAsync(string? connType, CancellationToken ct = default)
    {
        var query = db.Plans.AsNoTracking().Include(p => p.CallRates).AsQueryable();
        if (!string.IsNullOrWhiteSpace(connType))
        {
            query = query.Where(p => p.ConnType == connType.ToUpper());
        }

        return await query.OrderBy(p => p.ConnType).ThenBy(p => p.PlanId).ToListAsync(ct);
    }

    public async Task<Plan> CreatePlanAsync(SavePlanRequest req, CancellationToken ct = default)
    {
        ValidatePlan(req);
        if (await db.Plans.AnyAsync(p => p.ConnType == req.ConnType && p.GroupName == req.GroupName
                                        && p.PlanName == req.PlanName, ct))
        {
            throw new BusinessException(ErrorCode.DUPLICATE, "Gói cước trùng tên trong cùng nhóm.");
        }

        var plan = MapPlan(new Plan(), req);
        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<Plan> UpdatePlanAsync(int id, SavePlanRequest req, CancellationToken ct = default)
    {
        ValidatePlan(req);
        var plan = await db.Plans.Include(p => p.CallRates)
            .FirstOrDefaultAsync(p => p.PlanId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy gói cước.");

        MapPlan(plan, req);

        // Đồng bộ cước gọi: xoá dòng không còn, thêm/sửa dòng gửi lên.
        var incoming = req.CallRates ?? [];
        var remove = plan.CallRates
            .Where(r => incoming.All(i => i.CallType != r.CallType))
            .ToList();
        db.PlanCallRates.RemoveRange(remove);

        foreach (var rate in incoming)
        {
            var existing = plan.CallRates.FirstOrDefault(r => r.CallType == rate.CallType);
            if (existing is null)
            {
                plan.CallRates.Add(new PlanCallRate { CallType = rate.CallType, RatePerMin = rate.RatePerMin });
            }
            else
            {
                existing.RatePerMin = rate.RatePerMin;
            }
        }

        await db.SaveChangesAsync(ct);
        return plan;
    }

    /// <summary>Ngưng gói; không xoá vì đơn và kết nối cũ còn tham chiếu.</summary>
    public async Task DeactivatePlanAsync(int id, CancellationToken ct = default)
    {
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.PlanId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy gói cước.");
        plan.Status = "INACTIVE";
        await db.SaveChangesAsync(ct);
    }

    private static Plan MapPlan(Plan plan, SavePlanRequest req)
    {
        plan.ConnType = req.ConnType.ToUpper();
        plan.GroupName = req.GroupName.Trim();
        plan.PlanName = req.PlanName.Trim();
        plan.Price = req.Price;
        plan.ValidityMonths = req.ValidityMonths;
        plan.HoursIncluded = req.HoursIncluded;
        plan.OverageRatePerHour = req.OverageRatePerHour;
        plan.SpeedKbps = req.SpeedKbps;
        plan.Description = req.Description;
        return plan;
    }

    private static void ValidatePlan(SavePlanRequest req)
    {
        if (req.ConnType is not ("D" or "B" or "T"))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Loại kết nối phải là D, B hoặc T.");
        }

        if (req.Price < 0 || req.ValidityMonths <= 0)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Giá không âm và hiệu lực phải lớn hơn 0 tháng.");
        }

        if (req.HoursIncluded is <= 0)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số giờ của gói phải lớn hơn 0.");
        }
    }

    // ---------------------------------------------------------------------
    // Nhà cung cấp và thiết bị
    // ---------------------------------------------------------------------
    public async Task<IReadOnlyList<Vendor>> VendorsAsync(CancellationToken ct = default) =>
        await db.Vendors.AsNoTracking().OrderBy(v => v.VendorName).ToListAsync(ct);

    public async Task<Vendor> CreateVendorAsync(SaveVendorRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.VendorName))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Tên nhà cung cấp là bắt buộc.");
        }

        var vendor = new Vendor
        {
            VendorName = req.VendorName.Trim(), Contact = req.Contact,
            Phone = req.Phone, Email = req.Email, Status = "ACTIVE"
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(ct);
        return vendor;
    }

    public async Task DeactivateVendorAsync(int id, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.VendorId == id, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy nhà cung cấp.");
        vendor.Status = "INACTIVE";
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<object>> ProductsAsync(CancellationToken ct = default) =>
        await db.Products.AsNoTracking().Include(p => p.Vendor)
            .OrderBy(p => p.ProductName)
            .Select(p => (object)new
            {
                p.ProductId, p.ProductName, p.ProductType, p.VendorId,
                VendorName = p.Vendor == null ? null : p.Vendor.VendorName,
                p.Price, p.StockQty, p.Status
            })
            .ToListAsync(ct);

    public async Task<Product> CreateProductAsync(SaveProductRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.ProductName))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Tên thiết bị là bắt buộc.");
        }

        if (req.Price < 0 || req.StockQty < 0)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Giá và tồn kho không được âm.");
        }

        var product = new Product
        {
            ProductName = req.ProductName.Trim(),
            ProductType = req.ProductType,
            VendorId = req.VendorId,
            Price = req.Price,
            StockQty = req.StockQty,
            Status = "ACTIVE"
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product;
    }

    /// <summary>Điều chỉnh tồn kho theo delta, chặn tồn âm.</summary>
    public async Task<Product> AdjustStockAsync(int productId, int delta, string? note,
        CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy thiết bị.");

        if (product.StockQty + delta < 0)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Tồn kho không được âm.");
        }

        product.StockQty += delta;
        db.AuditLogs.Add(new AuditLog
        {
            Action = "ADJUST_STOCK",
            EntityType = "Product",
            EntityId = productId.ToString(),
            Detail = $"{delta:+0;-0;0} ({note})"
        });
        await db.SaveChangesAsync(ct);
        return product;
    }

    // ---------------------------------------------------------------------
    // Phản hồi (nhân viên xem)
    // ---------------------------------------------------------------------
    public async Task<IReadOnlyList<object>> FeedbacksAsync(CancellationToken ct = default) =>
        await db.Feedbacks.AsNoTracking().Include(f => f.Customer)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => (object)new
            {
                f.FeedbackId, CustomerName = f.Customer!.FullName, f.OrderId, f.AccountId,
                f.Rating, f.Comment, f.CreatedAt
            })
            .ToListAsync(ct);
}