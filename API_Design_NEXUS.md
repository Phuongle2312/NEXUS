# THIẾT KẾ API / URL: NEXUS SERVICE MARKETING SYSTEM

| Mục | Nội dung |
|---|---|
| Công nghệ | Java **JDK 17**, **JSP + Servlets** (Jakarta EE, package `jakarta.*`), **JDBC**, **SQL Server 2019 trở lên** |
| Máy chủ ứng dụng | Apache **Tomcat 10.1**. Nếu máy lab chỉ có JDK 8 thì cả nhóm chuyển sang Tomcat 9 và `javax.*` |
| Kiểu giao tiếp | Server-rendered (form POST + redirect). Tra cứu có thêm endpoint JSON |
| Tài liệu liên quan | `SRS_NEXUS_Service_Marketing_System.docx` (các mã FR-xxx, BR-xx) |
| Phiên bản | 1.2 (04/11/2026), đồng bộ với SRS 1.2 và mã nguồn. Giả định chờ mentor xác nhận: xem mục 7 |

---

## 1. Quy ước chung

### 1.1 Cấu trúc URL
```
/nexus/<module>/<action>
```
Ví dụ: `/nexus/order/create`, `/nexus/billing/generate`.

### 1.2 Phương thức
- `GET`: hiển thị trang hoặc lấy dữ liệu (không đổi dữ liệu).
- `POST`: tạo/sửa/xóa/chuyển trạng thái. Sau khi thành công: redirect (mẫu Post-Redirect-Get) kèm thông báo.
- Sửa/xóa dùng `POST` (form HTML không hỗ trợ PUT/DELETE), tham số `id` trong body.

### 1.3 Xác thực và phân quyền
- Đăng nhập tạo `HttpSession` chứa `userId`, `role` (`ADMIN`, `RETAIL`, `TECH`, `ACCOUNTS`, `CUSTOMER`).
- **AuthFilter** chặn mọi URL ngoại trừ nhóm công khai (`/public/*`, `/auth/login`).
- **RoleFilter** kiểm tra `role` theo bảng phân quyền ở mục 3. Sai quyền trả `403`.
- Mật khẩu băm (BCrypt hoặc PBKDF2). Session timeout 30 phút.
- Token CSRF cho mọi `POST`.

### 1.4 Định dạng JSON (chỉ cho nhóm `/nexus/api/*`)
Thành công:
```json
{ "success": true, "data": { }, "message": "OK" }
```
Lỗi:
```json
{ "success": false, "error": { "code": "ORDER_NOT_FOUND", "message": "Không tìm thấy đơn D0000000001" } }
```

### 1.5 Mã trạng thái HTTP
| Mã | Dùng khi |
|---|---|
| 200 | Thành công |
| 302 | Redirect sau POST thành công |
| 400 | Dữ liệu không hợp lệ |
| 401 | Chưa đăng nhập |
| 403 | Không đủ quyền |
| 404 | Không tìm thấy |
| 409 | Xung đột nghiệp vụ (ví dụ: ID trùng, thao tác sai trạng thái) |
| 500 | Lỗi hệ thống |

### 1.6 Định dạng dữ liệu
- Ngày: `yyyy-MM-dd`. Tiền: số thập phân 2 chữ số, USD.
- Encoding UTF-8.
- Phân trang: `page` (từ 1), `size` (mặc định 20).

### 1.7 Bảng mã lỗi nghiệp vụ
| Mã | Ý nghĩa |
|---|---|
| `AUTH_INVALID` | Sai tên đăng nhập hoặc mật khẩu |
| `FORBIDDEN` | Không đủ quyền |
| `VALIDATION_ERROR` | Thiếu hoặc sai dữ liệu |
| `NOT_FOUND` | Không tìm thấy dữ liệu (hóa đơn, biên lai, danh mục) |
| `DUPLICATE` | Dữ liệu trùng khóa |
| `IN_USE` | Dữ liệu đang được dùng nên không xóa hoặc đổi mã được; dùng deactivate |
| `BILL_STATE_INVALID` | Hóa đơn đã có thanh toán hoặc không phải hóa đơn mới nhất nên không sửa được |
| `ORDER_NOT_FOUND` | Không có Order ID |
| `ACCOUNT_NOT_FOUND` | Không có Account ID |
| `ORDER_STATE_INVALID` | Đơn không ở trạng thái cho phép thao tác |
| `NOT_FEASIBLE` | Đơn chưa khả thi, không thể tạo kết nối |
| `LANDLINE_REQUIRED` | Dial-up cần landline Nexus |
| `CONNECTION_STATE_INVALID` | Chuyển trạng thái kết nối không hợp lệ |
| `OUT_OF_STOCK` | Thiết bị hết hàng |
| `PAYMENT_EXCEEDS_DUE` | Số tiền thanh toán vượt số còn nợ |
| `BILL_EXISTS` | Đã có hóa đơn trùng hoặc chồng lấn kỳ cước này |
| `LANDLINE_IN_USE` | Landline đang phục vụ Dial-up còn hoạt động, không thể ngưng vĩnh viễn |
| `REQUEST_STATE_INVALID` | Yêu cầu đặt đơn đã được xử lý |
| `DEPOSIT_ALREADY_REFUNDED` | Tiền đặt cọc của kết nối đã được hoàn |

---

## 2. Quy tắc sinh ID (dùng chung cho các endpoint tạo)

Sinh ID **trong transaction**, khóa dòng bộ đếm để tránh trùng khi nhiều người tạo cùng lúc.

### Order ID (11 ký tự)
```
orderId = typeChar + LPAD(nextSeq(typeChar), 10, '0')     // D0000000001
```
Bộ đếm riêng cho `D`, `T`, `B`.

### Account ID (16 ký tự)
```
accountId = typeChar + cityCode(3 chữ số) + LPAD(nextSeq('ACC'), 12, '0')   // B001000000000001
```
Bảng `Sequence` (`seqName`, `currentValue`). Truy vấn: `SELECT ... WITH (UPDLOCK, ROWLOCK)` rồi `UPDATE` trong cùng transaction.

---

## 3. Bảng phân quyền endpoint (tóm tắt)

| Module | ADMIN | RETAIL | TECH | ACCOUNTS | CUSTOMER |
|---|:-:|:-:|:-:|:-:|:-:|
| `/public/*` | ✔ | ✔ | ✔ | ✔ | ✔ (không cần đăng nhập) |
| `/auth/*`, `/dashboard` | ✔ | ✔ | ✔ | ✔ | ✔ |
| `/admin/*` | ✔ | – | – | – | – |
| `/customer-mgmt/*` (hồ sơ khách) | R | CRU | R | R | – |
| `/order/create` | – | ✔ | – | – | (xem ghi chú) |
| `/order/requests/*` | – | ✔ | – | – | – |
| `/order/*` (xem) | ✔ | ✔ | ✔ | ✔ | của mình |
| `/connections/*` (xem) | ✔ | ✔ | ✔ | ✔ | qua `/my/*` |
| `/tech/*` | – | – | ✔ | – | – |
| `/billing/*` (tạo/sửa) | – | – | – | ✔ | – |
| `/billing/*` (xem) | ✔ | ✔ | – | ✔ | của mình |
| `/payment/*` (ghi nhận) | – | ✔ | – | ✔ | – |
| `/payment/list`, `/payment/receipt` (xem) | ✔ | ✔ | – | ✔ | qua `/my/bills` |
| `/my/*` | – | – | – | – | ✔ |
| `/feedback/*` | R | CR | R | R | C |

> Ghi chú: khách hàng tự đặt đơn là điểm chưa rõ của đề (SRS phần 10, mục 3). Thiết kế mặc định: khách gửi **yêu cầu** `POST /my/order-request`, nhân viên cửa hàng xác nhận qua `/order/requests/confirm` để thành đơn chính thức.

---

## 4. Danh sách endpoint

Ký hiệu: **R** = vai trò được phép. Mã yêu cầu SRS ghi ở cột cuối.

### 4.1 Xác thực (`/nexus/auth`)

| Method | URL | Mô tả | Tham số | Kết quả | SRS |
|---|---|---|---|---|---|
| GET | `/auth/login` | Trang đăng nhập | – | JSP | FR-AUTH-01 |
| POST | `/auth/login` | Đăng nhập | `username`, `password` | Redirect dashboard theo vai trò; nếu đang dùng mật khẩu tạm thì chuyển tới đổi mật khẩu; lỗi `AUTH_INVALID` | FR-AUTH-01,03 |
| POST | `/auth/logout` | Đăng xuất, hủy session | – | Redirect `/public/home` | FR-AUTH-04 |
| GET/POST | `/auth/change-password` | Đổi mật khẩu | `oldPassword`, `newPassword` | Redirect | FR-AUTH-04 |
| GET | `/dashboard` | Trang chủ sau đăng nhập, menu theo vai trò | – | JSP | FR-AUTH-01 |

### 4.2 Công khai (`/nexus/public`)

| Method | URL | Mô tả | Tham số | Kết quả | SRS |
|---|---|---|---|---|---|
| GET | `/public/home` | Trang chủ, bảng gói cước | – | JSP | – |
| GET | `/public/plans` | Danh sách gói cước (lọc theo loại) | `type` (D/B/T) | JSP | FR-CUS-02 |
| GET | `/public/track-order` | Tra cứu trạng thái đơn | `orderId` | JSP kết quả | FR-SRC-01 |
| GET | `/public/track-account` | Tra cứu trạng thái kết nối; xem thêm tên và số nợ nếu SĐT khớp | `accountId`, `phone` (tùy chọn) | JSP kết quả | FR-SRC-02 |

Quy tắc kiểm tra tham số: `orderId` khớp `^[DTB][0-9]{10}$`, `accountId` khớp `^[DBT][0-9]{15}$`.

Vì ID được cấp tuần tự nên dễ dò: tra cứu công khai chỉ trả trạng thái. Tên khách và số nợ chỉ hiển thị khi `phone` khớp với số đã đăng ký, hoặc khi đã đăng nhập đúng quyền.

### 4.3 Admin (`/nexus/admin`) — R: ADMIN

Mỗi thực thể có cùng bộ 7 endpoint. Bảng dưới liệt kê thực thể và tham số.

**Bộ endpoint chuẩn** (thay `{res}` bằng `employees`, `shops`, `vendors`, `products`, `plans`, `cities`, `purchases`):

| Method | URL | Mô tả |
|---|---|---|
| GET | `/admin/{res}` | Danh sách, tìm kiếm (`q`, `page`, `size`) |
| GET | `/admin/{res}/new` | Form thêm |
| POST | `/admin/{res}/create` | Thêm mới |
| GET | `/admin/{res}/edit?id=` | Form sửa |
| POST | `/admin/{res}/update` | Cập nhật |
| POST | `/admin/{res}/delete` | Xóa (chặn nếu đã có dữ liệu liên quan, trả `409`) |
| POST | `/admin/{res}/deactivate` | Ngưng sử dụng (`status = INACTIVE`) khi đã có dữ liệu liên quan, giữ nguyên lịch sử |

**Tham số theo thực thể**

| Thực thể | Tham số chính | SRS |
|---|---|---|
| `employees` | `fullName`, `role` (ADMIN/ACCOUNTS/TECH/RETAIL), `phone`, `email`, `shopId` (nếu RETAIL), `username`, `password` | FR-ADM-01 |
| `shops` | `name`, `address`, `cityId`, `phone` | FR-ADM-02 |
| `vendors` | `name`, `contact`, `phone`, `email` | FR-ADM-03 |
| `purchases` | `vendorId`, `purchaseDate`, danh sách `items[prodId, qty, unitPrice]`, `amountPaid` | FR-ADM-04 |
| `products` | `name`, `type` (MODEM/ROUTER/...), `vendorId`, `price`, `stockQty` | FR-ADM-05 |
| `plans` | `connType`, `groupName`, `name`, `price`, `validityMonths`, `hours`, `overageRatePerHour` (gói theo giờ), `speedKbps`, `description`, danh sách `callRates[callType, ratePerMin]` | FR-ADM-06 |
| `cities` | `name`, `code` (3 chữ số, duy nhất) | FR-ADM-07 |

Endpoint đặc biệt:

| Method | URL | Mô tả | SRS |
|---|---|---|---|
| POST | `/admin/purchases/receive` | Nhận hàng: cộng tồn kho trong transaction | FR-ADM-04 |
| POST | `/admin/employees/reset-password` | Đặt lại mật khẩu cho nhân viên | FR-ADM-01 |

Báo cáo tồn kho và nhu cầu thiết bị dùng chung `/reports/stock` (mục 4.13).

Phân vai thiết bị: Admin quản lý danh mục sản phẩm và nhập hàng từ nhà cung cấp; Technical cấp phát, thay thế và điều chỉnh tồn kho (mục 4.6).

### 4.4 Hồ sơ khách hàng (`/nexus/customer-mgmt`) — R: RETAIL (CRU), ADMIN/TECH/ACCOUNTS (xem)

| Method | URL | Mô tả | Tham số | SRS |
|---|---|---|---|---|
| GET | `/customer-mgmt/list` | Danh sách, tìm kiếm | `q`, `page` | FR-RET-01 |
| GET | `/customer-mgmt/new` | Form thêm | – | FR-RET-01 |
| POST | `/customer-mgmt/create` | Thêm khách | `fullName`, `address`, `cityId`, `phone`, `email`, `custType` (INDIVIDUAL/CORPORATE), `addressProofFile` (multipart) | FR-RET-01,09 |
| POST | `/customer-mgmt/update` | Cập nhật | như trên + `id` | FR-RET-01 |
| GET | `/customer-mgmt/view?id=` | Chi tiết, kèm đơn, kết nối | – | FR-RET-04,05 |
| POST | `/customer-mgmt/issue-account` | Cấp tài khoản cho khách. Mật khẩu tạm chỉ hiển thị một lần, phải đổi ở lần đăng nhập đầu | `custId` | FR-AUTH-06 |
| POST | `/customer-mgmt/reset-password` | Cấp lại mật khẩu tạm cho khách | `custId` | FR-AUTH-06 |

### 4.5 Đơn đăng ký (`/nexus/order`)

| Method | URL | Vai trò | Mô tả | SRS |
|---|---|---|---|---|
| GET | `/order/new` | RETAIL | Form tạo đơn | FR-RET-02 |
| POST | `/order/create` | RETAIL | Tạo đơn, sinh Order ID | FR-RET-02 |
| GET | `/order/list` | RETAIL, TECH, ADMIN, ACCOUNTS | Danh sách, lọc theo `status`, `type`, `cityId`, `from`, `to` | FR-RET-04, FR-TEC-01 |
| GET | `/order/view?id=` | tất cả (khách chỉ đơn của mình) | Chi tiết và lịch sử trạng thái | FR-RET-04, FR-CUS-04 |
| POST | `/order/cancel` | RETAIL | Hủy đơn khi chưa cấp kết nối nào | FR-RET-10 |
| GET | `/order/requests` | RETAIL | Yêu cầu đặt đơn online đang chờ của cửa hàng | FR-CUS-03 |
| POST | `/order/requests/confirm` | RETAIL | Xác nhận yêu cầu, mở `/order/new` với dữ liệu điền sẵn | FR-CUS-03 |
| POST | `/order/requests/reject` | RETAIL | Từ chối yêu cầu, kèm `reason` | FR-CUS-03 |

**`POST /order/create`**

Tham số:
```
custId            bắt buộc
connType          D | B | T
planId            bắt buộc (phải khớp connType)
equipmentProdId   Modem/Router (Dial-up, Broadband)
quantity          số kết nối (>=1), cùng loại và cùng gói; >=10 thì áp dụng ưu đãi
existingLandlineAccountId   Dial-up: landline Nexus đang có của khách (chỉ khi quantity=1)
landlinePlanId              Dial-up: gói landline khi đăng ký mới cả điện thoại và internet
requestId         tùy chọn: yêu cầu online được chuyển thành đơn
```
Xử lý (một transaction):
1. Kiểm tra dữ liệu. Với Dial-up: phải có `landlinePlanId`, hoặc `existingLandlineAccountId` là kết nối loại T, đang `ACTIVE`, của đúng khách này (server tự kiểm tra, không tin dữ liệu từ form). Nếu không trả `LANDLINE_REQUIRED`.
2. Sinh Order ID (mục 2).
3. Tính ưu đãi theo `quantity` (SRS BR-05). Lưu vào đơn: `discountPercent`, tiền đặt cọc mỗi kết nối, phí gói tại thời điểm đặt, `connectedQty = 0`.
4. Lưu đơn, `status = SUBMITTED`. Ghi `employeeId`, `shopId`, thời gian.
5. Redirect `/order/view?id=<orderId>`.

Trạng thái đơn: `SUBMITTED` → `FEASIBILITY_CHECK` → `FEASIBLE` → `CONNECTED` (hoặc `NOT_FEASIBLE`, `CANCELLED`). Đơn chỉ thành `CONNECTED` khi `connectedQty = quantity`.

### 4.6 Kỹ thuật (`/nexus/tech`) — R: TECH

| Method | URL | Mô tả | Tham số | SRS |
|---|---|---|---|---|
| GET | `/tech/orders` | Đơn chờ xử lý | `status`, `type`, `cityId`, `page` | FR-TEC-01 |
| POST | `/tech/orders/start-check` | Nhận đơn, chuyển `SUBMITTED` → `FEASIBILITY_CHECK` | `orderId` | FR-TEC-02 |
| POST | `/tech/orders/feasibility` | Ghi kết quả khả thi | `orderId`, `result` (FEASIBLE/NOT_FEASIBLE), `distanceKm`, `serverAvailable`, `landlineFeasible`, `internetFeasible`, `reason` (bắt buộc nếu không khả thi) | FR-TEC-02,06 |
| POST | `/tech/connections/create` | Tạo **một** kết nối từ đơn khả thi, cấp Account ID; đơn nhiều kết nối thì gọi nhiều lần | `orderId`, `activationDate`, `equipment[prodId, serial]` | FR-TEC-03 |
| GET | `/tech/connections` | Danh sách kết nối, gồm các đề nghị tạm ngưng của kế toán | `status`, `type`, `cityId`, `suspendRequested` | FR-TEC-03, FR-ACC-04 |
| POST | `/tech/connections/suspend` | Tạm ngưng | `accountId`, `reason` | FR-TEC-04 |
| POST | `/tech/connections/reactivate` | Kích hoạt lại | `accountId` | FR-TEC-04 |
| POST | `/tech/connections/terminate` | Ngưng vĩnh viễn | `accountId`, `reason` | FR-TEC-04 |
| GET | `/tech/equipment` | Tồn kho và thiết bị đã cấp | `q` | FR-TEC-05 |
| POST | `/tech/equipment/replace` | Thay thiết bị hỏng, tạo khoản phí thay thế (`UNBILLED`) để hóa đơn kỳ sau tự lấy | `accountId`, `oldSerial`, `newProdId`, `replacementFee` | FR-TEC-05 |
| POST | `/tech/equipment/stock-adjust` | Điều chỉnh tồn kho | `prodId`, `delta`, `note` | FR-TEC-05 |

**`POST /tech/connections/create`**: một transaction
1. Đơn phải ở trạng thái `FEASIBLE` và `connectedQty < quantity`, nếu không trả `NOT_FEASIBLE` hoặc `ORDER_STATE_INVALID`.
2. Lấy mã thành phố từ khách/cửa hàng, sinh Account ID (mục 2).
3. Trừ tồn kho thiết bị, nếu thiếu trả `OUT_OF_STOCK` và rollback.
4. Tạo `Connection` (`status = ACTIVE`), gắn thiết bị. Đơn Dial-up có `landlinePlanId`: tạo thêm kết nối loại T với Account ID riêng và gắn vào `landlineAccountId` của kết nối D.
5. Tăng `connectedQty`; khi `connectedQty = quantity` thì cập nhật đơn thành `CONNECTED`.

Chuyển trạng thái kết nối hợp lệ: `ACTIVE → TEMP_INACTIVE`, `TEMP_INACTIVE → ACTIVE`, `ACTIVE|TEMP_INACTIVE → PERM_INACTIVE`. Từ `PERM_INACTIVE` không chuyển tiếp (trả `CONNECTION_STATE_INVALID`). Không ngưng vĩnh viễn landline khi Dial-up gắn với nó còn hoạt động (`LANDLINE_IN_USE`).

**Xem kết nối** (`/nexus/connections`), R: ADMIN, RETAIL, TECH, ACCOUNTS

| Method | URL | Mô tả | Tham số | SRS |
|---|---|---|---|---|
| GET | `/connections/list` | Danh sách kết nối đã cấp và số còn nợ | `q`, `status`, `type`, `cityId`, `page` | FR-RET-05 |
| GET | `/connections/view?id=` | Chi tiết kết nối, thiết bị, hóa đơn | – | FR-RET-05 |

### 4.7 Hóa đơn (`/nexus/billing`)

| Method | URL | Vai trò | Mô tả | Tham số | SRS |
|---|---|---|---|---|---|
| GET | `/billing/list` | ACCOUNTS, RETAIL, ADMIN | Danh sách hóa đơn | `accountId`, `status`, `from`, `to`, `page` | FR-RET-05 |
| GET | `/billing/new` | ACCOUNTS | Form lập hóa đơn | `accountId` | FR-ACC-01 |
| POST | `/billing/preview` | ACCOUNTS | Tính thử, chưa lưu | như `generate` | FR-ACC-01 |
| POST | `/billing/generate` | ACCOUNTS | Lập hóa đơn | `accountId`, `periodStart`, `periodEnd`, `minutesLOCAL/STD/SMS`, `usageHours`, `adjustment`, `note` | FR-ACC-01,02 |
| GET | `/billing/edit?id=` | ACCOUNTS | Form sửa hóa đơn | – | FR-ACC-02 |
| POST | `/billing/update` | ACCOUNTS | Sửa hóa đơn mới nhất, chưa có thanh toán; kỳ cước giữ nguyên | `id`, `minutesLOCAL/STD/SMS`, `usageHours`, `adjustment`, `note` | FR-ACC-02 |
| GET | `/billing/view?id=` | ADMIN, RETAIL, ACCOUNTS, CUSTOMER (khách chỉ của mình) | Chi tiết, bản in | – | FR-CUS-06 |
| GET | `/billing/overdue` | ACCOUNTS | Hóa đơn quá hạn thanh toán | `days` (mặc định theo cấu hình, 30) | FR-ACC-04 |
| POST | `/billing/request-suspend` | ACCOUNTS | Đề nghị Technical tạm ngưng kết nối quá hạn | `accountId`, `reason` | FR-ACC-04 |

**`POST /billing/generate`**: công thức (SRS BR-07)
```
planFee      = phí gói nếu kỳ này bắt đầu một chu kỳ hiệu lực, ngược lại 0
               kỳ đầu tiên: planFee = advance = phí gói × (1 − discountPercent)
taxableBase  = planFee + callCharges + overageCharges + replacementCharges + adjustment
tax          = round(taxableBase × taxRate, 2)
deposit      = đặt cọc × (1 − discountPercent), chỉ ở kỳ đầu, không chịu thuế
total        = taxableBase + tax + deposit
dueAmount    = total − paidAmount

previousDue  = tổng dueAmount các hóa đơn còn mở trước đó (chỉ hiển thị)
amountToPay  = total + previousDue
```
- Kỳ cước theo tháng. Gói quý, 6 tháng, năm chỉ thu phí gói ở kỳ đầu mỗi chu kỳ hiệu lực.
- Phí gói lấy theo giá chụp trên đơn, kể cả các chu kỳ sau, nên Admin đổi giá không ảnh hưởng kết nối đã cấp. Ưu đãi chỉ áp dụng cho advance và đặt cọc, chu kỳ sau thu đủ giá.
- Số giờ của gói tính trên cả chu kỳ hiệu lực (gói 30 giờ / 3 tháng là 30 giờ cho 3 tháng), nên vượt gói tính lũy kế và chỉ thu phần vượt mới: `overageCharges = (max(0, priorHours + usageHours − plan.hours) − max(0, priorHours − plan.hours)) × plan.overageRatePerHour`, với `priorHours` là tổng giờ các kỳ trước trong cùng chu kỳ.
- `adjustment` có thể âm, nhưng `taxableBase` không được âm.
- Hạn thanh toán = ngày lập + `BILL_DUE_DAYS` (mặc định 15). Quá hạn tính từ hạn thanh toán, theo `OVERDUE_DAYS` (mặc định 30).
- `replacementCharges` lấy từ các khoản `UNBILLED` của kết nối, rồi đánh dấu `BILLED` trong cùng transaction.
- `previousDue` không cộng vào `total` để tránh tính nợ hai lần và thuế chồng thuế. Mỗi hóa đơn giữ số nợ của chính nó; số nợ của kết nối là tổng `dueAmount` các hóa đơn còn mở.
- Không cho lập hóa đơn trùng hoặc chồng lấn kỳ cước của cùng `accountId` (`BILL_EXISTS`). Kiểm tra chồng lấn ở Service, khóa unique ở DB chỉ chặn trùng tuyệt đối.
- Thuế suất 12,24% đọc từ bảng cấu hình, không hard-code. Làm tròn `HALF_UP`, dùng `BigDecimal`.

Ví dụ kỳ đầu, Broadband Unlimited 64 Kbps tháng (225$), cọc 500$:

| Trường hợp | Advance | Thuế | Cọc | Tổng |
|---|---|---|---|---|
| 1 kết nối, không giảm | 225,00$ | 27,54$ | 500,00$ | 752,54$ |
| Đơn 10 kết nối, giảm 25% (mỗi kết nối) | 168,75$ | 20,66$ | 375,00$ | 564,41$ |

### 4.8 Thanh toán (`/nexus/payment`) — R: RETAIL, ACCOUNTS (ADMIN chỉ xem)

| Method | URL | Mô tả | Tham số | SRS |
|---|---|---|---|---|
| GET | `/payment/list` | Lịch sử thanh toán | `accountId`, `billId`, `from`, `to` | FR-RET-05 |
| GET | `/payment/new?billId=` | Form ghi nhận | – | FR-RET-06 |
| POST | `/payment/create` | Ghi nhận thanh toán | `billId`, `amount`, `method` (CASH/CARD/TRANSFER), `paidDate`, `note` | FR-RET-06, FR-ACC-03 |
| GET | `/payment/receipt?id=` | Biên lai (bản in) | – | FR-RET-06 |
| POST | `/payment/refund-deposit` | ACCOUNTS: hoàn tiền đặt cọc khi hủy | `accountId` | FR-ACC-06 |

`POST /payment/create`: transaction ghi thanh toán và cập nhật `paidAmount`, `dueAmount` của hóa đơn. Nếu `amount > dueAmount` trả `PAYMENT_EXCEEDS_DUE`. Khi `dueAmount = 0` đánh dấu hóa đơn `PAID`.

Hoàn cọc chỉ áp dụng khi kết nối đã `PERM_INACTIVE`, mỗi kết nối một lần (`DEPOSIT_ALREADY_REFUNDED`):
```
refund = max(0, đặt cọc đã lập hóa đơn − tổng dueAmount hóa đơn còn mở − phí thay thế UNBILLED)
```
Phí thay thế đã lên hóa đơn không bị trừ lần nữa (SRS phần 10, mục 14).

Phần đã trừ vào cọc được bù trừ thật trong cùng transaction, nếu không khách bị thu hai lần: hóa đơn còn nợ (cũ nhất trước) nhận thanh toán `DEPOSIT_OFFSET`, phí `UNBILLED` chuyển `DEPOSIT_OFFSET` (cọc không đủ thì giảm số tiền của khoản đó). Người dùng không tự chọn được hình thức này ở `/payment/create`.

### 4.9 Khu vực khách hàng (`/nexus/my`) — R: CUSTOMER

| Method | URL | Mô tả | Tham số | SRS |
|---|---|---|---|---|
| GET | `/my/profile` | Xem thông tin liên hệ | – | FR-CUS-01 |
| POST | `/my/profile/update` | Cập nhật liên hệ | `phone`, `email`, `address` | FR-CUS-01 |
| POST | `/my/order-request` | Gửi yêu cầu đặt kết nối (chờ cửa hàng xác nhận) | `connType`, `planId`, `shopId`, `note` | FR-CUS-03 |
| GET | `/my/orders` | Đơn và yêu cầu đặt đơn của mình | – | FR-CUS-04 |
| GET | `/my/connections` | Kết nối và số nợ | – | FR-CUS-05 |
| GET | `/my/bills` | Hóa đơn và thanh toán | – | FR-CUS-06 |
| POST | `/my/feedback` | Gửi phản hồi | `refType` (ORDER/CONNECTION), `refId`, `rating`, `comment` | FR-CUS-07 |

Mọi truy vấn trong `/my/*` lọc theo `custId` từ session, không nhận `custId` từ client.

### 4.10 Tìm kiếm nâng cao (`/nexus/search`) — R: tất cả đã đăng nhập

| Method | URL | Mô tả | SRS |
|---|---|---|---|
| GET | `/search/advanced` | Form và kết quả tìm nâng cao (JSP) | FR-SRC-03 |
| GET | `/api/search/orders` | Kết quả JSON cho đơn | FR-SRC-03 |
| GET | `/api/search/connections` | Kết quả JSON cho kết nối | FR-SRC-03 |

Tham số (có thể kết hợp, bỏ trống thì bỏ qua tiêu chí):
```
id          Order ID hoặc Account ID (khớp chính xác)
name        tên người đặt / đứng tên (LIKE)
connType    D | B | T
dateFrom    yyyy-MM-dd   (ngày đăng ký hoặc cấp kết nối)
dateTo      yyyy-MM-dd
phone       số liên hệ khi đăng ký
status      trạng thái đơn / kết nối
page, size
```
Khách hàng bị giới hạn phạm vi về dữ liệu của chính mình. Truy vấn dùng `PreparedStatement` với điều kiện động.

Ví dụ phản hồi `/api/search/orders?name=Nguyen&connType=B`:
```json
{
  "success": true,
  "data": {
    "page": 1, "size": 20, "total": 1,
    "items": [
      {
        "orderId": "B0000000007",
        "customerName": "Nguyen Van A",
        "connType": "B",
        "planName": "Unlimited 64Kbps - Monthly",
        "orderDate": "2026-10-20",
        "status": "FEASIBLE",
        "phone": "0901234567"
      }
    ]
  }
}
```

### 4.11 JSON phụ trợ (`/nexus/api`) — dùng cho form động

| Method | URL | Mô tả | Tham số |
|---|---|---|---|
| GET | `/api/shops` | Danh sách cửa hàng theo thành phố (form `/my/order-request`) | `cityId` |
| GET | `/api/track/order` | Trạng thái đơn theo Order ID (cần đăng nhập, khách chỉ xem của mình) | `orderId` |
| GET | `/api/track/account` | Trạng thái kết nối, số nợ (cần đăng nhập, khách chỉ xem của mình) | `accountId` |
| GET | `/api/plans` | Gói cước theo loại | `type` |
| GET | `/api/discount` | Mức ưu đãi theo số kết nối | `qty` |
| GET | `/api/customers/lookup` | Gợi ý khách khi tạo đơn | `q` |

Ví dụ `/api/track/account?accountId=B001000000000001`:
```json
{
  "success": true,
  "data": {
    "accountId": "B001000000000001",
    "status": "ACTIVE",
    "customerName": "Nguyen Van A",
    "plan": "Unlimited 64Kbps - Monthly",
    "dueAmount": 0.00,
    "lastBillDate": "2026-10-01"
  }
}
```

### 4.12 Phản hồi (`/nexus/feedback`)

| Method | URL | Vai trò | Mô tả | SRS |
|---|---|---|---|---|
| GET | `/feedback/list` | ADMIN, TECH, ACCOUNTS, RETAIL | Danh sách phản hồi | FR-RPT-01 |
| POST | `/feedback/create` | RETAIL | Ghi nhận phản hồi của khách tại cửa hàng | FR-RET-08 |

(Khách hàng gửi qua `/my/feedback`.)

### 4.13 Báo cáo (`/nexus/reports`)

| Method | URL | Vai trò | Mô tả | SRS |
|---|---|---|---|---|
| GET | `/reports/orders` | ADMIN, TECH, RETAIL | Đơn theo trạng thái, khu vực, thời gian | FR-RPT-02 |
| GET | `/reports/connections` | ADMIN, TECH | Kết nối theo trạng thái và loại | FR-RPT-03 |
| GET | `/reports/stock` | ADMIN, TECH | Tồn kho, nhu cầu thiết bị | FR-RPT-04, FR-ADM-08 |
| GET | `/reports/billing` | ADMIN, ACCOUNTS | Hóa đơn, thanh toán, công nợ | FR-ACC-05 |

---

## 5. Cấu trúc mã nguồn gợi ý (Java)

```
src/main/java/com/nexus/
├─ filter/        AuthFilter, RoleFilter, CsrfFilter, EncodingFilter
├─ servlet/       AuthServlet, OrderServlet, TechServlet, BillingServlet,
│                 PaymentServlet, AdminXxxServlet, SearchServlet, ApiServlet...
├─ service/       OrderService, ConnectionService, BillingService,
│                 PaymentService, IdGenerator, DiscountService, FeasibilityService
├─ dao/           OrderDao, ConnectionDao, BillDao, PaymentDao, PlanDao...  (JDBC)
├─ model/         Order, Connection, Bill, Payment, Plan, Customer...
├─ util/          DbUtil (DataSource/connection pool), PasswordUtil, Validator
src/main/webapp/
├─ WEB-INF/views/ JSP theo module (jsp), WEB-INF/web.xml
├─ assets/        css, js, images
```

- Servlet chỉ nhận request, gọi Service và chọn JSP. **Không** đặt nghiệp vụ trong Servlet hay JSP.
- Service điều khiển transaction (`setAutoCommit(false)`, `commit`/`rollback`).
- DAO dùng `PreparedStatement`, đóng tài nguyên bằng try-with-resources.
- Mỗi lớp và hàm có comment giải thích logic theo chuẩn đề bài.

---

## 6. Yêu cầu phi chức năng áp dụng cho API

- **Bảo mật:** `PreparedStatement` cho mọi truy vấn, escape đầu ra JSP bằng `<c:out>`/JSTL, CSRF token, kiểm tra quyền sở hữu dữ liệu của khách ở tầng Service.
- **Toàn vẹn:** transaction cho tạo kết nối, ghi thanh toán, nhận hàng, sinh ID.
- **Kiểm toán:** ghi log người thực hiện và thời gian cho tạo đơn, đổi trạng thái, lập hóa đơn, thanh toán.
- **Đặt tên bảng:** dùng `Orders`, `Plans`, `Connections`, `Bills`... vì `ORDER` và `PLAN` là từ khóa của SQL Server.
- **Hiệu năng:** tra cứu theo ID ≤ 2 giây, tìm nâng cao ≤ 5 giây (đánh index theo `orderId`, `accountId`, `phone`, `orderDate`).

---

## 7. Phần cần xác nhận

Các endpoint phụ thuộc vào những điểm chưa rõ trong SRS phần 10:

1. Khách hàng có tự đặt đơn không: ảnh hưởng `/my/order-request`, `/order/requests/*` và `/order/create`.
2. Cách ghi nhận số phút gọi và số giờ dùng: ảnh hưởng `/billing/generate`.
3. Quy tắc tạm ngưng khi quá hạn thanh toán: ảnh hưởng `/billing/request-suspend` và `/tech/connections/suspend`.
4. Giá gói STD Yearly và giá vượt giờ của gói theo giờ: ảnh hưởng dữ liệu `/admin/plans`.
5. Thuế tính trên phần nào của hóa đơn, và cách hiểu "advance": ảnh hưởng công thức `/billing/generate`.
6. Đơn Dial-up kèm landline mới có thu cả hai khoản đặt cọc không: ảnh hưởng `/order/create`.
7. Số giờ của gói theo giờ tính trên cả chu kỳ hay mỗi tháng: ảnh hưởng `/billing/generate`.
8. Cách bù trừ nợ vào tiền đặt cọc khi hoàn cọc: ảnh hưởng `/payment/refund-deposit`.
