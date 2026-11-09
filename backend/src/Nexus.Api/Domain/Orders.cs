namespace Nexus.Api.Domain;

/// <summary>Trạng thái đơn đăng ký dịch vụ.</summary>
public static class OrderStatus
{
    public const string Submitted = "SUBMITTED";
    public const string FeasibilityCheck = "FEASIBILITY_CHECK";
    public const string Feasible = "FEASIBLE";
    public const string NotFeasible = "NOT_FEASIBLE";
    public const string Connected = "CONNECTED";
    public const string Cancelled = "CANCELLED";

    public static readonly string[] All =
        [Submitted, FeasibilityCheck, Feasible, NotFeasible, Connected, Cancelled];
}

/// <summary>Trạng thái kết nối.</summary>
public static class ConnectionStatus
{
    public const string Active = "ACTIVE";
    public const string TempInactive = "TEMP_INACTIVE";
    public const string PermInactive = "PERM_INACTIVE";

    public static readonly string[] All = [Active, TempInactive, PermInactive];
}

/// <summary>Vai trò người dùng.</summary>
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Retail = "RETAIL";
    public const string Tech = "TECH";
    public const string Accounts = "ACCOUNTS";
    public const string Customer = "CUSTOMER";

    public static readonly string[] All = [Admin, Retail, Tech, Accounts, Customer];
}

/// <summary>
/// Đơn đăng ký. PlanFee/DepositAmount là giá TRƯỚC ưu đãi, chụp tại thời điểm đặt,
/// để Admin đổi giá không ảnh hưởng đơn cũ. DiscountPercent lưu riêng.
/// </summary>
public class Order
{
    public string OrderId { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int ShopId { get; set; }
    public RetailShop? Shop { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string ConnType { get; set; } = "";
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }
    public int? EquipmentProductId { get; set; }
    public Product? EquipmentProduct { get; set; }
    public int Quantity { get; set; }
    public int ConnectedQty { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal PlanFee { get; set; }
    public decimal DepositAmount { get; set; }
    public int? LandlinePlanId { get; set; }
    public Plan? LandlinePlan { get; set; }
    public decimal? LandlinePlanFee { get; set; }
    public decimal? LandlineDepositAmount { get; set; }
    public string? ExistingLandlineAccountId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = OrderStatus.Submitted;

    public decimal? DistanceKm { get; set; }
    public bool? ServerAvailable { get; set; }
    public bool? LandlineFeasible { get; set; }
    public bool? InternetFeasible { get; set; }
    public string? FeasibilityReason { get; set; }
    public int? CheckedByEmployeeId { get; set; }

    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
}

/// <summary>Lịch sử chuyển trạng thái đơn, hiển thị ở trang chi tiết đơn.</summary>
public class OrderStatusHistory
{
    public long HistoryId { get; set; }
    public string OrderId { get; set; } = "";
    public Order? Order { get; set; }
    public string Status { get; set; } = "";
    public int? ActorUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    public string? Note { get; set; }
}

/// <summary>Yêu cầu đặt kết nối online của khách, chờ nhân viên cửa hàng xác nhận.</summary>
public class OrderRequest
{
    public int RequestId { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int ShopId { get; set; }
    public RetailShop? Shop { get; set; }
    public string ConnType { get; set; } = "";
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? RejectReason { get; set; }
    public string? OrderId { get; set; }
    public int? HandledByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Kết nối đã cấp. AccountId 16 ký tự: loại + mã thành phố (3) + 12 số tuần tự.
/// Dial-up luôn gắn với một landline Nexus qua LandlineAccountId.
/// </summary>
public class Connection
{
    public string AccountId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public Order? Order { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string ConnType { get; set; } = "";
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }
    public int CityId { get; set; }
    public City? City { get; set; }
    public string? LandlineAccountId { get; set; }
    public DateOnly ActivationDate { get; set; }
    public string Status { get; set; } = ConnectionStatus.Active;
    public string? StatusReason { get; set; }

    public bool SuspendRequested { get; set; }
    public string? SuspendRequestReason { get; set; }
    public DateTime? SuspendRequestedAt { get; set; }

    public decimal? DepositRefundAmount { get; set; }
    public DateTime? DepositRefundedAt { get; set; }
    public int? CreatedByEmployeeId { get; set; }

    public List<ConnectionEquipment> Equipment { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
}

/// <summary>Thiết bị đã cấp cho kết nối. Một serial chỉ ISSUED ở tối đa một nơi.</summary>
public class ConnectionEquipment
{
    public int EquipmentId { get; set; }
    public string AccountId { get; set; } = "";
    public Connection? Connection { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string SerialNo { get; set; } = "";
    public DateOnly IssuedDate { get; set; }
    public string Status { get; set; } = "ISSUED";
}