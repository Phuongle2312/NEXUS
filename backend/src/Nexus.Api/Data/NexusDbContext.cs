using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexus.Api.Domain;

namespace Nexus.Api.Data;

/// <summary>
/// Ánh xạ EF Core lên đúng schema SQL Server hiện có trong <c>database/schema.sql</c>.
/// Không dùng migration: database do script SQL tạo, EF chỉ đọc/ghi.
/// Tên bảng giữ số nhiều (Orders, Plans...) vì ORDER và PLAN là từ khoá của SQL Server.
/// </summary>
public class NexusDbContext : DbContext
{
    public NexusDbContext(DbContextOptions<NexusDbContext> options) : base(options)
    {
    }

    public DbSet<City> Cities => Set<City>();
    public DbSet<RetailShop> RetailShops => Set<RetailShop>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<BulkDiscount> BulkDiscounts => Set<BulkDiscount>();
    public DbSet<Sequence> Sequences => Set<Sequence>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanCallRate> PlanCallRates => Set<PlanCallRate>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<OrderRequest> OrderRequests => Set<OrderRequest>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<ConnectionEquipment> ConnectionEquipments => Set<ConnectionEquipment>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillCallUsage> BillCallUsages => Set<BillCallUsage>();
    public DbSet<ReplacementCharge> ReplacementCharges => Set<ReplacementCharge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // SQL Server lưu ngày dạng DATE; DateOnly cần converter khi làm việc với bảng đó.
        var dateOnly = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));

        b.Entity<City>(e =>
        {
            e.ToTable("Cities");
            e.HasKey(x => x.CityId);
            e.Property(x => x.CityName).HasMaxLength(100);
            e.Property(x => x.CityCode).HasMaxLength(3).IsFixedLength();
            e.Property(x => x.Status).HasMaxLength(10);
        });

        b.Entity<RetailShop>(e =>
        {
            e.ToTable("RetailShops");
            e.HasKey(x => x.ShopId);
            e.Property(x => x.ShopName).HasMaxLength(150);
            e.Property(x => x.Address).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(10);
            e.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
        });

        b.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.FullName).HasMaxLength(150);
            e.Property(x => x.Role).HasMaxLength(10);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Status).HasMaxLength(10);
            e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId);
        });

        b.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.FullName).HasMaxLength(150);
            e.Property(x => x.Address).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.CustomerType).HasMaxLength(10);
            e.Property(x => x.AddressProofPath).HasMaxLength(255);
            e.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
        });

        b.Entity<UserAccount>(e =>
        {
            e.ToTable("UserAccounts");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasMaxLength(100);
            e.Property(x => x.Role).HasMaxLength(10);
            e.Property(x => x.Status).HasMaxLength(10);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        });

        b.Entity<SystemConfig>(e =>
        {
            e.ToTable("SystemConfig");
            e.HasKey(x => x.ConfigKey);
            e.Property(x => x.ConfigKey).HasMaxLength(50);
            e.Property(x => x.ConfigValue).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(255);
        });

        b.Entity<BulkDiscount>(e =>
        {
            e.ToTable("BulkDiscounts");
            e.HasKey(x => x.DiscountId);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        });

        b.Entity<Sequence>(e =>
        {
            e.ToTable("Sequences");
            e.HasKey(x => x.SeqName);
            e.Property(x => x.SeqName).HasMaxLength(20);
        });

        b.Entity<Plan>(e =>
        {
            e.ToTable("Plans");
            e.HasKey(x => x.PlanId);
            e.Property(x => x.ConnType).HasMaxLength(1).IsFixedLength();
            e.Property(x => x.GroupName).HasMaxLength(50);
            e.Property(x => x.PlanName).HasMaxLength(100);
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.Property(x => x.OverageRatePerHour).HasPrecision(12, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(10);
            e.HasMany(x => x.CallRates).WithOne(x => x.Plan!).HasForeignKey(x => x.PlanId);
        });

        b.Entity<PlanCallRate>(e =>
        {
            e.ToTable("PlanCallRates");
            e.HasKey(x => new { x.PlanId, x.CallType });
            e.Property(x => x.CallType).HasMaxLength(5);
            e.Property(x => x.RatePerMin).HasPrecision(12, 2);
        });

        b.Entity<Vendor>(e =>
        {
            e.ToTable("Vendors");
            e.HasKey(x => x.VendorId);
            e.Property(x => x.VendorName).HasMaxLength(150);
            e.Property(x => x.Contact).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Status).HasMaxLength(10);
        });

        b.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductName).HasMaxLength(150);
            e.Property(x => x.ProductType).HasMaxLength(20);
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.Property(x => x.Status).HasMaxLength(10);
            e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId);
        });

        b.Entity<Purchase>(e =>
        {
            e.ToTable("Purchases");
            e.HasKey(x => x.PurchaseId);
            e.Property(x => x.PurchaseDate).HasConversion(dateOnly);
            e.Property(x => x.TotalAmount).HasPrecision(12, 2);
            e.Property(x => x.AmountPaid).HasPrecision(12, 2);
            e.Property(x => x.Status).HasMaxLength(10);
            e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId);
            e.HasMany(x => x.Items).WithOne(x => x.Purchase!).HasForeignKey(x => x.PurchaseId);
        });

        b.Entity<PurchaseItem>(e =>
        {
            e.ToTable("PurchaseItems");
            e.HasKey(x => new { x.PurchaseId, x.ProductId });
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        b.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.OrderId);
            e.Property(x => x.OrderId).HasMaxLength(11).IsFixedLength();
            e.Property(x => x.ConnType).HasMaxLength(1).IsFixedLength();
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.PlanFee).HasPrecision(12, 2);
            e.Property(x => x.DepositAmount).HasPrecision(12, 2);
            e.Property(x => x.LandlinePlanFee).HasPrecision(12, 2);
            e.Property(x => x.LandlineDepositAmount).HasPrecision(12, 2);
            e.Property(x => x.DistanceKm).HasPrecision(8, 2);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.ExistingLandlineAccountId).HasMaxLength(16).IsFixedLength();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId);
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);
            e.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId);
            e.HasOne(x => x.EquipmentProduct).WithMany().HasForeignKey(x => x.EquipmentProductId);
            e.HasOne(x => x.LandlinePlan).WithMany().HasForeignKey(x => x.LandlinePlanId);
            e.HasMany(x => x.StatusHistory).WithOne(x => x.Order!).HasForeignKey(x => x.OrderId);
            e.HasMany(x => x.Connections).WithOne(x => x.Order!).HasForeignKey(x => x.OrderId);
        });

        b.Entity<OrderStatusHistory>(e =>
        {
            e.ToTable("OrderStatusHistory");
            e.HasKey(x => x.HistoryId);
            e.Property(x => x.OrderId).HasMaxLength(11).IsFixedLength();
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);
        });

        b.Entity<OrderRequest>(e =>
        {
            e.ToTable("OrderRequests");
            e.HasKey(x => x.RequestId);
            e.Property(x => x.ConnType).HasMaxLength(1).IsFixedLength();
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(10);
            e.Property(x => x.RejectReason).HasMaxLength(500);
            e.Property(x => x.OrderId).HasMaxLength(11).IsFixedLength();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId);
            e.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId);
        });

        b.Entity<Connection>(e =>
        {
            e.ToTable("Connections");
            e.HasKey(x => x.AccountId);
            e.Property(x => x.AccountId).HasMaxLength(16).IsFixedLength();
            e.Property(x => x.OrderId).HasMaxLength(11).IsFixedLength();
            e.Property(x => x.ConnType).HasMaxLength(1).IsFixedLength();
            e.Property(x => x.LandlineAccountId).HasMaxLength(16).IsFixedLength();
            e.Property(x => x.ActivationDate).HasConversion(dateOnly);
            e.Property(x => x.Status).HasMaxLength(15);
            e.Property(x => x.StatusReason).HasMaxLength(500);
            e.Property(x => x.SuspendRequestReason).HasMaxLength(500);
            e.Property(x => x.DepositRefundAmount).HasPrecision(12, 2);
            e.HasOne(x => x.Order).WithMany(x => x.Connections).HasForeignKey(x => x.OrderId);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId);
            e.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
            e.HasMany(x => x.Equipment).WithOne(x => x.Connection!).HasForeignKey(x => x.AccountId);
            e.HasMany(x => x.Bills).WithOne(x => x.Connection!).HasForeignKey(x => x.AccountId);
        });

        b.Entity<ConnectionEquipment>(e =>
        {
            e.ToTable("ConnectionEquipment");
            e.HasKey(x => x.EquipmentId);
            e.Property(x => x.AccountId).HasMaxLength(16).IsFixedLength();
            e.Property(x => x.SerialNo).HasMaxLength(50);
            e.Property(x => x.IssuedDate).HasConversion(dateOnly);
            e.Property(x => x.Status).HasMaxLength(10);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        b.Entity<Bill>(e =>
        {
            e.ToTable("Bills");
            e.HasKey(x => x.BillId);
            e.Property(x => x.AccountId).HasMaxLength(16).IsFixedLength();
            e.Property(x => x.PeriodStart).HasConversion(dateOnly);
            e.Property(x => x.PeriodEnd).HasConversion(dateOnly);
            e.Property(x => x.BillDate).HasConversion(dateOnly);
            e.Property(x => x.DueDate).HasConversion(dateOnly);
            foreach (var p in new[]
            {
                nameof(Bill.PlanFee), nameof(Bill.CallCharges), nameof(Bill.OverageCharges),
                nameof(Bill.ReplacementCharges), nameof(Bill.Adjustment), nameof(Bill.TaxableBase),
                nameof(Bill.TaxAmount), nameof(Bill.DepositAmount), nameof(Bill.TotalAmount),
                nameof(Bill.PaidAmount), nameof(Bill.DueAmount), nameof(Bill.PreviousDue)
            })
            {
                e.Property(p).HasPrecision(12, 2);
            }
            e.Property(x => x.UsageHours).HasPrecision(8, 2);
            e.Property(x => x.TaxRate).HasPrecision(6, 4);
            e.Property(x => x.Status).HasMaxLength(10);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasMany(x => x.CallUsages).WithOne(x => x.Bill!).HasForeignKey(x => x.BillId);
            e.HasMany(x => x.Payments).WithOne(x => x.Bill!).HasForeignKey(x => x.BillId);
        });

        b.Entity<BillCallUsage>(e =>
        {
            e.ToTable("BillCallUsage");
            e.HasKey(x => new { x.BillId, x.CallType });
            e.Property(x => x.CallType).HasMaxLength(5);
            e.Property(x => x.Minutes).HasPrecision(10, 2);
            e.Property(x => x.RatePerMin).HasPrecision(12, 2);
            e.Property(x => x.Amount).HasPrecision(12, 2);
        });

        b.Entity<ReplacementCharge>(e =>
        {
            e.ToTable("ReplacementCharges");
            e.HasKey(x => x.ChargeId);
            e.Property(x => x.AccountId).HasMaxLength(16).IsFixedLength();
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.ChargeDate).HasConversion(dateOnly);
            e.Property(x => x.Status).HasMaxLength(15);
            e.Property(x => x.Note).HasMaxLength(255);
            // Khóa chính của Connections là AccountId (không phải Id) nên phải chỉ định rõ khóa ngoại,
            // nếu không EF tự sinh cột bóng ConnectionAccountId không tồn tại trong schema.
            e.HasOne(x => x.Connection).WithMany().HasForeignKey(x => x.AccountId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId);
        });

        b.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.Method).HasMaxLength(15);
            e.Property(x => x.PaidDate).HasConversion(dateOnly);
            e.Property(x => x.Note).HasMaxLength(255);
            e.HasOne(x => x.ReceivedByEmployee).WithMany().HasForeignKey(x => x.ReceivedByEmployeeId);
        });

        b.Entity<Feedback>(e =>
        {
            e.ToTable("Feedback");
            e.HasKey(x => x.FeedbackId);
            e.Property(x => x.OrderId).HasMaxLength(11).IsFixedLength();
            e.Property(x => x.AccountId).HasMaxLength(16).IsFixedLength();
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        });

        b.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLog");
            e.HasKey(x => x.AuditId);
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.EntityType).HasMaxLength(30);
            e.Property(x => x.EntityId).HasMaxLength(30);
            e.Property(x => x.Detail).HasMaxLength(500);
        });
    }
}