# PHẦN MỀM QUẢN TRỊ DỊCH VỤ VIỄN THÔNG & INTERNET (NEXUS SERVICE MARKETING SYSTEM)
## SOFTWARE REQUIREMENT SPECIFICATION (SRS)

### 1. Phạm vi hệ thống
Hệ thống tin học hóa quản trị kinh doanh dịch vụ viễn thông cho Nexus Communication System, hỗ trợ:
- Mạng lưới cửa hàng bán lẻ (Retail Outlets).
- Khách hàng cá nhân và doanh nghiệp đăng ký số lượng lớn.
- Bộ phận kỹ thuật khảo sát hạ tầng, thi công đấu nối.
- Bộ phận kế toán lập hóa đơn và quản lý công nợ.
- Ban quản trị theo dõi nhân sự, chi nhánh và kho thiết bị.

---

### 2. Danh mục chức năng nghiệp vụ (Functional Requirements)

| Mã | Tên chức năng | Phân hệ / Vai trò | Mô tả tóm tắt |
|---|---|---|---|
| **FR-01** | Cổng thông tin công khai (Public Portal) | Khách vãng lai | Giới thiệu các gói cước Dial-up, Broadband, Landline; Bảng so sánh; Form tra cứu đơn hàng & kết nối; Gửi liên hệ. |
| **FR-02** | Xác thực & Phân quyền (Auth & RBAC) | Toàn bộ | Đăng nhập an toàn, phân chia 5 vai trò (Admin, Retail, Technical, Accounts, Customer), đổi mật khẩu tạm thời. |
| **FR-03** | Quản lý khách hàng (Customer Management) | Retail Staff | Thêm mới, chỉnh sửa hồ sơ khách hàng, cấp tài khoản đăng nhập portal. |
| **FR-04** | Tiếp nhận & Tạo đơn hàng (Order Creation) | Retail Staff | Tạo đơn đăng ký kết nối mới, tự động tính chiết khấu số lượng (25% - 100%), sinh Order ID `[D|B|T]0000000001`. |
| **FR-05** | Khảo sát tính khả thi (Feasibility Check) | Technical Staff | Đo khoảng cách đường truyền, kiểm tra cổng tủ cáp, cập nhật `Feasible` hoặc `Not Feasible`. |
| **FR-06** | Cấp phát kết nối (Connection Provisioning) | Technical Staff | Đấu nối mạng, cấp phát thiết bị, sinh Account ID chuẩn 16 số `[D|B|T] + CityCode + 12 số`. |
| **FR-07** | Quản lý trạng thái kết nối | Technical Staff | Chuyển đổi trạng thái: `Active`, `Temporarily Inactive`, `Permanently Inactive`. |
| **FR-08** | Cấp phát & Thay thế thiết bị | Technical Staff | Xuất kho Modem/Router, ghi nhận hỏng hóc và tạo phụ phí `ReplacementCharges`. |
| **FR-09** | Tính cước & Lập hóa đơn (Billing) | Accounts | Tự động tính cước gói, cước phát sinh vượt hạn mức/cuộc gọi, thuế 12.24%, chiết khấu và cọc. |
| **FR-10** | Quản lý thanh toán & Phiếu thu (Payment) | Retail & Accounts | Ghi nhận thanh toán tại quầy hoặc trụ sở, sinh biên lai, bù trừ công nợ. |
| **FR-11** | Cổng khách hàng (Customer Portal) | Customer | Xem thông tin hợp đồng, tra cứu hóa đơn, theo dõi tiến độ đơn hàng và gửi khiếu nại. |
| **FR-12** | Quản trị hệ thống (Admin Master Data) | Admin | Quản lý nhân viên, cửa hàng, mã thành phố, nhà cung cấp, gói cước và thiết bị kho. |

---

### 3. Quy chuẩn gói cước & tính phí (Pricing Matrix)

1. **Tiền đặt cọc (Refundable Security Deposit):**
   - Dial-up: \$325
   - Broadband: \$500
   - Landline: \$250
2. **Chiết khấu khách hàng doanh nghiệp / Số lượng lớn:**
   - 10 - 15 đường: Giảm 25% (phí ứng trước & tiền cọc)
   - 16 - 25 đường: Giảm 50%
   - 26 - 50 đường: Giảm 75%
   - Trên 50 đường: Giảm 100%
3. **Thuế dịch vụ viễn thông:** Cố định 12.24% trên tổng cước phát sinh chịu thuế.

