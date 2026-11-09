namespace Nexus.Api.Domain;

/// <summary>Tham số hệ thống đọc từ DB: thuế suất, tiền cọc, số ngày quá hạn...</summary>
public class SystemConfig
{
    public string ConfigKey { get; set; } = "";
    public string ConfigValue { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>Bậc ưu đãi số lượng lớn (BR-05). MaxQty null = không giới hạn trên.</summary>
public class BulkDiscount
{
    public int DiscountId { get; set; }
    public int MinQty { get; set; }
    public int? MaxQty { get; set; }
    public decimal DiscountPercent { get; set; }
}

/// <summary>
/// Bộ đếm sinh ID. Đọc/ghi trong cùng transaction để không trùng khi nhiều người tạo.
/// Khoá: ORDER_D, ORDER_T, ORDER_B (riêng từng loại) và ACCOUNT (dùng chung).
/// </summary>
public class Sequence
{
    public string SeqName { get; set; } = "";
    public long CurrentValue { get; set; }
}