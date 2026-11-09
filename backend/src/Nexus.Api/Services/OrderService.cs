using Microsoft.EntityFrameworkCore;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Domain;

namespace Nexus.Api.Services;

/// <summary>Dữ liệu tạo đơn từ form của nhân viên cửa hàng.</summary>
public sealed record CreateOrderRequest(
    int CustomerId,
    string ConnType,
    int PlanId,
    int? EquipmentProductId,
    int Quantity,
    string? ExistingLandlineAccountId,
    int? LandlinePlanId,
    int? RequestId);

/// <summary>
/// Tạo đơn, kiểm tra khả thi, cấp kết nối (FR-04, FR-05, FR-06).
/// Mọi thao tác nhiều bước nằm trong một transaction của EF Core.
/// </summary>
public class OrderService(NexusDbContext db, DiscountService discounts, IdGenerator ids, ConfigService config)
{
    /// <summary>
    /// Tạo đơn mới (API Design 4.5): kiểm tra dữ liệu, sinh Order ID, chụp giá tại thời điểm đặt,
    /// áp ưu đãi theo số lượng. Dial-up bắt buộc có landline Nexus (đăng ký mới hoặc đã có).
    /// </summary>
    public async Task<Order> CreateAsync(CurrentUser actor, CreateOrderRequest req, CancellationToken ct = default)
    {
        ValidateConnType(req.ConnType);
        if (req.Quantity < 1)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Số lượng kết nối phải từ 1 trở lên.");
        }

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == req.CustomerId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy khách hàng.");

        var plan = await db.Plans.FirstOrDefaultAsync(p => p.PlanId == req.PlanId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy gói cước.");

        if (plan.ConnType != req.ConnType)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Gói cước không khớp loại kết nối.");
        }

        if (plan.Status != "ACTIVE")
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Gói cước đã ngưng áp dụng.");
        }

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == actor.EmployeeId, ct)
            ?? throw new BusinessException(ErrorCode.FORBIDDEN, "Tài khoản không gắn với nhân viên.");
        if (employee.ShopId is null)
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Nhân viên cửa hàng chưa được gán cửa hàng.");
        }

        // Dial-up: phải đăng ký landline mới, hoặc dùng landline Nexus đang ACTIVE của chính khách.
        Plan? landlinePlan = null;
        string? existingLandline = null;

        if (req.ConnType == "D")
        {
            if (req.LandlinePlanId is not null)
            {
                landlinePlan = await db.Plans.FirstOrDefaultAsync(p => p.PlanId == req.LandlinePlanId, ct)
                    ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy gói landline.");
                if (landlinePlan.ConnType != "T")
                {
                    throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Gói đính kèm phải là gói Landline.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(req.ExistingLandlineAccountId))
            {
                // Server tự kiểm tra sở hữu và trạng thái, không tin dữ liệu từ form.
                var landline = await db.Connections.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.AccountId == req.ExistingLandlineAccountId, ct)
                    ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, "Không tìm thấy landline đã khai báo.");

                if (landline.ConnType != "T" || landline.CustomerId != req.CustomerId || landline.Status != ConnectionStatus.Active)
                {
                    throw new BusinessException(ErrorCode.LANDLINE_REQUIRED,
                        "Landline phải là kết nối loại T đang hoạt động của đúng khách hàng này.");
                }

                if (req.Quantity != 1)
                {
                    throw new BusinessException(ErrorCode.LANDLINE_REQUIRED,
                        "Đơn dùng landline có sẵn chỉ được có một kết nối Dial-up.");
                }

                existingLandline = landline.AccountId;
            }
            else
            {
                throw new BusinessException(ErrorCode.LANDLINE_REQUIRED,
                    "Dial-up cần landline Nexus: chọn gói landline mới hoặc landline đang dùng.");
            }
        }

        var discountPercent = await discounts.PercentForAsync(req.Quantity, ct);

        var order = new Order
        {
            CustomerId = customer.CustomerId,
            ShopId = employee.ShopId.Value,
            EmployeeId = employee.EmployeeId,
            ConnType = req.ConnType,
            PlanId = plan.PlanId,
            EquipmentProductId = req.EquipmentProductId,
            Quantity = req.Quantity,
            ConnectedQty = 0,
            DiscountPercent = discountPercent,
            // Lưu giá TRƯỚC ưu đãi; hóa đơn tự nhân (1 − % ưu đãi).
            PlanFee = plan.Price,
            DepositAmount = await DepositForAsync(req.ConnType, ct),
            LandlinePlanId = landlinePlan?.PlanId,
            LandlinePlanFee = landlinePlan?.Price,
            LandlineDepositAmount = landlinePlan is null ? null : await DepositForAsync("T", ct),
            ExistingLandlineAccountId = existingLandline,
            OrderDate = DateTime.Now,
            Status = OrderStatus.Submitted
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        order.OrderId = await ids.NextOrderIdAsync(req.ConnType, ct);
        db.Orders.Add(order);
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            Status = OrderStatus.Submitted,
            ActorUserId = actor.UserId,
            ChangedAt = DateTime.Now,
            Note = "Tạo đơn tại cửa hàng"
        });

        if (req.RequestId is not null)
        {
            var request = await db.OrderRequests.FirstOrDefaultAsync(r => r.RequestId == req.RequestId, ct);
            if (request is null || request.Status != "PENDING")
            {
                throw new BusinessException(ErrorCode.REQUEST_STATE_INVALID, "Yêu cầu đặt đơn không còn chờ xử lý.");
            }

            request.Status = "CONFIRMED";
            request.OrderId = order.OrderId;
            request.HandledByEmployeeId = employee.EmployeeId;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "CREATE_ORDER",
            EntityType = "Order",
            EntityId = order.OrderId,
            Detail = $"{req.ConnType} x{req.Quantity}, giảm {discountPercent}%"
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return order;
    }

    /// <summary>Chuyển đơn sang trạng thái đang kiểm tra khả thi (FR-05).</summary>
    public async Task StartFeasibilityCheckAsync(CurrentUser actor, string orderId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, ct);
        if (order.Status != OrderStatus.Submitted)
        {
            throw new BusinessException(ErrorCode.ORDER_STATE_INVALID,
                "Chỉ đơn đang chờ mới bắt đầu kiểm tra khả thi.");
        }

        await ChangeStatusAsync(actor, order, OrderStatus.FeasibilityCheck, "Kỹ thuật tiếp nhận đơn", ct);
    }

    /// <summary>Ghi kết quả khảo sát: FEASIBLE hoặc NOT_FEASIBLE kèm lý do (FR-05).</summary>
    public async Task RecordFeasibilityAsync(CurrentUser actor, string orderId, bool feasible,
        decimal? distanceKm, bool? serverAvailable, bool? landlineFeasible, bool? internetFeasible,
        string? reason, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, ct);
        if (order.Status is not (OrderStatus.Submitted or OrderStatus.FeasibilityCheck))
        {
            throw new BusinessException(ErrorCode.ORDER_STATE_INVALID,
                "Đơn đã được xử lý khả thi trước đó.");
        }

        if (!feasible && string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Phải ghi lý do khi đơn không khả thi.");
        }

        order.DistanceKm = distanceKm;
        order.ServerAvailable = serverAvailable;
        order.LandlineFeasible = landlineFeasible;
        order.InternetFeasible = internetFeasible;
        order.FeasibilityReason = feasible ? null : reason;
        order.CheckedByEmployeeId = actor.EmployeeId;

        await ChangeStatusAsync(actor, order, feasible ? OrderStatus.Feasible : OrderStatus.NotFeasible,
            feasible ? "Khảo sát đạt" : reason, ct);
    }

    /// <summary>
    /// Cấp một kết nối từ đơn khả thi, sinh Account ID (FR-06). Đơn nhiều kết nối thì gọi nhiều lần.
    /// Trừ tồn kho thiết bị; Dial-up đăng ký landline mới thì tạo luôn kết nối T trước rồi gắn vào D.
    /// </summary>
    public async Task<Connection> CreateConnectionAsync(CurrentUser actor, string orderId,
        DateOnly activationDate, int? equipmentProductId, string? equipmentSerial, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct)
            ?? throw new BusinessException(ErrorCode.ORDER_NOT_FOUND, $"Không tìm thấy đơn {orderId}.");

        if (order.Status != OrderStatus.Feasible)
        {
            throw new BusinessException(ErrorCode.NOT_FEASIBLE, "Đơn chưa ở trạng thái khả thi.");
        }

        if (order.ConnectedQty >= order.Quantity)
        {
            throw new BusinessException(ErrorCode.ORDER_STATE_INVALID, "Đơn đã cấp đủ số kết nối.");
        }

        var cityId = order.Customer!.CityId;
        var cityCode = await db.Cities.Where(c => c.CityId == cityId)
            .Select(c => c.CityCode).FirstAsync(ct);

        // Đơn Dial-up đăng ký landline mới: tạo landline trước để Dial-up có chỗ gắn vào.
        string? landlineAccountId = order.ExistingLandlineAccountId;
        if (order.ConnType == "D" && order.LandlinePlanId is not null && order.ConnectedQty == 0)
        {
            var landline = new Connection
            {
                AccountId = await ids.NextAccountIdAsync("T", cityCode, ct),
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                ConnType = "T",
                PlanId = order.LandlinePlanId.Value,
                CityId = cityId,
                ActivationDate = activationDate,
                Status = ConnectionStatus.Active,
                CreatedByEmployeeId = actor.EmployeeId
            };
            db.Connections.Add(landline);
            landlineAccountId = landline.AccountId;
        }

        if (order.ConnType == "D" && landlineAccountId is null)
        {
            throw new BusinessException(ErrorCode.LANDLINE_REQUIRED, "Dial-up phải gắn với một landline Nexus.");
        }

        var connection = new Connection
        {
            AccountId = await ids.NextAccountIdAsync(order.ConnType, cityCode, ct),
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            ConnType = order.ConnType,
            PlanId = order.PlanId,
            CityId = cityId,
            LandlineAccountId = order.ConnType == "D" ? landlineAccountId : null,
            ActivationDate = activationDate,
            Status = ConnectionStatus.Active,
            CreatedByEmployeeId = actor.EmployeeId
        };
        db.Connections.Add(connection);

        var productId = equipmentProductId ?? order.EquipmentProductId;
        if (productId is not null)
        {
            await IssueEquipmentAsync(connection.AccountId, productId.Value, equipmentSerial, activationDate, ct);
        }

        order.ConnectedQty += 1;
        if (order.ConnectedQty >= order.Quantity)
        {
            order.Status = OrderStatus.Connected;
            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = OrderStatus.Connected,
                ActorUserId = actor.UserId,
                ChangedAt = DateTime.Now,
                Note = "Đã cấp đủ kết nối"
            });
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "CREATE_CONNECTION",
            EntityType = "Connection",
            EntityId = connection.AccountId,
            Detail = $"Đơn {order.OrderId}"
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return connection;
    }

    /// <summary>Chuyển trạng thái kết nối hợp lệ: ACTIVE ⇄ TEMP_INACTIVE → PERM_INACTIVE (FR-07).</summary>
    public async Task ChangeConnectionStatusAsync(CurrentUser actor, string accountId, string targetStatus,
        string? reason, CancellationToken ct = default)
    {
        if (!ConnectionStatus.All.Contains(targetStatus))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Trạng thái kết nối không hợp lệ.");
        }

        var connection = await db.Connections.FirstOrDefaultAsync(c => c.AccountId == accountId, ct)
            ?? throw new BusinessException(ErrorCode.ACCOUNT_NOT_FOUND, $"Không tìm thấy kết nối {accountId}.");

        var allowed = (connection.Status, targetStatus) switch
        {
            (ConnectionStatus.Active, ConnectionStatus.TempInactive) => true,
            (ConnectionStatus.TempInactive, ConnectionStatus.Active) => true,
            (ConnectionStatus.Active or ConnectionStatus.TempInactive, ConnectionStatus.PermInactive) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new BusinessException(ErrorCode.CONNECTION_STATE_INVALID,
                $"Không thể chuyển từ {connection.Status} sang {targetStatus}.");
        }

        // Không ngưng vĩnh viễn landline khi còn Dial-up gắn với nó đang hoạt động.
        if (targetStatus == ConnectionStatus.PermInactive && connection.ConnType == "T")
        {
            var inUse = await db.Connections.AnyAsync(c =>
                c.LandlineAccountId == accountId && c.Status != ConnectionStatus.PermInactive, ct);
            if (inUse)
            {
                throw new BusinessException(ErrorCode.LANDLINE_IN_USE,
                    "Landline đang phục vụ Dial-up còn hoạt động.");
            }
        }

        connection.Status = targetStatus;
        connection.StatusReason = reason;
        if (targetStatus != ConnectionStatus.TempInactive)
        {
            connection.SuspendRequested = false;
            connection.SuspendRequestReason = null;
            connection.SuspendRequestedAt = null;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "CHANGE_CONNECTION_STATUS",
            EntityType = "Connection",
            EntityId = accountId,
            Detail = $"{targetStatus}: {reason}"
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Thay thiết bị hỏng, tạo phí chờ lập hóa đơn (FR-08).</summary>
    public async Task ReplaceEquipmentAsync(CurrentUser actor, string accountId, string oldSerial,
        int newProductId, decimal replacementFee, CancellationToken ct = default)
    {
        var old = await db.ConnectionEquipments
            .FirstOrDefaultAsync(e => e.AccountId == accountId && e.SerialNo == oldSerial && e.Status == "ISSUED", ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy thiết bị đang cấp cho kết nối.");

        old.Status = "REPLACED";
        await IssueEquipmentAsync(accountId, newProductId, null, DateOnly.FromDateTime(DateTime.Now), ct);

        db.ReplacementCharges.Add(new ReplacementCharge
        {
            AccountId = accountId,
            ProductId = newProductId,
            Amount = replacementFee,
            ChargeDate = DateOnly.FromDateTime(DateTime.Now),
            Status = "UNBILLED",
            Note = $"Thay thiết bị {oldSerial}"
        });

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "REPLACE_EQUIPMENT",
            EntityType = "Connection",
            EntityId = accountId,
            Detail = $"Phí {replacementFee}"
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Trừ tồn kho và gắn thiết bị; thiếu hàng thì báo để transaction rollback.</summary>
    private async Task IssueEquipmentAsync(string accountId, int productId, string? serial,
        DateOnly issuedDate, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct)
            ?? throw new BusinessException(ErrorCode.NOT_FOUND, "Không tìm thấy thiết bị.");

        if (product.StockQty <= 0)
        {
            throw new BusinessException(ErrorCode.OUT_OF_STOCK, $"Thiết bị {product.ProductName} đã hết hàng.");
        }

        product.StockQty -= 1;
        db.ConnectionEquipments.Add(new ConnectionEquipment
        {
            AccountId = accountId,
            ProductId = productId,
            SerialNo = string.IsNullOrWhiteSpace(serial) ? $"SN-{Guid.NewGuid():N}"[..16].ToUpperInvariant() : serial,
            IssuedDate = issuedDate,
            Status = "ISSUED"
        });
    }

    private async Task<Order> LoadOrderAsync(string orderId, CancellationToken ct) =>
        await db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct)
        ?? throw new BusinessException(ErrorCode.ORDER_NOT_FOUND, $"Không tìm thấy đơn {orderId}.");

    private async Task ChangeStatusAsync(CurrentUser actor, Order order, string status, string? note,
        CancellationToken ct)
    {
        order.Status = status;
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            Status = status,
            ActorUserId = actor.UserId,
            ChangedAt = DateTime.Now,
            Note = note
        });
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.UserId,
            Action = "ORDER_STATUS",
            EntityType = "Order",
            EntityId = order.OrderId,
            Detail = status
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Tiền đặt cọc đọc từ SystemConfig: DEPOSIT_D / DEPOSIT_B / DEPOSIT_T.</summary>
    private async Task<decimal> DepositForAsync(string connType, CancellationToken ct) =>
        await config.DecimalAsync("DEPOSIT_" + connType.ToUpperInvariant(), connType switch
        {
            "D" => 325m,
            "B" => 500m,
            _ => 250m
        }, ct);

    private static void ValidateConnType(string connType)
    {
        if (connType is not ("D" or "B" or "T"))
        {
            throw new BusinessException(ErrorCode.VALIDATION_ERROR, "Loại kết nối phải là D, B hoặc T.");
        }
    }
}