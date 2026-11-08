/* =============================================================================
   NEXUS SERVICE MARKETING SYSTEM - schema.sql (Task 2)
   CSDL: SQL Server 2019 tro len. Dong bo voi SRS 1.1 (muc 6) va API Design 1.1.

   Cach chay (chi tao moi, script dung lai neu database NEXUS da co bang):
       sqlcmd -S .\SQLEXPRESS -E -C -b -i database\schema.sql
       sqlcmd -S .\SQLEXPRESS -E -C -b -f 65001 -i database\seed.sql
   Muon tao lai tu dau: tu xoa database NEXUS bang tay, sau khi da sao luu.

   Quy uoc:
   - Tien: DECIMAL(12,2). Ti le phan tram: DECIMAL(5,2). Thue suat luu trong SystemConfig.
   - Ten bang so nhieu (Orders, Plans...) vi ORDER, PLAN la tu khoa cua SQL Server.
   - Khong xoa cung don/ket noi/hoa don/nhan vien: dung cot Status.
   - Thay doi sau khi chot: chi THEM cot, khong doi ten (de B va C khong bi vo code).
   ========================================================================== */
-- sqlcmd mac dinh tat QUOTED_IDENTIFIER, trong khi filtered index bat buoc phai bat.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO
USE master;
GO
:on error exit
-- Collation tieng Viet de sap xep va tim kiem ten dung; dung NVARCHAR cho moi cot van ban.
IF DB_ID(N'NEXUS') IS NULL
    CREATE DATABASE NEXUS COLLATE Vietnamese_100_CI_AS;
GO
USE NEXUS;
GO
-- Khong tu xoa du lieu: database da co bang thi bao loi va dung han (sqlcmd -b).
IF EXISTS (SELECT 1 FROM sys.tables)
    RAISERROR(N'Database NEXUS da co bang. Script khong ghi de du lieu.', 16, 1);
GO

/* ---------------------------------------------------------------------------
   1. Danh muc nen: thanh pho, cua hang, nhan vien, khach hang, tai khoan
   ------------------------------------------------------------------------ */

-- Thanh pho. CityCode 3 chu so la mot phan cua Account ID (BR-02).
CREATE TABLE Cities (
    CityId      INT IDENTITY(1,1) CONSTRAINT PK_Cities PRIMARY KEY,
    CityName    NVARCHAR(100) NOT NULL,
    CityCode    CHAR(3)       NOT NULL,
    Status      VARCHAR(10)   NOT NULL CONSTRAINT DF_Cities_Status DEFAULT 'ACTIVE',
    CONSTRAINT UQ_Cities_Code UNIQUE (CityCode),
    CONSTRAINT CK_Cities_Code CHECK (CityCode LIKE '[0-9][0-9][0-9]'),
    CONSTRAINT CK_Cities_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
);

-- Cua hang ban le.
CREATE TABLE RetailShops (
    ShopId      INT IDENTITY(1,1) CONSTRAINT PK_RetailShops PRIMARY KEY,
    ShopName    NVARCHAR(150) NOT NULL,
    Address     NVARCHAR(255) NOT NULL,
    CityId      INT           NOT NULL CONSTRAINT FK_Shops_City REFERENCES Cities(CityId),
    Phone       VARCHAR(20)   NULL,
    Status      VARCHAR(10)   NOT NULL CONSTRAINT DF_Shops_Status DEFAULT 'ACTIVE',
    CONSTRAINT CK_Shops_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
);

-- Nhan vien. ShopId chi bat buoc voi nhan vien cua hang.
CREATE TABLE Employees (
    EmployeeId  INT IDENTITY(1,1) CONSTRAINT PK_Employees PRIMARY KEY,
    FullName    NVARCHAR(150) NOT NULL,
    Role        VARCHAR(10)   NOT NULL,
    Phone       VARCHAR(20)   NULL,
    Email       VARCHAR(150)  NULL,
    ShopId      INT           NULL CONSTRAINT FK_Employees_Shop REFERENCES RetailShops(ShopId),
    Status      VARCHAR(10)   NOT NULL CONSTRAINT DF_Employees_Status DEFAULT 'ACTIVE',
    CONSTRAINT CK_Employees_Role CHECK (Role IN ('ADMIN','ACCOUNTS','TECH','RETAIL')),
    CONSTRAINT CK_Employees_Status CHECK (Status IN ('ACTIVE','INACTIVE')),
    CONSTRAINT CK_Employees_RetailShop CHECK (Role <> 'RETAIL' OR ShopId IS NOT NULL)
);

-- Khach hang. AddressProofPath: duong dan file giay to dia chi da upload (FR-RET-09).
CREATE TABLE Customers (
    CustomerId       INT IDENTITY(1,1) CONSTRAINT PK_Customers PRIMARY KEY,
    FullName         NVARCHAR(150) NOT NULL,
    Address          NVARCHAR(255) NOT NULL,
    CityId           INT           NOT NULL CONSTRAINT FK_Customers_City REFERENCES Cities(CityId),
    Phone            VARCHAR(20)   NOT NULL,
    Email            VARCHAR(150)  NULL,
    CustomerType     VARCHAR(10)   NOT NULL CONSTRAINT DF_Customers_Type DEFAULT 'INDIVIDUAL',
    AddressProofPath NVARCHAR(255) NULL,
    CreatedByEmployeeId INT        NULL CONSTRAINT FK_Customers_Employee REFERENCES Employees(EmployeeId),
    CreatedAt        DATETIME2(0)  NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Customers_Type CHECK (CustomerType IN ('INDIVIDUAL','CORPORATE'))
);
CREATE INDEX IX_Customers_Phone ON Customers(Phone);
CREATE INDEX IX_Customers_Name ON Customers(FullName);

-- Tai khoan dang nhap. Dung chung cho nhan vien va khach, dung mot trong hai khoa ngoai.
-- MustChangePassword = 1: mat khau tam, bat doi o lan dang nhap dau (FR-AUTH-06).
CREATE TABLE UserAccounts (
    UserId       INT IDENTITY(1,1) CONSTRAINT PK_UserAccounts PRIMARY KEY,
    Username     VARCHAR(50)  NOT NULL,
    PasswordHash VARCHAR(100) NOT NULL,
    Role         VARCHAR(10)  NOT NULL,
    EmployeeId   INT          NULL CONSTRAINT FK_Users_Employee REFERENCES Employees(EmployeeId),
    CustomerId   INT          NULL CONSTRAINT FK_Users_Customer REFERENCES Customers(CustomerId),
    MustChangePassword BIT    NOT NULL CONSTRAINT DF_Users_MustChange DEFAULT 0,
    Status       VARCHAR(10)  NOT NULL CONSTRAINT DF_Users_Status DEFAULT 'ACTIVE',
    LastLoginAt  DATETIME2(0) NULL,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('ADMIN','ACCOUNTS','TECH','RETAIL','CUSTOMER')),
    CONSTRAINT CK_Users_Status CHECK (Status IN ('ACTIVE','INACTIVE')),
    CONSTRAINT CK_Users_Owner CHECK (
        (Role = 'CUSTOMER' AND CustomerId IS NOT NULL AND EmployeeId IS NULL)
        OR (Role <> 'CUSTOMER' AND EmployeeId IS NOT NULL AND CustomerId IS NULL))
);
-- Moi nhan vien/khach co toi da mot tai khoan (unique bo qua NULL nho filtered index).
CREATE UNIQUE INDEX UX_Users_Employee ON UserAccounts(EmployeeId) WHERE EmployeeId IS NOT NULL;
CREATE UNIQUE INDEX UX_Users_Customer ON UserAccounts(CustomerId) WHERE CustomerId IS NOT NULL;

/* ---------------------------------------------------------------------------
   2. Cau hinh: tham so he thong, bac uu dai, bo dem sinh ID
   ------------------------------------------------------------------------ */

-- Tham so doi duoc khong can sua code: thue suat, tien coc theo loai, so ngay qua han...
CREATE TABLE SystemConfig (
    ConfigKey   VARCHAR(50)   NOT NULL CONSTRAINT PK_SystemConfig PRIMARY KEY,
    ConfigValue VARCHAR(100)  NOT NULL,
    Description NVARCHAR(255) NULL
);

-- Bac uu dai so luong lon (BR-05). MaxQty NULL = khong gioi han tren.
CREATE TABLE BulkDiscounts (
    DiscountId      INT IDENTITY(1,1) CONSTRAINT PK_BulkDiscounts PRIMARY KEY,
    MinQty          INT          NOT NULL,
    MaxQty          INT          NULL,
    DiscountPercent DECIMAL(5,2) NOT NULL,
    CONSTRAINT UQ_BulkDiscounts_Min UNIQUE (MinQty),
    CONSTRAINT CK_BulkDiscounts_Range CHECK (MinQty >= 1 AND (MaxQty IS NULL OR MaxQty >= MinQty)),
    CONSTRAINT CK_BulkDiscounts_Pct CHECK (DiscountPercent BETWEEN 0 AND 100)
);

-- Bo dem sinh ID: ORDER_D, ORDER_T, ORDER_B (rieng tung loai) va ACCOUNT (chung).
-- IdGenerator doc bang SELECT ... WITH (UPDLOCK, ROWLOCK) roi UPDATE trong cung transaction.
CREATE TABLE Sequences (
    SeqName      VARCHAR(20) NOT NULL CONSTRAINT PK_Sequences PRIMARY KEY,
    CurrentValue BIGINT      NOT NULL CONSTRAINT DF_Sequences_Value DEFAULT 0
);

/* ---------------------------------------------------------------------------
   3. Goi cuoc
   ------------------------------------------------------------------------ */

-- HoursIncluded: gio cua goi theo gio, tinh tren CA chu ky hieu luc (30 gio / 3 thang).
-- OverageRatePerHour: gia vuot gio; NULL voi goi khong theo gio.
CREATE TABLE Plans (
    PlanId         INT IDENTITY(1,1) CONSTRAINT PK_Plans PRIMARY KEY,
    ConnType       CHAR(1)       NOT NULL,
    GroupName      NVARCHAR(50)  NOT NULL,
    PlanName       NVARCHAR(100) NOT NULL,
    Price          DECIMAL(12,2) NOT NULL,
    ValidityMonths INT           NOT NULL,
    HoursIncluded  INT           NULL,
    OverageRatePerHour DECIMAL(12,2) NULL,
    SpeedKbps      INT           NULL,
    Description    NVARCHAR(500) NULL,
    Status         VARCHAR(10)   NOT NULL CONSTRAINT DF_Plans_Status DEFAULT 'ACTIVE',
    CONSTRAINT UQ_Plans_Name UNIQUE (ConnType, GroupName, PlanName),
    CONSTRAINT CK_Plans_Type CHECK (ConnType IN ('D','B','T')),
    CONSTRAINT CK_Plans_Price CHECK (Price >= 0),
    CONSTRAINT CK_Plans_Validity CHECK (ValidityMonths > 0),
    CONSTRAINT CK_Plans_Hours CHECK (HoursIncluded IS NULL OR HoursIncluded > 0),
    CONSTRAINT CK_Plans_Overage CHECK (OverageRatePerHour IS NULL OR OverageRatePerHour >= 0),
    CONSTRAINT CK_Plans_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
);

-- Cuoc goi theo loai cua goi Landline. SMS = "Messaging For Mobiles".
CREATE TABLE PlanCallRates (
    PlanId     INT           NOT NULL CONSTRAINT FK_CallRates_Plan REFERENCES Plans(PlanId) ON DELETE CASCADE,
    CallType   VARCHAR(5)    NOT NULL,
    RatePerMin DECIMAL(12,2) NOT NULL,
    CONSTRAINT PK_PlanCallRates PRIMARY KEY (PlanId, CallType),
    CONSTRAINT CK_CallRates_Type CHECK (CallType IN ('LOCAL','STD','SMS')),
    CONSTRAINT CK_CallRates_Rate CHECK (RatePerMin >= 0)
);

/* ---------------------------------------------------------------------------
   4. Nha cung cap, thiet bi, nhap hang
   ------------------------------------------------------------------------ */

CREATE TABLE Vendors (
    VendorId    INT IDENTITY(1,1) CONSTRAINT PK_Vendors PRIMARY KEY,
    VendorName  NVARCHAR(150) NOT NULL,
    Contact     NVARCHAR(150) NULL,
    Phone       VARCHAR(20)   NULL,
    Email       VARCHAR(150)  NULL,
    Status      VARCHAR(10)   NOT NULL CONSTRAINT DF_Vendors_Status DEFAULT 'ACTIVE',
    CONSTRAINT CK_Vendors_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
);

CREATE TABLE Products (
    ProductId   INT IDENTITY(1,1) CONSTRAINT PK_Products PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    ProductType VARCHAR(20)   NOT NULL,
    VendorId    INT           NULL CONSTRAINT FK_Products_Vendor REFERENCES Vendors(VendorId),
    Price       DECIMAL(12,2) NOT NULL,
    StockQty    INT           NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0,
    Status      VARCHAR(10)   NOT NULL CONSTRAINT DF_Products_Status DEFAULT 'ACTIVE',
    CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    -- Chan ton kho am ngay o DB, phong khi Service kiem tra thieu.
    CONSTRAINT CK_Products_Stock CHECK (StockQty >= 0),
    CONSTRAINT CK_Products_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
);

-- Don mua hang tu nha cung cap (FR-ADM-04). ReceivedAt co gia tri = da cong ton kho.
CREATE TABLE Purchases (
    PurchaseId   INT IDENTITY(1,1) CONSTRAINT PK_Purchases PRIMARY KEY,
    VendorId     INT           NOT NULL CONSTRAINT FK_Purchases_Vendor REFERENCES Vendors(VendorId),
    PurchaseDate DATE          NOT NULL,
    TotalAmount  DECIMAL(12,2) NOT NULL CONSTRAINT DF_Purchases_Total DEFAULT 0,
    AmountPaid   DECIMAL(12,2) NOT NULL CONSTRAINT DF_Purchases_Paid DEFAULT 0,
    ReceivedAt   DATETIME2(0)  NULL,
    Status       VARCHAR(10)   NOT NULL CONSTRAINT DF_Purchases_Status DEFAULT 'ACTIVE',
    CONSTRAINT CK_Purchases_Amount CHECK (TotalAmount >= 0 AND AmountPaid >= 0),
    CONSTRAINT CK_Purchases_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
);

CREATE TABLE PurchaseItems (
    PurchaseId INT           NOT NULL CONSTRAINT FK_Items_Purchase REFERENCES Purchases(PurchaseId) ON DELETE CASCADE,
    ProductId  INT           NOT NULL CONSTRAINT FK_Items_Product REFERENCES Products(ProductId),
    Quantity   INT           NOT NULL,
    UnitPrice  DECIMAL(12,2) NOT NULL,
    CONSTRAINT PK_PurchaseItems PRIMARY KEY (PurchaseId, ProductId),
    CONSTRAINT CK_Items_Qty CHECK (Quantity > 0),
    CONSTRAINT CK_Items_Price CHECK (UnitPrice >= 0)
);

/* ---------------------------------------------------------------------------
   5. Don dang ky va yeu cau dat don online
   ------------------------------------------------------------------------ */

-- OrderId 11 ky tu: [D|T|B] + 10 chu so (BR-01).
-- PlanFee, DepositAmount, LandlinePlanFee, LandlineDepositAmount la gia TRUOC uu dai,
-- chup lai luc dat de Admin doi gia khong anh huong don cu. Hoa don ky dau nhan
-- (1 - DiscountPercent/100) tu cac gia tri nay.
-- Cac cot Landline* chi co gia tri khi don Dial-up dang ky moi ca landline.
CREATE TABLE Orders (
    OrderId        CHAR(11)      NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId     INT           NOT NULL CONSTRAINT FK_Orders_Customer REFERENCES Customers(CustomerId),
    ShopId         INT           NOT NULL CONSTRAINT FK_Orders_Shop REFERENCES RetailShops(ShopId),
    EmployeeId     INT           NOT NULL CONSTRAINT FK_Orders_Employee REFERENCES Employees(EmployeeId),
    ConnType       CHAR(1)       NOT NULL,
    PlanId         INT           NOT NULL CONSTRAINT FK_Orders_Plan REFERENCES Plans(PlanId),
    EquipmentProductId INT       NULL CONSTRAINT FK_Orders_Equipment REFERENCES Products(ProductId),
    Quantity       INT           NOT NULL,
    ConnectedQty   INT           NOT NULL CONSTRAINT DF_Orders_ConnectedQty DEFAULT 0,
    DiscountPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_Orders_Discount DEFAULT 0,
    PlanFee        DECIMAL(12,2) NOT NULL,
    DepositAmount  DECIMAL(12,2) NOT NULL,
    LandlinePlanId INT           NULL CONSTRAINT FK_Orders_LandlinePlan REFERENCES Plans(PlanId),
    LandlinePlanFee DECIMAL(12,2) NULL,
    LandlineDepositAmount DECIMAL(12,2) NULL,
    ExistingLandlineAccountId CHAR(16) NULL,   -- FK them sau khi tao Connections
    OrderDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_Orders_Date DEFAULT SYSDATETIME(),
    Status         VARCHAR(20)   NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 'SUBMITTED',
    DistanceKm     DECIMAL(8,2)  NULL,
    ServerAvailable BIT          NULL,
    LandlineFeasible BIT         NULL,
    InternetFeasible BIT         NULL,
    FeasibilityReason NVARCHAR(500) NULL,
    CheckedByEmployeeId INT      NULL CONSTRAINT FK_Orders_Checker REFERENCES Employees(EmployeeId),
    CONSTRAINT CK_Orders_Id CHECK (OrderId LIKE '[DTB][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_Orders_Type CHECK (ConnType IN ('D','B','T')),
    -- Ky tu dau cua OrderId phai trung voi loai ket noi.
    CONSTRAINT CK_Orders_IdMatchesType CHECK (LEFT(OrderId, 1) = ConnType),
    CONSTRAINT CK_Orders_Qty CHECK (Quantity >= 1 AND ConnectedQty BETWEEN 0 AND Quantity),
    CONSTRAINT CK_Orders_Discount CHECK (DiscountPercent BETWEEN 0 AND 100),
    CONSTRAINT CK_Orders_Status CHECK (Status IN ('SUBMITTED','FEASIBILITY_CHECK','FEASIBLE','NOT_FEASIBLE','CONNECTED','CANCELLED')),
    -- Dial-up bat buoc co landline: dang ky moi hoac da co (BR-04). Hai cach khong dong thoi.
    CONSTRAINT CK_Orders_DialupLandline CHECK (
        (ConnType = 'D' AND (
            (LandlinePlanId IS NOT NULL AND LandlinePlanFee IS NOT NULL AND LandlineDepositAmount IS NOT NULL AND ExistingLandlineAccountId IS NULL)
            OR (LandlinePlanId IS NULL AND ExistingLandlineAccountId IS NOT NULL AND Quantity = 1)))
        OR (ConnType <> 'D' AND LandlinePlanId IS NULL AND ExistingLandlineAccountId IS NULL))
);
CREATE INDEX IX_Orders_Customer ON Orders(CustomerId);
CREATE INDEX IX_Orders_Date ON Orders(OrderDate);
CREATE INDEX IX_Orders_Status ON Orders(Status, ConnType);

-- Lich su trang thai don (hien thi o /order/view).
CREATE TABLE OrderStatusHistory (
    HistoryId   BIGINT IDENTITY(1,1) CONSTRAINT PK_OrderStatusHistory PRIMARY KEY,
    OrderId     CHAR(11)     NOT NULL CONSTRAINT FK_History_Order REFERENCES Orders(OrderId),
    Status      VARCHAR(20)  NOT NULL,
    ActorUserId INT          NULL CONSTRAINT FK_History_User REFERENCES UserAccounts(UserId),
    ChangedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_History_At DEFAULT SYSDATETIME(),
    Note        NVARCHAR(500) NULL
);
CREATE INDEX IX_History_Order ON OrderStatusHistory(OrderId, ChangedAt);

-- Yeu cau dat ket noi online cua khach, cho nhan vien cua hang xac nhan (FR-CUS-03).
CREATE TABLE OrderRequests (
    RequestId   INT IDENTITY(1,1) CONSTRAINT PK_OrderRequests PRIMARY KEY,
    CustomerId  INT          NOT NULL CONSTRAINT FK_Requests_Customer REFERENCES Customers(CustomerId),
    ShopId      INT          NOT NULL CONSTRAINT FK_Requests_Shop REFERENCES RetailShops(ShopId),
    ConnType    CHAR(1)      NOT NULL,
    PlanId      INT          NOT NULL CONSTRAINT FK_Requests_Plan REFERENCES Plans(PlanId),
    Note        NVARCHAR(500) NULL,
    Status      VARCHAR(10)  NOT NULL CONSTRAINT DF_Requests_Status DEFAULT 'PENDING',
    RejectReason NVARCHAR(500) NULL,
    OrderId     CHAR(11)     NULL CONSTRAINT FK_Requests_Order REFERENCES Orders(OrderId),
    HandledByEmployeeId INT  NULL CONSTRAINT FK_Requests_Employee REFERENCES Employees(EmployeeId),
    CreatedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_Requests_At DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Requests_Type CHECK (ConnType IN ('D','B','T')),
    CONSTRAINT CK_Requests_Status CHECK (Status IN ('PENDING','CONFIRMED','REJECTED'))
);
CREATE INDEX IX_Requests_Shop ON OrderRequests(ShopId, Status);

/* ---------------------------------------------------------------------------
   6. Ket noi va thiet bi da cap
   ------------------------------------------------------------------------ */

-- AccountId 16 ky tu: [D|B|T] + ma thanh pho 3 so + so thu tu 12 so (BR-02).
-- LandlineAccountId: Dial-up gan voi landline Nexus (BR-04).
-- SuspendRequested*: ke toan de nghi, Ky thuat moi doi trang thai (FR-ACC-04).
-- DepositRefund*: hoan coc mot lan (FR-ACC-06).
CREATE TABLE Connections (
    AccountId   CHAR(16)     NOT NULL CONSTRAINT PK_Connections PRIMARY KEY,
    OrderId     CHAR(11)     NOT NULL CONSTRAINT FK_Connections_Order REFERENCES Orders(OrderId),
    CustomerId  INT          NOT NULL CONSTRAINT FK_Connections_Customer REFERENCES Customers(CustomerId),
    ConnType    CHAR(1)      NOT NULL,
    PlanId      INT          NOT NULL CONSTRAINT FK_Connections_Plan REFERENCES Plans(PlanId),
    CityId      INT          NOT NULL CONSTRAINT FK_Connections_City REFERENCES Cities(CityId),
    LandlineAccountId CHAR(16) NULL CONSTRAINT FK_Connections_Landline REFERENCES Connections(AccountId),
    ActivationDate DATE      NOT NULL,
    Status      VARCHAR(15)  NOT NULL CONSTRAINT DF_Connections_Status DEFAULT 'ACTIVE',
    StatusReason NVARCHAR(500) NULL,
    SuspendRequested BIT     NOT NULL CONSTRAINT DF_Connections_SuspendReq DEFAULT 0,
    SuspendRequestReason NVARCHAR(500) NULL,
    SuspendRequestedAt DATETIME2(0) NULL,
    DepositRefundAmount DECIMAL(12,2) NULL,
    DepositRefundedAt DATETIME2(0) NULL,
    CreatedByEmployeeId INT  NULL CONSTRAINT FK_Connections_Employee REFERENCES Employees(EmployeeId),
    CONSTRAINT CK_Connections_Id CHECK (AccountId LIKE '[DBT][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_Connections_Type CHECK (ConnType IN ('D','B','T')),
    CONSTRAINT CK_Connections_IdMatchesType CHECK (LEFT(AccountId, 1) = ConnType),
    CONSTRAINT CK_Connections_Status CHECK (Status IN ('ACTIVE','TEMP_INACTIVE','PERM_INACTIVE')),
    -- Chi Dial-up moi gan landline, va Dial-up luon phai co landline.
    CONSTRAINT CK_Connections_Landline CHECK (
        (ConnType = 'D' AND LandlineAccountId IS NOT NULL) OR (ConnType <> 'D' AND LandlineAccountId IS NULL))
);
CREATE INDEX IX_Connections_Order ON Connections(OrderId);
CREATE INDEX IX_Connections_Customer ON Connections(CustomerId);
CREATE INDEX IX_Connections_Landline ON Connections(LandlineAccountId) WHERE LandlineAccountId IS NOT NULL;

ALTER TABLE Orders ADD CONSTRAINT FK_Orders_ExistingLandline
    FOREIGN KEY (ExistingLandlineAccountId) REFERENCES Connections(AccountId);

-- Thiet bi da cap cho ket noi. Mot serial chi dang ISSUED o toi da mot noi.
CREATE TABLE ConnectionEquipment (
    EquipmentId INT IDENTITY(1,1) CONSTRAINT PK_ConnectionEquipment PRIMARY KEY,
    AccountId   CHAR(16)     NOT NULL CONSTRAINT FK_Equipment_Connection REFERENCES Connections(AccountId),
    ProductId   INT          NOT NULL CONSTRAINT FK_Equipment_Product REFERENCES Products(ProductId),
    SerialNo    VARCHAR(50)  NOT NULL,
    IssuedDate  DATE         NOT NULL,
    Status      VARCHAR(10)  NOT NULL CONSTRAINT DF_Equipment_Status DEFAULT 'ISSUED',
    CONSTRAINT CK_Equipment_Status CHECK (Status IN ('ISSUED','REPLACED','RETURNED'))
);
CREATE UNIQUE INDEX UX_Equipment_Serial ON ConnectionEquipment(SerialNo) WHERE Status = 'ISSUED';
CREATE INDEX IX_Equipment_Account ON ConnectionEquipment(AccountId);

/* ---------------------------------------------------------------------------
   7. Hoa don, cuoc phat sinh, thanh toan
   ------------------------------------------------------------------------ */

-- Hoa don theo ky thang cua tung ket noi (BR-07). Tach tung khoan de doi nhanh
-- cong thuc khi mentor tra loi. PreviousDue chi hien thi, khong nam trong TotalAmount.
-- UsageHours luu lai de tinh vuot gio luy ke trong chu ky hieu luc.
CREATE TABLE Bills (
    BillId        INT IDENTITY(1,1) CONSTRAINT PK_Bills PRIMARY KEY,
    AccountId     CHAR(16)      NOT NULL CONSTRAINT FK_Bills_Connection REFERENCES Connections(AccountId),
    PeriodStart   DATE          NOT NULL,
    PeriodEnd     DATE          NOT NULL,
    PlanFee       DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_PlanFee DEFAULT 0,
    CallCharges   DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_Call DEFAULT 0,
    UsageHours    DECIMAL(8,2)  NOT NULL CONSTRAINT DF_Bills_Hours DEFAULT 0,
    OverageCharges DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_Overage DEFAULT 0,
    ReplacementCharges DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_Replacement DEFAULT 0,
    Adjustment    DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_Adjustment DEFAULT 0,
    TaxableBase   DECIMAL(12,2) NOT NULL,
    TaxRate       DECIMAL(6,4)  NOT NULL,
    TaxAmount     DECIMAL(12,2) NOT NULL,
    DepositAmount DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_Deposit DEFAULT 0,
    TotalAmount   DECIMAL(12,2) NOT NULL,
    PaidAmount    DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_Paid DEFAULT 0,
    DueAmount     DECIMAL(12,2) NOT NULL,
    PreviousDue   DECIMAL(12,2) NOT NULL CONSTRAINT DF_Bills_PrevDue DEFAULT 0,
    BillDate      DATE          NOT NULL,
    DueDate       DATE          NOT NULL,
    Status        VARCHAR(10)   NOT NULL CONSTRAINT DF_Bills_Status DEFAULT 'UNPAID',
    Note          NVARCHAR(500) NULL,
    CreatedByEmployeeId INT     NULL CONSTRAINT FK_Bills_Employee REFERENCES Employees(EmployeeId),
    -- Chan trung tuyet doi o DB; chong lan ky kiem tra them o BillingService.
    CONSTRAINT UQ_Bills_Period UNIQUE (AccountId, PeriodStart, PeriodEnd),
    CONSTRAINT CK_Bills_Period CHECK (PeriodEnd >= PeriodStart),
    CONSTRAINT CK_Bills_Status CHECK (Status IN ('UNPAID','PARTIAL','PAID')),
    CONSTRAINT CK_Bills_Paid CHECK (PaidAmount >= 0),
    -- So con no luon khop voi tong tru da tra, va khong am (khong tra vuot, 6.3).
    CONSTRAINT CK_Bills_Due CHECK (DueAmount >= 0 AND DueAmount = TotalAmount - PaidAmount)
);
CREATE INDEX IX_Bills_Due ON Bills(Status, DueDate);

-- Cuoc goi theo loai cua hoa don (de in chi tiet va sua hoa don chua thanh toan).
CREATE TABLE BillCallUsage (
    BillId   INT           NOT NULL CONSTRAINT FK_Usage_Bill REFERENCES Bills(BillId) ON DELETE CASCADE,
    CallType VARCHAR(5)    NOT NULL,
    Minutes  DECIMAL(10,2) NOT NULL,
    RatePerMin DECIMAL(12,2) NOT NULL,
    Amount   DECIMAL(12,2) NOT NULL,
    CONSTRAINT PK_BillCallUsage PRIMARY KEY (BillId, CallType),
    CONSTRAINT CK_Usage_Type CHECK (CallType IN ('LOCAL','STD','SMS')),
    CONSTRAINT CK_Usage_Minutes CHECK (Minutes >= 0)
);

-- Phi thay thiet bi hong. UNBILLED -> BILLED khi vao hoa don; DEPOSIT_OFFSET khi tru vao coc.
CREATE TABLE ReplacementCharges (
    ChargeId   INT IDENTITY(1,1) CONSTRAINT PK_ReplacementCharges PRIMARY KEY,
    AccountId  CHAR(16)      NOT NULL CONSTRAINT FK_Charges_Connection REFERENCES Connections(AccountId),
    ProductId  INT           NOT NULL CONSTRAINT FK_Charges_Product REFERENCES Products(ProductId),
    Amount     DECIMAL(12,2) NOT NULL,
    ChargeDate DATE          NOT NULL,
    Status     VARCHAR(15)   NOT NULL CONSTRAINT DF_Charges_Status DEFAULT 'UNBILLED',
    BillId     INT           NULL CONSTRAINT FK_Charges_Bill REFERENCES Bills(BillId),
    Note       NVARCHAR(255) NULL,
    CONSTRAINT CK_Charges_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_Charges_Status CHECK (Status IN ('UNBILLED','BILLED','DEPOSIT_OFFSET'))
);
CREATE INDEX IX_Charges_Account ON ReplacementCharges(AccountId, Status);

-- Thanh toan theo hoa don. Method DEPOSIT_OFFSET: bu tru no tu tien coc khi hoan coc.
CREATE TABLE Payments (
    PaymentId   INT IDENTITY(1,1) CONSTRAINT PK_Payments PRIMARY KEY,
    BillId      INT           NOT NULL CONSTRAINT FK_Payments_Bill REFERENCES Bills(BillId),
    Amount      DECIMAL(12,2) NOT NULL,
    Method      VARCHAR(15)   NOT NULL,
    PaidDate    DATE          NOT NULL,
    Note        NVARCHAR(255) NULL,
    ReceivedByEmployeeId INT  NOT NULL CONSTRAINT FK_Payments_Employee REFERENCES Employees(EmployeeId),
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Payments_At DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Method CHECK (Method IN ('CASH','CARD','TRANSFER','DEPOSIT_OFFSET'))
);
CREATE INDEX IX_Payments_Bill ON Payments(BillId);

/* ---------------------------------------------------------------------------
   8. Phan hoi va kiem toan
   ------------------------------------------------------------------------ */

-- Phan hoi gan voi dung mot don hoac mot ket noi.
CREATE TABLE Feedback (
    FeedbackId  INT IDENTITY(1,1) CONSTRAINT PK_Feedback PRIMARY KEY,
    CustomerId  INT           NOT NULL CONSTRAINT FK_Feedback_Customer REFERENCES Customers(CustomerId),
    OrderId     CHAR(11)      NULL CONSTRAINT FK_Feedback_Order REFERENCES Orders(OrderId),
    AccountId   CHAR(16)      NULL CONSTRAINT FK_Feedback_Connection REFERENCES Connections(AccountId),
    Rating      TINYINT       NULL,
    Comment     NVARCHAR(1000) NOT NULL,
    RecordedByEmployeeId INT  NULL CONSTRAINT FK_Feedback_Employee REFERENCES Employees(EmployeeId),
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Feedback_At DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Feedback_Rating CHECK (Rating IS NULL OR Rating BETWEEN 1 AND 5),
    CONSTRAINT CK_Feedback_Ref CHECK ((OrderId IS NULL AND AccountId IS NOT NULL) OR (OrderId IS NOT NULL AND AccountId IS NULL))
);

-- Nhat ky thao tac quan trong (NFR-08).
CREATE TABLE AuditLog (
    AuditId     BIGINT IDENTITY(1,1) CONSTRAINT PK_AuditLog PRIMARY KEY,
    ActorUserId INT           NULL CONSTRAINT FK_Audit_User REFERENCES UserAccounts(UserId),
    Action      VARCHAR(50)   NOT NULL,
    EntityType  VARCHAR(30)   NOT NULL,
    EntityId    VARCHAR(30)   NOT NULL,
    Detail      NVARCHAR(500) NULL,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Audit_At DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Audit_Entity ON AuditLog(EntityType, EntityId);
GO
PRINT 'NEXUS schema created.';
GO
