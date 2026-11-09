# THIẾT KẾ API: NEXUS SERVICE MARKETING SYSTEM (.NET 10)

| Mục | Nội dung |
|---|---|
| Backend | **ASP.NET Core Web API**, **.NET 10** (`net10.0`), Controller-based |
| Truy cập dữ liệu | **EF Core 10** + SQL Server provider, map lên schema có sẵn |
| Xác thực | **JWT Bearer** (thay `HttpSession` của bản Java), mật khẩu băm **BCrypt** |
| Frontend | **React 19 + Vite 6**, gọi API qua `fetch`, token lưu `localStorage` |
| Kiến trúc | **Monolith** một tiến trình API. Không API gateway, không microservice |
| Địa chỉ dev | `http://localhost:5080/api` (Vite proxy `/api` → backend) |
| Tài liệu liên quan | `SRS_NEXUS_Service_Marketing_System.docx` (mã FR-xxx, BR-xx), `backend/README.md` |
| Phiên bản | 2.0 (11/2026), chuyển từ Java/JSP sang .NET 10. Giả định chờ mentor xác nhận: mục 8 |

> Bản Java/JSP/Servlet/JDBC vẫn được giữ trong repo (`src/main/java`, `pom.xml`) để tham chiếu,
> nhưng **bản chạy chính thức là .NET 10** trong `backend/`.

---

## 1. Quy ước chung

### 1.1 Cấu trúc URL

```
/api/<module>/<action>
```

Ví dụ: `/api/orders`, `/api/billing/generate`, `/api/public/plans`.

### 1.2 Phương thức

| Method | Dùng khi |
|---|---|
| `GET` | Đọc dữ liệu, không thay đổi trạng thái |
| `POST` | Tạo mới, chuyển trạng thái, thao tác nghiệp vụ |
| `PUT` | Cập nhật toàn bộ một bản ghi đã có |

Không dùng `DELETE`: dữ liệu nghiệp vụ ngừng dùng bằng `POST /{id}/deactivate` (xoá mềm qua cột `Status`),
giữ nguyên lịch sử đơn, kết nối, hóa đơn.

### 1.3 Xác thực và phân quyền

- `POST /api/auth/login` trả **JWT** (`accessToken`, `expiresAt`, `mustChangePassword`, `user`).
- Token mang claims: `sub` (userId), `role`, `employeeId`, `customerId`.
- Client gửi `Authorization: Bearer <token>` cho mọi endpoint không công khai.
- `[Authorize]` trên `ApiControllerBase` chặn toàn bộ controller; endpoint công khai đánh dấu `[AllowAnonymous]`.
- Phân quyền theo vai trò kiểm tra trong controller bằng `RequireRoles(...)`, sai quyền trả `403` với `FORBIDDEN`.
- JWT stateless: đăng xuất chỉ là xoá token ở client; đổi mật khẩu không thu hồi token đã phát hành.
- Khoá ký `Jwt:Key` tối thiểu 32 ký tự, đọc từ user-secrets/biến môi trường, **không commit**.

### 1.4 Định dạng JSON

Thành công:

```json
{ "success": true, "data": { }, "message": "OK" }
```

Lỗi:

```json
{ "success": false, "error": { "code": "ORDER_NOT_FOUND", "message": "Không tìm thấy đơn D0000000001." } }
```

Lỗi được sinh từ `BusinessException` + `ExceptionHandlingMiddleware`, nên mọi endpoint có cùng một hình dạng lỗi.

### 1.5 Mã trạng thái HTTP

| Mã | Dùng khi |
|---|---|
| 200 | Thành công |
| 400 | Dữ liệu không hợp lệ (`VALIDATION_ERROR`) |
| 401 | Thiếu/sai/hết hạn token (`AUTH_INVALID`) |
| 403 | Không đủ quyền (`FORBIDDEN`) |
| 404 | Không tìm thấy (`NOT_FOUND`, `ORDER_NOT_FOUND`, `ACCOUNT_NOT_FOUND`) |
| 409 | Xung đột nghiệp vụ: trùng mã, sai trạng thái, vượt tồn kho, đã hoàn cọc |
| 500 | Lỗi hệ thống |

### 1.6 Định dạng dữ liệu

- Ngày: `yyyy-MM-dd`. Giờ: ISO-8601.
- Tiền: số thập phân, đơn vị USD, hiển thị 2 chữ số.
- Encoding UTF-8. Tên trường JSON dùng camelCase (mặc định của ASP.NET Core).
- Phân trang: `page` (từ 1), `size` (mặc định 20, tối đa 100); kết quả là
  `{ items, page, size, total, totalPages }`.

### 1.7 Bảng mã lỗi nghiệp vụ

Enum `ErrorCode` trong `backend/src/Nexus.Api/Common/ErrorCode.cs`.

| Mã | Ý nghĩa |
|---|---|
| `AUTH_INVALID` | Sai tên đăng nhập hoặc mật khẩu; thiếu thông tin đăng nhập |
| `FORBIDDEN` | Không đủ quyền |
| `VALIDATION_ERROR` | Thiếu hoặc sai dữ liệu |
| `NOT_FOUND` | Không tìm thấy dữ liệu (hóa đơn, biên lai, danh mục) |
| `DUPLICATE` | Dữ liệu trùng khóa |
| `IN_USE` | Đang được tham chiếu nên không xoá/đổi mã; dùng deactivate |
| `BILL_STATE_INVALID` | Hóa đơn đã có thanh toán hoặc không phải mới nhất nên không sửa được |
| `ORDER_NOT_FOUND` | Không có Order ID |
| `ACCOUNT_NOT_FOUND` | Không có Account ID |
| `ORDER_STATE_INVALID` | Đơn không ở trạng thái cho phép thao tác |
| `NOT_FEASIBLE` | Đơn chưa khả thi, không thể cấp kết nối |
| `LANDLINE_REQUIRED` | Dial-up cần landline Nexus |
| `CONNECTION_STATE_INVALID` | Chuyển trạng thái kết nối không hợp lệ |
| `OUT_OF_STOCK` | Thiết bị hết hàng |
| `PAYMENT_EXCEEDS_DUE` | Số tiền thanh toán vượt số còn nợ |
| `BILL_EXISTS` | Đã có hóa đơn trùng/chồng lấn kỳ cước |
| `LANDLINE_IN_USE` | Landline đang phục vụ Dial-up còn hoạt động |
| `REQUEST_STATE_INVALID` | Yêu cầu đặt đơn đã được xử lý |
| `DEPOSIT_ALREADY_REFUNDED` | Tiền đặt cọc đã được hoàn |
| `SYSTEM_ERROR` | Lỗi ngoài dự kiến (CSDL không kết nối được, exception chưa xử lý) |

---

## 2. Quy tắc sinh ID

Sinh ID **trong transaction**, khoá dòng bộ đếm bằng `UPDLOCK, ROWLOCK` trên bảng `Sequences`
(`FromSqlInterpolated` trong `IdGenerator`) để hai nhân viên tạo cùng lúc không trùng.

### Order ID (11 ký tự)

```
orderId = typeChar + LPAD(nextSeq(typeChar), 10, '0')     // D0000000001
```

Bộ đếm riêng cho `D`, `T`, `B`.

### Account ID (16 ký tự)

```
accountId = typeChar + cityCode(3 chữ số) + LPAD(nextSeq('ACC'), 12, '0')   // B001000000000001
```

---

## 3. Bảng phân quyền endpoint

| Module | ADMIN | RETAIL | TECH | ACCOUNTS | CUSTOMER |
|---|:-:|:-:|:-:|:-:|:-:|
| `/api/auth/login`, `/api/public/*` | ✔ | ✔ | ✔ | ✔ | ✔ (không cần đăng nhập) |
| `/api/auth/change-password`, `/api/auth/logout` | ✔ | ✔ | ✔ | ✔ | ✔ |
| `/api/admin/*` | ✔ | – | – | – | – |
| `/api/customers/*` | ✔ | CRU | R | R | – |
| `/api/orders` (tạo) | – | ✔ | – | – | qua `/api/my/order-requests` |
| `/api/orders` (xem) | ✔ | ✔ | ✔ | ✔ | của mình |
| `/api/order-requests` | ✔ | ✔ | – | – | – |
| `/api/tech/*` | – | – | ✔ | – | – |
| `/api/tech/connections` (xem) | ✔ | ✔ | ✔ | ✔ | qua `/api/my/connections` |
| `/api/billing` (lập/sửa) | xem | – | – | ✔ | – |
| `/api/billing` (xem) | ✔ | ✔ | – | ✔ | của mình |
| `/api/payments` (ghi nhận) | – | ✔ | – | ✔ | – |
| `/api/payments/{id}/receipt` | ✔ | ✔ | – | ✔ | qua `/api/my/bills` |
| `/api/my/*` | – | – | – | – | ✔ |

> Khách tự đặt đơn vẫn là điểm chưa rõ của đề (mục 8). Thiết kế hiện tại: khách gửi **yêu cầu**
> `POST /api/my/order-requests`, nhân viên cửa hàng xác nhận thành đơn chính thức.

---

## 4. Danh sách endpoint

### 4.1 Xác thực — `/api/auth`

| Method | URL | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/auth/login` | công khai | Đăng nhập, trả JWT + hồ sơ người dùng |
| POST | `/api/auth/change-password` | đã đăng nhập | Đổi mật khẩu của chính mình |
| POST | `/api/auth/logout` | đã đăng nhập | Xoá token phía client (API trả OK) |

```jsonc
// POST /api/auth/login
{ "username": "admin", "password": "Nexus@2026" }

// data trả về
{
  "accessToken": "eyJ...",
  "expiresAt": "2026-11-09T20:00:00Z",
  "mustChangePassword": false,
  "user": { "userId": 1, "username": "admin", "role": "ADMIN", "employeeId": 1, "customerId": null }
}
```

### 4.2 Công khai — `/api/public`

| Method | URL | Mô tả | Tham số |
|---|---|---|---|
| GET | `/api/public/plans` | Gói cước đang áp dụng, kèm tiền cọc và cước gọi | `type` (D/B/T, tuỳ chọn) |
| GET | `/api/public/cities` | Thành phố đang hoạt động | – |
| GET | `/api/public/shops` | Cửa hàng đang hoạt động | `cityId` (tuỳ chọn) |
| GET | `/api/public/discount` | Mức ưu đãi theo số lượng, kèm toàn bộ bậc | `qty` (≥ 1) |
| GET | `/api/public/track-order` | Trạng thái đơn | `orderId` |
| GET | `/api/public/track-account` | Trạng thái kết nối + tổng nợ | `accountId` |

`PlanDto` trả về: `planId, connType, groupName, planName, price, validityMonths, hoursIncluded,
overageRatePerHour, speedKbps, description, deposit, callRates[]`.

`deposit` đọc từ `SystemConfig` (`DEPOSIT_D`, `DEPOSIT_B`, `DEPOSIT_T`), không hard-code ở client.

**Quy tắc bảo mật tra cứu:** vì ID cấp tuần tự nên dễ dò; tra cứu công khai **chỉ trả trạng thái,
tên gói, thành phố và tổng nợ**, không trả tên khách, địa chỉ, thiết bị hay chi tiết hóa đơn.
Muốn xem chi tiết phải đăng nhập qua `/api/my/*`.

```jsonc
// GET /api/public/track-order?orderId=B0000000001
{
  "orderId": "B0000000001", "connType": "B", "planName": "Tháng",
  "status": "CONNECTED", "quantity": 12, "connectedQty": 12,
  "orderDate": "2026-11-02", "discountPercent": 25
}

// GET /api/public/track-account?accountId=B001000000000001
{
  "accountId": "B001000000000001", "connType": "B", "planName": "Tháng",
  "status": "ACTIVE", "activationDate": "2026-11-03",
  "cityName": "Hà Nội", "customerName": null, "dueAmount": 0.00
}
```

### 4.3 Quản trị danh mục — `/api/admin` (R: ADMIN)

Mỗi thực thể dùng cùng một bộ endpoint:

| Method | URL | Mô tả |
|---|---|---|
| GET | `/api/admin/{res}` | Danh sách |
| POST | `/api/admin/{res}` | Thêm mới |
| PUT | `/api/admin/{res}/{id}` | Cập nhật |
| POST | `/api/admin/{res}/{id}/deactivate` | Ngưng sử dụng (`Status = INACTIVE`), giữ lịch sử |

`{res}` ∈ `cities`, `shops`, `employees`, `plans`, `vendors`, `products`.

Body theo thực thể:

```
cities     { cityName, cityCode }                                            FR-ADM-07
shops      { shopName, address, cityId, phone }                              FR-ADM-02
employees  { fullName, role, phone, email, shopId, username, password }      FR-ADM-01
plans      { connType, groupName, planName, price, validityMonths,
             hoursIncluded, overageRatePerHour, speedKbps, description,
             callRates: [ { callType, ratePerMin } ] }                       FR-ADM-06
vendors    { vendorName, contact, phone, email }                             FR-ADM-03
products   { productName, productType, vendorId, price, stockQty }           FR-ADM-05
```

Endpoint đặc biệt:

| Method | URL | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/admin/products/{id}/stock` | ADMIN, TECH | Điều chỉnh tồn kho: `{ delta, note }` |
| GET | `/api/admin/feedback` | ADMIN, RETAIL, TECH, ACCOUNTS | Danh sách phản hồi |

Danh mục `plans` cho phép sửa giá; đơn và kết nối đã phát sinh giữ **giá chụp tại thời điểm đặt**
(`Orders.PlanFee`, `Orders.DepositAmount`) nên đổi giá không làm sai hóa đơn cũ.

### 4.4 Khách hàng — `/api/customers`

| Method | URL | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/customers` | RETAIL, ADMIN, TECH, ACCOUNTS | Tìm theo tên/SĐT: `q`, `page`, `size` |
| GET | `/api/customers/{id}` | đã đăng nhập | Chi tiết hồ sơ |
| POST | `/api/customers` | RETAIL, ADMIN | Thêm khách |
| PUT | `/api/customers/{id}` | RETAIL, ADMIN | Cập nhật hồ sơ |
| POST | `/api/customers/{id}/issue-account` | RETAIL, ADMIN | Cấp tài khoản portal, mật khẩu tạm trả về **một lần** |
| POST | `/api/customers/{id}/reset-password` | RETAIL, ADMIN | Cấp lại mật khẩu tạm |

```
POST/PUT body: { fullName, address, cityId, phone, email, customerType }   // INDIVIDUAL | CORPORATE
issue-account body: { username }  // bỏ trống thì hệ thống tự sinh
```

Khách được cấp tài khoản có `MustChangePassword = true`, phải đổi ở lần đăng nhập đầu.

### 4.5 Đơn đăng ký — `/api/orders`

| Method | URL | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/orders` | RETAIL | Tạo đơn, sinh Order ID |
| GET | `/api/orders` | RETAIL, TECH, ADMIN, ACCOUNTS | Danh sách: `status`, `type`, `from`, `to`, `page`, `size` |
| GET | `/api/orders/{orderId}` | đã đăng nhập (khách chỉ đơn của mình) | Chi tiết + lịch sử trạng thái + kết nối |

**`POST /api/orders`**

```jsonc
{
  "customerId": 1,
  "connType": "D",                        // D | B | T
  "planId": 4,                            // phải khớp connType
  "equipmentProductId": 1,                // modem/router, tuỳ chọn
  "quantity": 12,                         // >= 1, cùng loại và cùng gói
  "existingLandlineAccountId": null,      // Dial-up: landline Nexus đang có (chỉ khi quantity = 1)
  "landlinePlanId": 15,                   // Dial-up: gói landline khi đăng ký mới cả hai
  "requestId": null                       // chuyển từ yêu cầu online
}
```

Xử lý (một transaction):
1. Kiểm tra dữ liệu. Với Dial-up: phải có `landlinePlanId`, hoặc `existingLandlineAccountId` là kết nối
   loại T đang `ACTIVE` **của đúng khách này** (server tự kiểm tra, không tin client). Nếu không, trả
   `LANDLINE_REQUIRED`.
2. Sinh Order ID (mục 2).
3. Tính ưu đãi theo `quantity` (BR-05). Lưu `DiscountPercent`, giá gói và tiền cọc tại thời điểm đặt,
   `ConnectedQty = 0`.
4. Lưu đơn `SUBMITTED`, kèm `EmployeeId`, `ShopId`, thời gian.
5. Trả về `orderId`, `connType`, `quantity`, `discountPercent`, `planFee`, `depositAmount`, `status`.

Trạng thái đơn: `SUBMITTED → FEASIBILITY_CHECK → FEASIBLE → CONNECTED`
(hoặc `NOT_FEASIBLE`, `CANCELLED`). Đơn chỉ thành `CONNECTED` khi `connectedQty = quantity`.

### 4.6 Kỹ thuật — `/api/tech` (R: TECH)

| Method | URL | Mô tả |
|---|---|---|
| POST | `/api/tech/start-check` | Nhận đơn: `{ orderId }`, `SUBMITTED → FEASIBILITY_CHECK` |
| POST | `/api/tech/feasibility` | Ghi kết quả khảo sát |
| POST | `/api/tech/connections` | Cấp **một** kết nối từ đơn khả thi, sinh Account ID |
| GET | `/api/tech/connections` | Danh sách kết nối: `status`, `type`, `suspendRequested` |
| GET | `/api/tech/connections/{accountId}` | Chi tiết kết nối + thiết bị + hóa đơn |
| POST | `/api/tech/connections/status` | Đổi trạng thái: `{ accountId, status, reason }` |
| POST | `/api/tech/equipment/replace` | Thay thiết bị hỏng, ghi phí `UNBILLED` |
| GET | `/api/tech/equipment` | Tồn kho thiết bị (TECH, ADMIN) |

```jsonc
// POST /api/tech/feasibility
{
  "orderId": "D0000000002",
  "feasible": true,
  "distanceKm": 2.5,
  "serverAvailable": true,
  "landlineFeasible": true,
  "internetFeasible": true,
  "reason": null                    // bắt buộc khi feasible = false
}

// POST /api/tech/connections
{ "orderId": "D0000000002", "activationDate": "2026-11-10",
  "equipmentProductId": 1, "equipmentSerial": "MDM-0001" }

// POST /api/tech/connections/status
{ "accountId": "D002000000000001", "status": "TEMP_INACTIVE", "reason": "Quá hạn 30 ngày" }

// POST /api/tech/equipment/replace
{ "accountId": "B001000000000001", "oldSerial": "RTR-CISCO-8801",
  "newProductId": 2, "replacementFee": 40.00 }
```

**`POST /api/tech/connections`** — một transaction:
1. Đơn phải `FEASIBLE` và `connectedQty < quantity`, nếu không trả `NOT_FEASIBLE`/`ORDER_STATE_INVALID`.
2. Lấy mã thành phố từ khách/cửa hàng, sinh Account ID.
3. Trừ tồn kho thiết bị; thiếu thì `OUT_OF_STOCK` và rollback.
4. Tạo `Connection` (`ACTIVE`), gắn thiết bị. Đơn Dial-up có `landlinePlanId`: tạo thêm kết nối T với
   Account ID riêng và gắn vào `LandlineAccountId` của kết nối D.
5. Tăng `connectedQty`; đủ số lượng thì đơn thành `CONNECTED`.

Chuyển trạng thái hợp lệ: `ACTIVE → TEMP_INACTIVE`, `TEMP_INACTIVE → ACTIVE`,
`ACTIVE|TEMP_INACTIVE → PERM_INACTIVE`. Từ `PERM_INACTIVE` không chuyển tiếp. Không ngưng vĩnh viễn
landline khi Dial-up gắn với nó còn hoạt động (`LANDLINE_IN_USE`).

### 4.7 Hóa đơn — `/api/billing`

| Method | URL | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/billing/preview` | ACCOUNTS, ADMIN | Tính thử, không lưu |
| POST | `/api/billing/generate` | ACCOUNTS | Lập hóa đơn |
| PUT | `/api/billing/{billId}` | ACCOUNTS | Sửa hóa đơn mới nhất, chưa có thanh toán |
| GET | `/api/billing` | ACCOUNTS, ADMIN, RETAIL; CUSTOMER xem của mình | Danh sách, phân trang |
| GET | `/api/billing/{billId}` | như trên | Chi tiết + cước gọi + thanh toán |
| GET | `/api/billing/overdue` | ACCOUNTS, ADMIN | Hóa đơn quá hạn: `days` |
| POST | `/api/billing/{accountId}/request-suspend` | ACCOUNTS | Đề nghị kỹ thuật tạm ngưng: `{ reason }` |

```jsonc
// POST /api/billing/preview | /generate
{
  "accountId": "B001000000000001",
  "periodStart": "2026-11-01",
  "periodEnd": "2026-11-30",
  "minutesLocal": 0, "minutesStd": 0, "minutesSms": 0,
  "usageHours": 0,
  "adjustment": 0,
  "note": null
}
```

Công thức (BR-07, `BillingCalculator`):

```
planFee      = phí gói nếu kỳ này bắt đầu một chu kỳ hiệu lực, ngược lại 0
               kỳ đầu tiên: planFee = advance = phí gói × (1 − discountPercent)
taxableBase  = planFee + callCharges + overageCharges + replacementCharges + adjustment
tax          = round(taxableBase × taxRate, 2)          // taxRate = 12,24% đọc từ SystemConfig
deposit      = đặt cọc × (1 − discountPercent), chỉ kỳ đầu, không chịu thuế
total        = taxableBase + tax + deposit
dueAmount    = total − paidAmount                        // không được âm
previousDue  = tổng dueAmount các hóa đơn còn mở trước đó (chỉ hiển thị)
amountToPay  = total + previousDue
```

- Kỳ cước theo tháng. Gói quý/6 tháng/năm chỉ thu phí gói ở kỳ đầu mỗi chu kỳ hiệu lực.
- Phí gói lấy theo giá chụp trên đơn; ưu đãi chỉ áp cho advance và đặt cọc, chu kỳ sau thu đủ giá.
- Giờ của gói tính trên cả chu kỳ hiệu lực nên vượt gói tính luỹ kế và chỉ thu phần vượt mới:
  `overageCharges = (max(0, priorHours + usageHours − plan.hours) − max(0, priorHours − plan.hours)) × overageRatePerHour`.
- `adjustment` có thể âm nhưng `taxableBase` không được âm.
- Hạn thanh toán = ngày lập + `BILL_DUE_DAYS` (mặc định 15). Quá hạn theo `OVERDUE_DAYS` (mặc định 30).
- `replacementCharges` lấy từ khoản `UNBILLED` của kết nối rồi đánh dấu `BILLED` trong cùng transaction.
- Không lập hóa đơn trùng/chồng lấn kỳ cước cùng `accountId` (`BILL_EXISTS`).
- Làm tròn `HALF_UP` bằng `decimal` của .NET.

Ví dụ kỳ đầu, Broadband Unlimited 64 Kbps tháng (225$), cọc 500$:

| Trường hợp | Advance | Thuế | Cọc | Tổng |
|---|---|---|---|---|
| 1 kết nối, không giảm | 225,00$ | 27,54$ | 500,00$ | **752,54$** |
| Đơn 10 kết nối, giảm 25% (mỗi kết nối) | 168,75$ | 20,66$ | 375,00$ | **564,41$** |

Hai con số này là test case trong `backend/tests/Nexus.Tests/BillingCalculatorTests.cs`.

### 4.8 Thanh toán — `/api/payments`

| Method | URL | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/payments` | RETAIL, ACCOUNTS | Ghi nhận thanh toán |
| GET | `/api/payments` | RETAIL, ACCOUNTS, ADMIN | Lịch sử: `billId`, `accountId`, `from`, `to` |
| GET | `/api/payments/{paymentId}/receipt` | RETAIL, ACCOUNTS, ADMIN; CUSTOMER của mình | Biên lai |
| POST | `/api/payments/refund-deposit/{accountId}` | ACCOUNTS | Hoàn tiền đặt cọc |

```jsonc
// POST /api/payments
{ "billId": 1, "amount": 100.00, "method": "CASH",
  "paidDate": "2026-11-09", "note": "Khách trả tại quầy" }
```

`method` ∈ `CASH | CARD | TRANSFER`. Ghi thanh toán và cập nhật `PaidAmount`, `DueAmount` trong một
transaction, khoá dòng hóa đơn trước khi cộng tiền. `amount > dueAmount` trả `PAYMENT_EXCEEDS_DUE`.
Khi `dueAmount = 0`, hóa đơn thành `PAID`.

Hoàn cọc chỉ khi kết nối đã `PERM_INACTIVE`, mỗi kết nối một lần (`DEPOSIT_ALREADY_REFUNDED`):

```
refund = max(0, cọc đã lập hóa đơn − tổng dueAmount hóa đơn còn mở − phí thay thế UNBILLED)
```

Phần đã trừ vào cọc được bù trừ thật trong cùng transaction: hóa đơn còn nợ (cũ nhất trước) nhận thanh
toán `DEPOSIT_OFFSET`, phí `UNBILLED` chuyển `DEPOSIT_OFFSET`. Người dùng không chọn được hình thức này
ở `/api/payments`.

### 4.9 Cổng khách hàng — `/api/my` (R: CUSTOMER)

| Method | URL | Mô tả |
|---|---|---|
| GET | `/api/my/profile` | Hồ sơ liên hệ |
| PUT | `/api/my/profile` | Cập nhật `{ phone, email, address }` |
| GET | `/api/my/orders` | Đơn của mình |
| GET | `/api/my/connections` | Kết nối, số nợ, thiết bị đang cấp |
| GET | `/api/my/bills` | Hóa đơn và thanh toán |
| GET | `/api/my/order-requests` | Yêu cầu đặt đơn của mình |
| POST | `/api/my/order-requests` | Gửi yêu cầu: `{ connType, planId, shopId, note }` |
| POST | `/api/my/feedback` | Gửi phản hồi: `{ refType, refId, rating, comment }` |

Mọi truy vấn trong `/api/my/*` lọc theo `customerId` **lấy từ token**, không bao giờ nhận `customerId`
từ client. `refType` ∈ `ORDER | CONNECTION`; server kiểm tra `refId` thuộc đúng khách.

### 4.10 Yêu cầu đặt đơn — `/api/order-requests` (R: RETAIL, ADMIN)

| Method | URL | Mô tả |
|---|---|---|
| GET | `/api/order-requests` | Yêu cầu đang chờ: `status` |
| POST | `/api/order-requests/{requestId}/reject` | Từ chối kèm lý do: `{ reason }` |

Nhân viên xác nhận yêu cầu bằng cách tạo đơn qua `POST /api/orders` với `requestId` tương ứng.

### 4.11 Kiểm tra sức khoẻ — `/api/health`

`GET /api/health` (công khai): trả `{ success, data: { database: "up" | "down" } }`.
Dùng cho healthcheck của container và CI/CD sau khi deploy.

### 4.12 OpenAPI

Ở môi trường Development, đặc tả OpenAPI có tại `http://localhost:5080/openapi/v1.json`
(`app.MapOpenApi()`). Có thể import vào Postman hoặc Swagger UI để thử endpoint.

---

## 5. Cấu trúc mã nguồn (.NET 10)

```
backend/
├─ Nexus.slnx
├─ src/Nexus.Api/
│  ├─ Program.cs                  # DI, JWT, CORS, middleware, /api/health
│  ├─ Common/
│  │  ├─ ErrorCode.cs             # enum mã lỗi nghiệp vụ
│  │  ├─ BusinessException.cs     # exception → HTTP status
│  │  └─ ApiResponse.cs           # vỏ JSON + PagedResult
│  ├─ Domain/                     # Identity, Configuration, Catalog, Orders, Billing
│  ├─ Data/NexusDbContext.cs      # map 25 bảng, DateOnly converter
│  ├─ Services/
│  │  ├─ BillingCalculator.cs     # công thức cước + DiscountService + IdGenerator + ConfigService
│  │  ├─ AuthService.cs           # JWT + BCrypt
│  │  ├─ OrderService.cs          # tạo đơn, khảo sát, cấp kết nối, đổi trạng thái
│  │  ├─ BillingService.cs        # lập/sửa hóa đơn
│  │  ├─ PaymentService.cs        # thanh toán, hoàn cọc
│  │  ├─ CatalogService.cs        # gói cước, thành phố, cửa hàng, ưu đãi
│  │  ├─ CustomerService.cs       # hồ sơ khách, cấp tài khoản portal
│  │  ├─ AdminService.cs          # danh mục quản trị
│  │  ├─ PortalService.cs         # /my/*, tra cứu công khai, phản hồi
│  │  └─ ExceptionHandlingMiddleware.cs
│  └─ Controllers/                # Auth, Public, Customers, Orders+Tech, Billing,
│                                 # Payments, Portal+OrderRequests, Admin
└─ tests/Nexus.Tests/             # xUnit: BillingCalculatorTests, BusinessRuleTests
```

Nguyên tắc: controller chỉ kiểm tra quyền và ánh xạ DTO; toàn bộ nghiệp vụ nằm trong service;
service nhận `CurrentUser` từ controller, không đọc `HttpContext`.

---

## 6. Yêu cầu phi chức năng áp dụng cho API

| Mã | Yêu cầu | Cách đáp ứng |
|---|---|---|
| NFR-01 | Truy vấn tham số hoá, chống SQL Injection | EF Core LINQ; chỗ cần SQL thô dùng `FromSqlInterpolated` |
| NFR-02 | Mật khẩu không lưu thuần văn bản | BCrypt (`BCrypt.Net-Next`), giữ thuật toán `$2a$` tương thích seed cũ |
| NFR-03 | Thao tác nhiều bước phải nguyên tử | `BeginTransactionAsync` cho tạo đơn, cấp kết nối, thanh toán, hoàn cọc |
| NFR-04 | Hiệu năng tra cứu ≤ 2 giây | Index sẵn có trên `OrderId`, `AccountId`, `Phone`; `AsNoTracking` cho truy vấn đọc |
| NFR-05 | Không xoá cứng dữ liệu đã phát sinh | Xoá mềm qua `Status`, trạng thái thay vì DELETE |
| NFR-06 | Phân quyền chặt theo vai trò | `[Authorize]` + `RequireRoles`, khách chỉ thấy dữ liệu của mình |
| NFR-07 | Cấu hình không hard-code | Giá, ưu đãi, thuế, cọc, hạn thanh toán trong `SystemConfig` / `BulkDiscounts` |
| NFR-08 | Ghi nhật ký thao tác quan trọng | Bảng `AuditLog` (schema sẵn sàng) |
| NFR-09 | Chạy được trên Azure | Docker image `aspnet:10.0`, cấu hình qua App Settings, `/api/health` để probe |
| NFR-10 | Sao lưu dữ liệu | SQL Server backup định kỳ; `database/backup/` cho bản nộp |

---

## 7. Triển khai (Azure)

| Thành phần | Dịch vụ gợi ý | Ghi chú |
|---|---|---|
| API | Azure App Service (Linux) hoặc Container Apps | Dùng `backend/Dockerfile` |
| CSDL | Azure SQL Database | Chạy `database/schema.sql` + `seed.sql` |
| Web client | Azure Static Web Apps hoặc App Service | `npm run build` rồi deploy `frontend/dist` |
| CI/CD | GitHub Actions | `dotnet test` → `dotnet publish` → deploy API; `npm ci` → `npm run build` → deploy web |
| Bí mật | GitHub Secrets + App Service Settings | `ConnectionStrings__Nexus`, `Jwt__Key`, `Cors__AllowedOrigins__0` |

Bật CORS chỉ cho đúng domain web client (`Cors:AllowedOrigins`), không dùng `AllowAnyOrigin`
khi đã phát hành.

---

## 8. Phần cần mentor xác nhận

1. Giá gói **STD Yearly** bị bỏ trống trong đề (seed tạm 780$).
2. **Half-yearly** đề ghi "valid for a month" (seed tạm 6 tháng).
3. Ai được đặt đơn: chỉ nhân viên cửa hàng, hay cả khách hàng tự đặt.
4. Ưu đãi ở giá trị biên (đúng 15, 25, 50) và khi dưới 10 kết nối.
5. Định nghĩa "advance" (phí ứng trước); thuế có tính trên đặt cọc và nợ kỳ trước không.
6. Quy tắc chuyển trạng thái kết nối theo tình trạng thanh toán.
7. Giá vượt giờ của gói theo giờ.
8. Ngày kết thúc dự án.

Chi tiết và giả định tạm thời: `srs.md`, mục 10. Nội dung email: `DOUBT_Email_NEXUS.md`.