# ĐỀ TÀI: NEXUS SERVICE MARKETING SYSTEM (eProject)

## 1. Giới thiệu dự án
**Nexus Communication System** là hệ thống cung cấp hai nhóm dịch vụ chính:
- **Dịch vụ Viễn thông & Điện thoại bàn (Landline)**
- **Dịch vụ Internet** bao gồm:
  - **Dial-up (D)**: Yêu cầu bắt buộc phải có đường dây điện thoại bàn (Landline) của chính nhà mạng Nexus.
  - **Broadband (B)**: Đường truyền băng thông rộng độc lập qua Modem/Router.

Do nhu cầu dịch vụ tăng cao trên khắp vùng lãnh thổ, Nexus mở rộng mạng lưới các **Cửa hàng bán lẻ (Retail Shops)** tại từng thành phố để khách hàng có thể trực tiếp đến đăng ký, thanh toán hoặc gửi khiếu nại thay vì phải lên trụ sở chính.

Dự án **Nexus Service Marketing System** tin học hóa toàn bộ quy trình giấy tờ thủ công trước đây thành hệ thống quản lý tập trung và tự động.

---

## 2. Các phân hệ và vai trò người dùng (Roles)

1. **Khách hàng (Customer)**
   - Xem thông tin cá nhân và gói dịch vụ đang sử dụng.
   - Theo dõi tiến độ đơn hàng qua **Order ID** (11 ký tự).
   - Tra cứu kết nối và trạng thái tài khoản qua **Account ID** (16 ký tự).
   - Xem chi tiết hóa đơn (Billing), lịch sử thanh toán.
   - Gửi phản hồi (Feedback) và khiếu nại dịch vụ.

2. **Nhân viên Cửa hàng (Retail Staff)**
   - Tiếp nhận thông tin khách hàng, cấp tài khoản cho khách.
   - **Tạo đơn hàng đăng ký dịch vụ (Orders)**: Duy nhất vai trò này được phép tạo đơn.
   - Tự động áp dụng chính sách chiết khấu số lượng (Bulk & Corporate schemes).
   - Thu tiền trực tiếp tại quầy (tiền mặt/thẻ) và ghi nhận thanh toán.
   - Theo dõi tiến độ đơn hàng và danh sách kết nối của khách.

3. **Bộ phận Kỹ thuật (Technical Staff)**
   - Nhận danh sách đơn đặt hàng từ các cửa hàng bán lẻ.
   - **Kiểm tra khả thi (Feasibility Check)**: Đo khoảng cách đường truyền, kiểm tra cổng/băng thông trạm máy chủ. Cập nhật trạng thái `Feasible` hoặc `Not Feasible`.
   - **Cấp phát kết nối**: Khi đơn khả thi, tiến hành lắp đặt và kích hoạt kết nối, sinh **Account ID** duy nhất.
   - Quản lý trạng thái kết nối: `Active`, `Temporarily Inactive` (tạm khóa), `Permanently Inactive` (hủy hẳn).
   - Xuất kho và thay thế thiết bị hỏng (Modem/Router), ghi nhận phụ phí thay thế (`Replacement Charges`).

4. **Bộ phận Kế toán (Accounts Department)**
   - **Tính cước và phát hành hóa đơn định kỳ (Billing)**: Duy nhất vai trò này được lập hóa đơn.
   - Tính toán thuế giá trị dịch vụ theo quy định chính phủ (**12.24%**).
   - Tính phí vượt hạn mức (gói giờ) và cước cuộc gọi thoại (Local, STD, Mobile messages).
   - Ghi nhận thanh toán tại văn phòng trung tâm và in phiếu thu/biên lai.
   - Theo dõi nợ cước quá hạn và gửi yêu cầu ngưng kết nối sang Kỹ thuật.

5. **Quản trị viên (Admin / Manager)**
   - Quản lý toàn bộ nhân viên (Employees) và phân quyền vai trò.
   - Quản lý mạng lưới cửa hàng bán lẻ (Retail Shops) và mã thành phố (City Codes).
   - Quản lý danh mục nhà cung cấp (Vendors), đơn mua thiết bị và kho thiết bị.
   - Quản lý danh mục gói cước, bảng giá và các tham số dịch vụ.

---

## 3. Quy chuẩn nghiệp vụ cốt lõi

### 3.1 Cấu trúc mã định danh
- **Order ID (11 ký tự):** `[D|T|B]` + 10 chữ số tuần tự.
  - Ví dụ: `D0000000001` (Đơn Dial-up), `B0000000001` (Đơn Broadband), `T0000000001` (Đơn Landline).
- **Account ID (16 ký tự):** `[D|B|T]` + Mã thành phố (3 số) + 12 chữ số tuần tự.
  - Ví dụ: `B001000000000001` (Broadband tại thành phố mã 001). Chỉ được cấp sau khi kỹ thuật kiểm tra khả thi và lắp đặt xong.

### 3.2 Quy trình xử lý đơn hàng
`Đăng ký (Submitted) -> Khảo sát khả thi (Feasibility Check) -> Khả thi (Feasible) -> Cấp kết nối & Account ID (Connection Provided)` (nếu không khả thi thì đóng đơn `Not Feasible`).
- Đơn Dial-up nếu khách chưa có điện thoại bàn Nexus: Phải kiểm tra khả thi cả Landline và Internet. Nếu đã có Landline Nexus: Chỉ kiểm tra Internet.

### 3.3 Tiền đặt cọc bảo đảm (Security Deposit)
Hoàn lại cho khách khi hủy dịch vụ:
- **Dial-up:** 325 $
- **Broadband:** 500 $
- **Landline:** 250 $

### 3.4 Chính sách chiết khấu mua số lượng lớn (Bulk & Corporate Discount)
Áp dụng giảm trừ trực tiếp trên phí ứng trước (Advance Plan Fee) và tiền đặt cọc (Deposit):
- 10 – 15 kết nối: Giảm **25%**
- 16 – 25 kết nối: Giảm **50%**
- 26 – 50 kết nối: Giảm **75%**
- Trên 50 kết nối: Giảm **100%** (miễn phí cọc và cước trả trước)

### 3.5 Công thức tính hóa đơn (Billing Formula)
- Phí chịu thuế = Cước gói chu kỳ + Cước cuộc gọi phát sinh / vượt dung lượng + Phí đổi thiết bị
- Thuế dịch vụ = **12.24%** × Phí chịu thuế
- Tổng hóa đơn kỳ đầu = Phí chịu thuế + Thuế + Tiền đặt cọc (sau giảm giá)
- Còn phải trả = Tổng hóa đơn − Số tiền đã thanh toán
- Nợ kỳ trước: Hiển thị riêng, không bị tính thuế đúp.

