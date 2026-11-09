using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>Kết quả tra cứu đơn công khai (chỉ trạng thái, không lộ dữ liệu cá nhân).</summary>
public sealed record OrderTrackingDto(string OrderId, string ConnType, string PlanName, string Status,
    int Quantity, int ConnectedQty, DateOnly OrderDate, decimal DiscountPercent);

/// <summary>Kết quả tra cứu kết nối công khai.</summary>
public sealed record AccountTrackingDto(string AccountId, string ConnType, string PlanName, string Status,
    DateOnly ActivationDate, string CityName, string? CustomerName, decimal DueAmount);

/// <summary>
/// Khu vực khách hàng (<c>/api/my/*</c>) và tra cứu công khai.
/// Mọi truy vấn của khách lọc theo customerId từ token, không nhận customerId từ client.
/// </summary>
public class PortalService(NexusDbContext db)
{
    /// <summary>Tra cứu đơn công khai theo Order ID, chỉ trả trạng thái tiến độ.</summary>
    public async Task<OrderTrackingDto> TrackOrderAsync(string orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Plan)
            .FirstOrDefaultAsync(o => o.OrderId == orderId.ToUpper(), ct)
            ?? throw new BusinessException(ErrorCode.ORDER_NOT_FOUND, $"Không tìm thấy đơn {orderId}.");

        return new OrderTrackingDto(order.OrderId, order.ConnType, order.Plan!.PlanName, order.Status,
            order.Quantity, order.ConnectedQty, DateOnly.FromDateTime(order.OrderDate), order.DiscountPercent);
    }

    /// <summary>
    /// Tra cứu kết nối công khai: chỉ trả trạng thái và số nợ tổng.
    /// Tên khách chỉ hiển thị khi đã đăng nhập đúng chủ sở hữu (xem <see cref="MyConnectionAsync"/>).
    /// </summary>
    public async Task<AccountTrackingDto> TrackAccountAsync(string accountId, CancellationToken ct = default)
    {
        var conn = await db.Connections.AsNoTracking()
            .Include(c => c.Plan).Include(c => c.City)
            .FirstOrDefaultAsync(c => c.AccountId == accountId.ToUpper(), ct)
            ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, $"Không tìm thấy kết nối {accountId}.");

        var due = await db.Bills.AsNoTracking()
            .Where(b => b.AccountId == conn.AccountId)
            .SumAsync(b => b.DueAmount, ct);

        return new AccountTrackingDto(conn.AccountId, conn.ConnType, conn.Plan!.PlanName, conn.Status,
            conn.ActivationDate, conn.City!.CityName, null, due);
    }

    /// <summary>Hồ sơ của khách đang đăng nhập.</summary>
    public async Task<Customer> ProfileAsync(CurrentUser user, CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().Include(c => c.City)
            .FirstOrDefaultAsync(c => c.CustomerId == user.CustomerId, ct)
        ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hồ sơ khách hàng.");

    /// <summary>Cập nhật thông tin liên hệ; không cho đổi loại khách hay mã thành phố.</summary>
    public async Task<Customer> UpdateProfileAsync(CurrentUser user, string phone, string? email,
        string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số điện thoại là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Địa chỉ là bắt buộc.");
        }

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == user.CustomerId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy hồ sơ khách hàng.");

        customer.Phone = phone.Trim();
        customer.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        customer.Address = address.Trim();
        await db.SaveChangesAsync(ct);
        return customer;
    }

    /// <summary>Đơn của khách đang đăng nhập, mới nhất trước.</summary>
    public async Task<IReadOnlyList<object>> MyOrdersAsync(CurrentUser user, CancellationToken ct = default) =>
        await db.Orders.AsNoTracking().Include(o => o.Plan)
            .Where(o => o.CustomerId == user.CustomerId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => (object)new
            {
                o.OrderId, o.ConnType, PlanName = o.Plan!.PlanName, o.Quantity, o.ConnectedQty,
                o.DiscountPercent, o.Status, o.OrderDate
            })
            .ToListAsync(ct);

    /// <summary>Kết nối của khách kèm số nợ hiện tại và thiết bị đang cấp.</summary>
    public async Task<IReadOnlyList<object>> MyConnectionsAsync(CurrentUser user, CancellationToken ct = default)
    {
        var connections = await db.Connections.AsNoTracking()
            .Include(c => c.Plan).Include(c => c.City).Include(c => c.Equipment)
            .Where(c => c.CustomerId == user.CustomerId)
            .OrderBy(c => c.AccountId)
            .ToListAsync(ct);

        var dues = await db.Bills.AsNoTracking()
            .Where(b => connections.Select(c => c.AccountId).Contains(b.AccountId))
            .GroupBy(b => b.AccountId)
            .Select(g => new { AccountId = g.Key, Due = g.Sum(b => b.DueAmount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Due, ct);

        return connections.Select(c => (object)new
        {
            c.AccountId,
            c.ConnType,
            PlanName = c.Plan!.PlanName,
            CityName = c.City!.CityName,
            c.Status,
            c.ActivationDate,
            c.LandlineAccountId,
            DueAmount = dues.GetValueOrDefault(c.AccountId),
            Equipment = c.Equipment.Where(e => e.Status == "ISSUED")
                .Select(e => new { e.SerialNo, e.ProductId, e.IssuedDate }).ToList()
        }).ToList();
    }

    /// <summary>Hóa đơn và thanh toán của khách.</summary>
    public async Task<IReadOnlyList<object>> MyBillsAsync(CurrentUser user, CancellationToken ct = default) =>
        await db.Bills.AsNoTracking()
            .Where(b => db.Connections.Any(c => c.AccountId == b.AccountId && c.CustomerId == user.CustomerId))
            .OrderByDescending(b => b.BillDate)
            .Select(b => (object)new
            {
                b.BillId, b.AccountId, b.PeriodStart, b.PeriodEnd, b.PlanFee, b.CallCharges,
                b.OverageCharges, b.ReplacementCharges, b.TaxAmount, b.DepositAmount,
                b.TotalAmount, b.PaidAmount, b.DueAmount, b.PreviousDue, b.DueDate, b.Status,
                Payments = b.Payments.Select(p => new { p.PaymentId, p.Amount, p.Method, p.PaidDate }).ToList()
            })
            .ToListAsync(ct);

    /// <summary>Khách gửi yêu cầu đặt kết nối; nhân viên cửa hàng xác nhận sau.</summary>
    public async Task<OrderRequest> CreateOrderRequestAsync(CurrentUser user, string connType, int planId,
        int shopId, string? note, CancellationToken ct = default)
    {
        if (connType is not ("D" or "B" or "T"))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Loại kết nối phải là D, B hoặc T.");
        }

        var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == planId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy gói cước.");
        if (plan.ConnType != connType)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Gói cước không khớp loại kết nối.");
        }

        if (!await db.RetailShops.AnyAsync(s => s.ShopId == shopId && s.Status == "ACTIVE", ct))
        {
            throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy cửa hàng.");
        }

        var request = new OrderRequest
        {
            CustomerId = user.CustomerId!.Value,
            ShopId = shopId,
            ConnType = connType,
            PlanId = planId,
            Note = note,
            Status = "PENDING",
            CreatedAt = DateTime.Now
        };

        db.OrderRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request;
    }

    /// <summary>Yêu cầu đặt đơn của khách đang đăng nhập.</summary>
    public async Task<IReadOnlyList<object>> MyOrderRequestsAsync(CurrentUser user, CancellationToken ct = default) =>
        await db.OrderRequests.AsNoTracking().Include(r => r.Plan).Include(r => r.Shop)
            .Where(r => r.CustomerId == user.CustomerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (object)new
            {
                r.RequestId, r.ConnType, PlanName = r.Plan!.PlanName, ShopName = r.Shop!.ShopName,
                r.Status, r.Note, r.RejectReason, r.OrderId, r.CreatedAt
            })
            .ToListAsync(ct);

    /// <summary>Gửi phản hồi gắn với một đơn hoặc một kết nối của chính khách.</summary>
    public async Task<Feedback> CreateFeedbackAsync(CurrentUser user, string refType, string refId,
        byte? rating, string comment, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Nội dung phản hồi là bắt buộc.");
        }

        if (rating is < 1 or > 5)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Điểm đánh giá phải từ 1 đến 5.");
        }

        string? orderId = null;
        string? accountId = null;

        if (refType.Equals("ORDER", StringComparison.OrdinalIgnoreCase))
        {
            var owned = await db.Orders.AnyAsync(o => o.OrderId == refId && o.CustomerId == user.CustomerId, ct);
            if (!owned)
            {
                throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy đơn của bạn.");
            }

            orderId = refId;
        }
        else if (refType.Equals("CONNECTION", StringComparison.OrdinalIgnoreCase))
        {
            var owned = await db.Connections.AnyAsync(c => c.AccountId == refId && c.CustomerId == user.CustomerId, ct);
            if (!owned)
            {
                throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy kết nối của bạn.");
            }

            accountId = refId;
        }
        else
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Loại tham chiếu phải là ORDER hoặc CONNECTION.");
        }

        var feedback = new Feedback
        {
            CustomerId = user.CustomerId!.Value,
            OrderId = orderId,
            AccountId = accountId,
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = DateTime.Now
        };

        db.Feedbacks.Add(feedback);
        await db.SaveChangesAsync(ct);
        return feedback;
    }
}