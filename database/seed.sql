/* =============================================================================
   NEXUS - seed.sql (Task 3): cau hinh, bang gia theo de bai, tai khoan demo,
   du lieu mau de B va C test ngay ma khong phai cho task truoc.

   File UTF-8, bat buoc chay voi -f 65001 de giu tieng Viet:
       sqlcmd -S .\SQLEXPRESS -E -C -b -f 65001 -i database\seed.sql
   Script dung lai neu da co du lieu. Muon nap lai: chay reset_data.sql truoc.

   Tai khoan demo (mat khau chung Nexus@2026, chi dung de demo):
       admin, accounts, tech, retail, retail2, customer1, customer2

   Gia dinh cho mentor xac nhan (SRS phan 10):
       - STD Yearly de bai bo trong gia: tam dat 780$.
       - STD Half-yearly de bai ghi "1 thang": tam coi la 6 thang.
       - Gia vuot gio: gia goi chia so gio.
   ========================================================================== */
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO
USE NEXUS;
GO
:on error exit
IF EXISTS (SELECT 1 FROM Cities)
    RAISERROR(N'Database NEXUS da co du lieu. Chay reset_data.sql truoc neu muon nap lai.', 16, 1);
GO
BEGIN TRANSACTION;

/* --- Cau hinh ------------------------------------------------------------ */
INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description) VALUES
    ('TAX_RATE',      '0.1224', N'Thuế dịch vụ 12,24%'),
    ('DEPOSIT_D',     '325.00', N'Tiền đặt cọc Dial-up'),
    ('DEPOSIT_B',     '500.00', N'Tiền đặt cọc Broadband'),
    ('DEPOSIT_T',     '250.00', N'Tiền đặt cọc Landline'),
    ('OVERDUE_DAYS',  '30',     N'Quá hạn bao nhiêu ngày thì kế toán đề nghị tạm ngưng'),
    ('BILL_DUE_DAYS', '15',     N'Hạn thanh toán tính từ ngày lập hóa đơn');

-- Bien thuoc bac thap hon: dung 15 = 25%, dung 25 = 50%, dung 50 = 75%. Duoi 10 khong giam.
INSERT INTO BulkDiscounts (MinQty, MaxQty, DiscountPercent) VALUES
    (10, 15, 25), (16, 25, 50), (26, 50, 75), (51, NULL, 100);

INSERT INTO Sequences (SeqName, CurrentValue) VALUES
    ('ORDER_D', 2), ('ORDER_T', 2), ('ORDER_B', 4), ('ACCOUNT', 6);

/* --- Thanh pho, cua hang, nhan vien, khach hang --------------------------- */
INSERT INTO Cities (CityName, CityCode) VALUES       -- CityId 1..3
    (N'Hà Nội', '001'), (N'Hồ Chí Minh', '002'), (N'Đà Nẵng', '003');

INSERT INTO RetailShops (ShopName, Address, CityId, Phone) VALUES   -- ShopId 1..2
    (N'Nexus Hoàn Kiếm', N'12 Hàng Bài, Hoàn Kiếm', 1, '02438250001'),
    (N'Nexus Quận 1', N'45 Lê Lợi, Quận 1', 2, '02838220002');

INSERT INTO Employees (FullName, Role, Phone, Email, ShopId) VALUES -- EmployeeId 1..5
    (N'Quản lý Demo', 'ADMIN', '0900000001', 'admin@nexus.test', NULL),
    (N'Kế toán Demo', 'ACCOUNTS', '0900000002', 'accounts@nexus.test', NULL),
    (N'Kỹ thuật Demo', 'TECH', '0900000003', 'tech@nexus.test', NULL),
    (N'Cửa hàng Hà Nội', 'RETAIL', '0900000004', 'retail@nexus.test', 1),
    (N'Cửa hàng HCM', 'RETAIL', '0900000005', 'retail2@nexus.test', 2);

INSERT INTO Customers (FullName, Address, CityId, Phone, Email, CustomerType, CreatedByEmployeeId) VALUES -- CustomerId 1..3
    (N'Nguyễn Văn An', N'5 Tràng Tiền, Hoàn Kiếm', 1, '0901234567', 'an@example.test', 'INDIVIDUAL', 4),
    (N'Công ty TNHH ABC', N'20 Phố Huế, Hai Bà Trưng', 1, '0912345678', 'abc@example.test', 'CORPORATE', 4),
    (N'Trần Thị Bình', N'88 Nguyễn Huệ, Quận 1', 2, '0987654321', 'binh@example.test', 'INDIVIDUAL', 5);

-- Hash BCrypt cua mat khau demo. MustChangePassword = 0 de demo khong bi chan.
DECLARE @h VARCHAR(100) = '$2a$10$ZQlsMIoPzOUSR.mtV82QSedo416qhA2thGhJCYj4KHen7ECLjXHvK';
INSERT INTO UserAccounts (Username, PasswordHash, Role, EmployeeId, CustomerId) VALUES
    ('admin', @h, 'ADMIN', 1, NULL),
    ('accounts', @h, 'ACCOUNTS', 2, NULL),
    ('tech', @h, 'TECH', 3, NULL),
    ('retail', @h, 'RETAIL', 4, NULL),
    ('retail2', @h, 'RETAIL', 5, NULL),
    ('customer1', @h, 'CUSTOMER', NULL, 1),
    ('customer2', @h, 'CUSTOMER', NULL, 3);

/* --- Goi cuoc theo de bai -------------------------------------------------- */
INSERT INTO Plans (ConnType, GroupName, PlanName, Price, ValidityMonths, HoursIncluded, OverageRatePerHour, SpeedKbps, Description) VALUES
    -- Dial-up: PlanId 1..7
    ('D', N'Theo giờ', N'10 giờ', 50, 1, 10, 5.00, NULL, N'10 giờ, hiệu lực 1 tháng'),
    ('D', N'Theo giờ', N'30 giờ', 130, 3, 30, 4.33, NULL, N'30 giờ, hiệu lực 3 tháng'),
    ('D', N'Theo giờ', N'60 giờ', 260, 6, 60, 4.33, NULL, N'60 giờ, hiệu lực 6 tháng'),
    ('D', N'Không giới hạn 28 Kbps', N'Tháng', 75, 1, NULL, NULL, 28, NULL),
    ('D', N'Không giới hạn 28 Kbps', N'Quý', 150, 3, NULL, NULL, 28, NULL),
    ('D', N'Không giới hạn 56 Kbps', N'Tháng', 100, 1, NULL, NULL, 56, NULL),
    ('D', N'Không giới hạn 56 Kbps', N'Quý', 180, 3, NULL, NULL, 56, NULL),
    -- Broadband: PlanId 8..13
    ('B', N'Theo giờ', N'30 giờ', 175, 1, 30, 5.83, NULL, N'30 giờ, hiệu lực 1 tháng'),
    ('B', N'Theo giờ', N'60 giờ', 315, 6, 60, 5.25, NULL, N'60 giờ, hiệu lực 6 tháng'),
    ('B', N'Không giới hạn 64 Kbps', N'Tháng', 225, 1, NULL, NULL, 64, NULL),
    ('B', N'Không giới hạn 64 Kbps', N'Quý', 400, 3, NULL, NULL, 64, NULL),
    ('B', N'Không giới hạn 128 Kbps', N'Tháng', 350, 1, NULL, NULL, 128, NULL),
    ('B', N'Không giới hạn 128 Kbps', N'Quý', 445, 3, NULL, NULL, 128, NULL),
    -- Landline: PlanId 14..18
    ('T', N'Local', N'Unlimited', 75, 12, NULL, NULL, NULL, N'Thuê bao 1 năm'),
    ('T', N'Local', N'Monthly', 35, 1, NULL, NULL, NULL, N'Thuê bao 1 tháng'),
    ('T', N'STD', N'Monthly', 125, 1, NULL, NULL, NULL, N'Thuê bao 1 tháng'),
    ('T', N'STD', N'Half-yearly', 420, 6, NULL, NULL, NULL, N'Giả định 6 tháng, đề ghi 1 tháng, chờ mentor xác nhận'),
    ('T', N'STD', N'Yearly', 780, 12, NULL, NULL, NULL, N'Đề bỏ trống giá, tạm đặt 780$, chờ mentor xác nhận');

INSERT INTO PlanCallRates (PlanId, CallType, RatePerMin) VALUES
    (14, 'LOCAL', 0.55),
    (15, 'LOCAL', 0.75),
    (16, 'LOCAL', 0.70), (16, 'STD', 2.25), (16, 'SMS', 1.00),
    (17, 'LOCAL', 0.60), (17, 'STD', 2.00), (17, 'SMS', 1.15),
    (18, 'LOCAL', 0.60), (18, 'STD', 1.75), (18, 'SMS', 1.25);

/* --- Nha cung cap, thiet bi, nhap hang ------------------------------------- */
INSERT INTO Vendors (VendorName, Contact, Phone, Email) VALUES
    (N'Công ty Thiết bị Mạng Việt', N'Phòng kinh doanh', '02439990001', 'sales@vendor1.test'),
    (N'NetParts Distribution', N'Sales team', '02839990002', 'sales@vendor2.test');

INSERT INTO Products (ProductName, ProductType, VendorId, Price, StockQty) VALUES -- ProductId 1..2
    (N'Modem ADSL', 'MODEM', 1, 40.00, 50),
    (N'Router WiFi', 'ROUTER', 2, 65.00, 30);

INSERT INTO Purchases (VendorId, PurchaseDate, TotalAmount, AmountPaid, ReceivedAt) VALUES
    (1, '2026-06-15', 2000.00, 2000.00, '2026-06-20T09:00:00');
INSERT INTO PurchaseItems (PurchaseId, ProductId, Quantity, UnitPrice) VALUES (1, 1, 50, 40.00);

/* --- Don va ket noi mau ---------------------------------------------------- */
-- Don Landline, da co hoa don: dung de test no ky truoc, qua han, phi thay thiet bi.
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, Quantity, ConnectedQty, PlanFee, DepositAmount, OrderDate, Status, CheckedByEmployeeId) VALUES
    ('T0000000001', 3, 2, 5, 'T', 16, 1, 1, 125.00, 250.00, '2026-06-25T10:00:00', 'CONNECTED', 3);
INSERT INTO Connections (AccountId, OrderId, CustomerId, ConnType, PlanId, CityId, ActivationDate, CreatedByEmployeeId) VALUES
    ('T002000000000003', 'T0000000001', 3, 'T', 16, 2, '2026-07-01', 3);

-- Broadband 1 ket noi: hoa don ky dau phai ra 752,54$ (vi du trong SRS).
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, EquipmentProductId, Quantity, ConnectedQty, PlanFee, DepositAmount, OrderDate, Status, CheckedByEmployeeId) VALUES
    ('B0000000001', 1, 1, 4, 'B', 10, 2, 1, 1, 225.00, 500.00, '2026-10-20T09:00:00', 'CONNECTED', 3);
INSERT INTO Connections (AccountId, OrderId, CustomerId, ConnType, PlanId, CityId, ActivationDate, CreatedByEmployeeId) VALUES
    ('B001000000000001', 'B0000000001', 1, 'B', 10, 1, '2026-10-25', 3);

-- Broadband doanh nghiep 10 ket noi, giam 25%, moi cap 1: hoa don ky dau phai ra 564,41$.
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, EquipmentProductId, Quantity, ConnectedQty, DiscountPercent, PlanFee, DepositAmount, OrderDate, Status, CheckedByEmployeeId) VALUES
    ('B0000000002', 2, 1, 4, 'B', 10, 2, 10, 1, 25, 225.00, 500.00, '2026-10-21T09:00:00', 'FEASIBLE', 3);
INSERT INTO Connections (AccountId, OrderId, CustomerId, ConnType, PlanId, CityId, ActivationDate, CreatedByEmployeeId) VALUES
    ('B001000000000002', 'B0000000002', 2, 'B', 10, 1, '2026-10-26', 3);

-- Dial-up theo gio 30 gio / 3 thang, dung landline Nexus da co: test vuot gio luy ke.
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, EquipmentProductId, Quantity, ConnectedQty, PlanFee, DepositAmount, ExistingLandlineAccountId, OrderDate, Status, CheckedByEmployeeId) VALUES
    ('D0000000001', 3, 2, 5, 'D', 2, 1, 1, 1, 130.00, 325.00, 'T002000000000003', '2026-10-22T09:00:00', 'CONNECTED', 3);
INSERT INTO Connections (AccountId, OrderId, CustomerId, ConnType, PlanId, CityId, LandlineAccountId, ActivationDate, CreatedByEmployeeId) VALUES
    ('D002000000000004', 'D0000000001', 3, 'D', 2, 2, 'T002000000000003', '2026-10-27', 3);

-- Dial-up dang ky moi ca landline: mot don tao hai ket noi T va D, hai khoan coc.
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, EquipmentProductId, Quantity, ConnectedQty, PlanFee, DepositAmount, LandlinePlanId, LandlinePlanFee, LandlineDepositAmount, OrderDate, Status, CheckedByEmployeeId) VALUES
    ('D0000000002', 1, 1, 4, 'D', 4, 1, 1, 1, 75.00, 325.00, 15, 35.00, 250.00, '2026-10-23T09:00:00', 'CONNECTED', 3);
INSERT INTO Connections (AccountId, OrderId, CustomerId, ConnType, PlanId, CityId, ActivationDate, CreatedByEmployeeId) VALUES
    ('T001000000000005', 'D0000000002', 1, 'T', 15, 1, '2026-10-28', 3);
INSERT INTO Connections (AccountId, OrderId, CustomerId, ConnType, PlanId, CityId, LandlineAccountId, ActivationDate, CreatedByEmployeeId) VALUES
    ('D001000000000006', 'D0000000002', 1, 'D', 4, 1, 'T001000000000005', '2026-10-28', 3);

-- Cac don o trang thai khac de B va C test danh sach, kiem tra kha thi, cap ket noi.
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, EquipmentProductId, Quantity, PlanFee, DepositAmount, OrderDate, Status) VALUES
    ('B0000000003', 1, 1, 4, 'B', 12, 2, 1, 350.00, 500.00, '2026-11-01T09:00:00', 'SUBMITTED');
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, Quantity, PlanFee, DepositAmount, OrderDate, Status, DistanceKm, ServerAvailable, LandlineFeasible, CheckedByEmployeeId) VALUES
    ('T0000000002', 2, 1, 4, 'T', 15, 1, 35.00, 250.00, '2026-11-01T10:00:00', 'FEASIBLE', 1.20, 1, 1, 3);
INSERT INTO Orders (OrderId, CustomerId, ShopId, EmployeeId, ConnType, PlanId, EquipmentProductId, Quantity, PlanFee, DepositAmount, OrderDate, Status, DistanceKm, ServerAvailable, InternetFeasible, FeasibilityReason, CheckedByEmployeeId) VALUES
    ('B0000000004', 3, 2, 5, 'B', 8, 1, 1, 175.00, 500.00, '2026-11-02T09:00:00', 'NOT_FEASIBLE', 18.50, 0, 0, N'Khoảng cách quá xa, máy chủ khu vực hết cổng', 3);

INSERT INTO OrderStatusHistory (OrderId, Status, ActorUserId, ChangedAt, Note) VALUES
    ('B0000000001', 'SUBMITTED', 4, '2026-10-20T09:00:00', NULL),
    ('B0000000001', 'FEASIBLE', 3, '2026-10-22T09:00:00', NULL),
    ('B0000000001', 'CONNECTED', 3, '2026-10-25T09:00:00', NULL),
    ('B0000000003', 'SUBMITTED', 4, '2026-11-01T09:00:00', NULL),
    ('B0000000004', 'SUBMITTED', 5, '2026-11-02T09:00:00', NULL),
    ('B0000000004', 'NOT_FEASIBLE', 3, '2026-11-03T09:00:00', N'Khoảng cách quá xa');

INSERT INTO OrderRequests (CustomerId, ShopId, ConnType, PlanId, Note) VALUES
    (1, 1, 'B', 13, N'Khách muốn chuyển sang gói quý 128 Kbps');

INSERT INTO ConnectionEquipment (AccountId, ProductId, SerialNo, IssuedDate) VALUES
    ('B001000000000001', 2, 'RT-0001', '2026-10-25'),
    ('B001000000000002', 2, 'RT-0002', '2026-10-26'),
    ('D002000000000004', 1, 'MD-0001', '2026-10-27'),
    ('D001000000000006', 1, 'MD-0002', '2026-10-28');

/* --- Hoa don mau cua T002000000000003 -------------------------------------- */
-- Ky 7: 125 + 100 phut Local x 0,70 = 195; thue 23,87; coc 250; tong 468,87; da tra du.
INSERT INTO Bills (AccountId, PeriodStart, PeriodEnd, PlanFee, CallCharges, TaxableBase, TaxRate, TaxAmount, DepositAmount, TotalAmount, PaidAmount, DueAmount, BillDate, DueDate, Status, CreatedByEmployeeId) VALUES
    ('T002000000000003', '2026-07-01', '2026-07-31', 125.00, 70.00, 195.00, 0.1224, 23.87, 250.00, 468.87, 468.87, 0.00, '2026-08-01', '2026-08-16', 'PAID', 2);
INSERT INTO BillCallUsage (BillId, CallType, Minutes, RatePerMin, Amount) VALUES (1, 'LOCAL', 100, 0.70, 70.00);
INSERT INTO Payments (BillId, Amount, Method, PaidDate, ReceivedByEmployeeId) VALUES (1, 468.87, 'CASH', '2026-08-05', 5);

-- Ky 8: 125; thue 15,30; tong 140,30; chua tra, da qua han hon 30 ngay.
INSERT INTO Bills (AccountId, PeriodStart, PeriodEnd, PlanFee, TaxableBase, TaxRate, TaxAmount, TotalAmount, DueAmount, BillDate, DueDate, Status, CreatedByEmployeeId) VALUES
    ('T002000000000003', '2026-08-01', '2026-08-31', 125.00, 125.00, 0.1224, 15.30, 140.30, 140.30, '2026-09-01', '2026-09-16', 'UNPAID', 2);

-- Phi thay thiet bi chua lap hoa don: ky ke tiep phai tu lay.
INSERT INTO ReplacementCharges (AccountId, ProductId, Amount, ChargeDate, Note) VALUES
    ('T002000000000003', 1, 40.00, '2026-09-10', N'Thay thiết bị hỏng do khách');

INSERT INTO Feedback (CustomerId, AccountId, Rating, Comment, RecordedByEmployeeId) VALUES
    (3, 'T002000000000003', 4, N'Nhân viên hỗ trợ nhanh, chất lượng cuộc gọi tốt.', 5);

COMMIT;
PRINT 'NEXUS seed loaded.';
GO
