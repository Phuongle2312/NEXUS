namespace Nexus.Api.Domain;

/// <summary>
/// Thành phố. CityCode 3 chữ số là một phần của Account ID (BR-02).
/// </summary>
public class City
{
    public int CityId { get; set; }
    public string CityName { get; set; } = "";
    public string CityCode { get; set; } = "";
    public string Status { get; set; } = "ACTIVE";
}

/// <summary>Cửa hàng bán lẻ trực thuộc một thành phố.</summary>
public class RetailShop
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public string Address { get; set; } = "";
    public int CityId { get; set; }
    public City? City { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

/// <summary>
/// Nhân viên. ShopId chỉ bắt buộc với vai trò RETAIL.
/// </summary>
public class Employee
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? ShopId { get; set; }
    public RetailShop? Shop { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

/// <summary>Khách hàng cá nhân hoặc doanh nghiệp.</summary>
public class Customer
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = "";
    public string Address { get; set; } = "";
    public int CityId { get; set; }
    public City? City { get; set; }
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string CustomerType { get; set; } = "INDIVIDUAL";
    public string? AddressProofPath { get; set; }
    public int? CreatedByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Tài khoản đăng nhập dùng chung cho nhân viên và khách hàng.
/// Mỗi tài khoản gắn với đúng một EmployeeId hoặc CustomerId.
/// </summary>
public class UserAccount
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public bool MustChangePassword { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime? LastLoginAt { get; set; }
}