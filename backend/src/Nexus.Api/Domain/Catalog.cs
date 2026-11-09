namespace Nexus.Api.Domain;

/// <summary>
/// Gói cước. HoursIncluded tính trên cả chu kỳ hiệu lực (30 giờ / 3 tháng).
/// OverageRatePerHour null với gói không giới hạn.
/// </summary>
public class Plan
{
    public int PlanId { get; set; }
    public string ConnType { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string PlanName { get; set; } = "";
    public decimal Price { get; set; }
    public int ValidityMonths { get; set; }
    public int? HoursIncluded { get; set; }
    public decimal? OverageRatePerHour { get; set; }
    public int? SpeedKbps { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";

    public List<PlanCallRate> CallRates { get; set; } = new();
}

/// <summary>Cước gọi theo loại của gói Landline: LOCAL, STD, SMS.</summary>
public class PlanCallRate
{
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }
    public string CallType { get; set; } = "";
    public decimal RatePerMin { get; set; }
}

/// <summary>Nhà cung cấp thiết bị.</summary>
public class Vendor
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = "";
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

/// <summary>Danh mục thiết bị (Modem, Router...).</summary>
public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductType { get; set; } = "";
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public decimal Price { get; set; }
    public int StockQty { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

/// <summary>Đơn mua thiết bị từ nhà cung cấp. ReceivedAt có giá trị = đã cộng tồn kho.</summary>
public class Purchase
{
    public int PurchaseId { get; set; }
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public List<PurchaseItem> Items { get; set; } = new();
}

public class PurchaseItem
{
    public int PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}