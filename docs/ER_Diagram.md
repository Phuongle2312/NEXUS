# E-R Diagram: NEXUS Service Marketing System (Task 1)

Đồng bộ với `database/schema.sql` (25 bảng). Sơ đồ tách theo nhóm để dễ đọc; GitHub và VS Code (có extension Mermaid) hiển thị trực tiếp. Khi đưa vào project report, xuất từng sơ đồ thành ảnh.

## 1. Danh mục, tài khoản

```mermaid
erDiagram
    Cities ||--o{ RetailShops : "có"
    Cities ||--o{ Customers : "địa chỉ"
    RetailShops ||--o{ Employees : "RETAIL thuộc"
    Employees |o--o| UserAccounts : "đăng nhập"
    Customers |o--o| UserAccounts : "đăng nhập"
    UserAccounts ||--o{ AuditLog : "thực hiện"

    Cities { int CityId PK
             nvarchar CityName
             char CityCode UK "3 chữ số" }
    RetailShops { int ShopId PK
                  nvarchar ShopName
                  int CityId FK }
    Employees { int EmployeeId PK
                nvarchar FullName
                varchar Role "ADMIN ACCOUNTS TECH RETAIL"
                int ShopId FK "bắt buộc nếu RETAIL"
                varchar Status }
    Customers { int CustomerId PK
                nvarchar FullName
                int CityId FK
                varchar Phone
                varchar CustomerType }
    UserAccounts { int UserId PK
                   varchar Username UK
                   varchar PasswordHash "BCrypt"
                   varchar Role
                   int EmployeeId FK "hoặc CustomerId"
                   int CustomerId FK
                   bit MustChangePassword }
    AuditLog { bigint AuditId PK
               int ActorUserId FK
               varchar Action }
```

## 2. Đơn, kết nối, thiết bị

```mermaid
erDiagram
    Customers ||--o{ Orders : "đặt"
    RetailShops ||--o{ Orders : "tiếp nhận"
    Employees ||--o{ Orders : "tạo"
    Plans ||--o{ Orders : "gói"
    Plans ||--o{ PlanCallRates : "cước gọi"
    Orders ||--o{ OrderStatusHistory : "lịch sử"
    Customers ||--o{ OrderRequests : "gửi online"
    OrderRequests |o--o| Orders : "xác nhận thành"
    Orders ||--o{ Connections : "cấp"
    Connections |o--o{ Connections : "Dial-up gắn Landline"
    Connections ||--o{ ConnectionEquipment : "thiết bị"
    Products ||--o{ ConnectionEquipment : "loại"
    Vendors ||--o{ Products : "cung cấp"
    Vendors ||--o{ Purchases : "bán"
    Purchases ||--|{ PurchaseItems : "gồm"
    Products ||--o{ PurchaseItems : "hàng"

    Orders { char11 OrderId PK "D T B + 10 số"
             int Quantity
             int ConnectedQty
             decimal DiscountPercent
             decimal PlanFee "giá trước ưu đãi"
             decimal DepositAmount "giá trước ưu đãi"
             int LandlinePlanId FK "Dial-up kèm landline mới"
             decimal LandlinePlanFee
             decimal LandlineDepositAmount
             char16 ExistingLandlineAccountId FK
             varchar Status }
    Connections { char16 AccountId PK "loại + thành phố + 12 số"
                  char11 OrderId FK
                  int PlanId FK
                  char16 LandlineAccountId FK
                  date ActivationDate
                  varchar Status
                  bit SuspendRequested
                  decimal DepositRefundAmount }
    Plans { int PlanId PK
            char ConnType
            decimal Price
            int ValidityMonths
            int HoursIncluded "tính trên cả chu kỳ"
            decimal OverageRatePerHour }
```

## 3. Hóa đơn, thanh toán

```mermaid
erDiagram
    Connections ||--o{ Bills : "theo kỳ"
    Bills ||--o{ BillCallUsage : "dòng cước gọi"
    Bills ||--o{ Payments : "thanh toán"
    Connections ||--o{ ReplacementCharges : "phí thay thiết bị"
    Bills |o--o{ ReplacementCharges : "đã lập"
    Employees ||--o{ Payments : "nhận"
    Customers ||--o{ Feedback : "gửi"
    Orders |o--o{ Feedback : "về đơn"
    Connections |o--o{ Feedback : "về kết nối"

    Bills { int BillId PK
            char16 AccountId FK
            date PeriodStart "unique cùng AccountId, PeriodEnd"
            date PeriodEnd
            decimal PlanFee
            decimal CallCharges
            decimal UsageHours
            decimal OverageCharges
            decimal ReplacementCharges
            decimal TaxableBase
            decimal TaxRate
            decimal TaxAmount
            decimal DepositAmount
            decimal TotalAmount
            decimal PaidAmount
            decimal DueAmount "= Total - Paid, CHECK"
            decimal PreviousDue "chỉ hiển thị"
            varchar Status }
    Payments { int PaymentId PK
               int BillId FK
               decimal Amount
               varchar Method "CASH CARD TRANSFER DEPOSIT_OFFSET" }
    ReplacementCharges { int ChargeId PK
                         char16 AccountId FK
                         decimal Amount
                         varchar Status "UNBILLED BILLED DEPOSIT_OFFSET"
                         int BillId FK }
```

Bảng cấu hình không có quan hệ: `SystemConfig` (thuế, tiền cọc, số ngày), `BulkDiscounts` (bậc ưu đãi), `Sequences` (bộ đếm sinh ID).
