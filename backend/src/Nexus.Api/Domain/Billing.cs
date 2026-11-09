namespace Nexus.Api.Domain;

/// <summary>
/// Hóa đơn theo kỳ tháng của từng kết nối (BR-07).
/// Tách từng khoản để đổi nhanh công thức khi mentor trả lời.
/// PreviousDue chỉ hiển thị, không nằm trong TotalAmount.
/// </summary>
public class Bill
{
    public int BillId { get; set; }
    public string AccountId { get; set; } = "";
    public Connection? Connection { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public decimal PlanFee { get; set; }
    public decimal CallCharges { get; set; }
    public decimal UsageHours { get; set; }
    public decimal OverageCharges { get; set; }
    public decimal ReplacementCharges { get; set; }
    public decimal Adjustment { get; set; }

    public decimal TaxableBase { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DepositAmount { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public decimal PreviousDue { get; set; }

    public DateOnly BillDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = "UNPAID";
    public string? Note { get; set; }
    public int? CreatedByEmployeeId { get; set; }

    public List<BillCallUsage> CallUsages { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

/// <summary>Cước gọi theo loại của một hóa đơn.</summary>
public class BillCallUsage
{
    public int BillId { get; set; }
    public Bill? Bill { get; set; }
    public string CallType { get; set; } = "";
    public decimal Minutes { get; set; }
    public decimal RatePerMin { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>Phí thay thiết bị hỏng: UNBILLED → BILLED khi vào hóa đơn, DEPOSIT_OFFSET khi trừ vào cọc.</summary>
public class ReplacementCharge
{
    public int ChargeId { get; set; }
    public string AccountId { get; set; } = "";
    public Connection? Connection { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Amount { get; set; }
    public DateOnly ChargeDate { get; set; }
    public string Status { get; set; } = "UNBILLED";
    public int? BillId { get; set; }
    public Bill? Bill { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Thanh toán theo hóa đơn. Method DEPOSIT_OFFSET: bù trừ nợ từ tiền cọc khi hoàn cọc,
/// người dùng không tự chọn được hình thức này.
/// </summary>
public class Payment
{
    public int PaymentId { get; set; }
    public int BillId { get; set; }
    public Bill? Bill { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public DateOnly PaidDate { get; set; }
    public string? Note { get; set; }
    public int ReceivedByEmployeeId { get; set; }
    public Employee? ReceivedByEmployee { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Phản hồi gắn với đúng một đơn hoặc một kết nối.</summary>
public class Feedback
{
    public int FeedbackId { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? OrderId { get; set; }
    public string? AccountId { get; set; }
    public byte? Rating { get; set; }
    public string Comment { get; set; } = "";
    public int? RecordedByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Nhật ký thao tác quan trọng (tạo đơn, đổi trạng thái, lập hóa đơn, thanh toán).</summary>
public class AuditLog
{
    public long AuditId { get; set; }
    public int? ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}