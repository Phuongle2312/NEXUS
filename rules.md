# DESIGN SYSTEM & UI RULES (NEXUS SYSTEM)

## 1. Nguyên tắc giao diện chung (UI Guidelines)
- **Chế độ màu chủ đạo**: **Light Mode (Nền sáng)** chuyên nghiệp, thanh lịch, phong cách viễn thông hiện đại.
- **Border Radius**: Sử dụng biến root chuẩn **`4px`** cho toàn bộ hệ thống (nút bấm, ô nhập liệu, thẻ card, bảng biểu, modal, popup).
  ```css
  :root {
    --radius-root: 4px;
    --radius-sm: 2px;
    --radius: 4px;
    --radius-md: 4px;
    --radius-lg: 4px;
    --radius-xl: 4px;
  }
  ```

## 2. Bảng mã màu chuẩn (Color Palette)
- **Background chính**: `#f8fafc` (Slate 50) hoặc `#f1f5f9` (Slate 100)
- **Card / Surface**: `#ffffff` (White thuần)
- **Primary (Chủ đạo)**: `#0284c7` (Sky 600) / `#0369a1` (Sky 700) - Tượng trưng cho công nghệ viễn thông và kết nối mạng
- **Secondary**: `#0f172a` (Slate 900)
- **Text Chính**: `#0f172a` (Slate 900)
- **Text Phụ / Mờ**: `#64748b` (Slate 500)
- **Border / Viền**: `#e2e8f0` (Slate 200)
- **Success (Thành công / Active / Feasible)**: `#16a34a` (Green 600)
- **Warning (Cảnh báo / Tạm ngưng / Chờ duyệt)**: `#ea580c` (Orange 600)
- **Danger (Lỗi / Quá hạn / Not Feasible / Inactive)**: `#dc2626` (Red 600)

## 3. Quy chuẩn thành phần giao diện (Component Specifications)
- **Button (Nút)**:
  - Chiều cao chuẩn: 40px (md), 32px (sm), 48px (lg).
  - Bo góc: `8px`.
  - Hiệu ứng hover & active mượt mà, bóng đổ nhẹ (`box-shadow: 0 1px 2px rgba(0,0,0,0.05)`).
- **Card (Thẻ nội dung)**:
  - Nền trắng `#ffffff`, viền `1px solid #e2e8f0`, bo góc `8px`, đệm trong `p-5` hoặc `p-6`.
- **Input & Select**:
  - Viền `1px solid #cbd5e1`, bo góc `8px`, nền trắng, focus viền `2px solid #0284c7`.
- **Table (Bảng dữ liệu)**:
  - Header nền `#f8fafc`, text in đậm vừa, phân cách bởi viền ngang mỏng.
  - Hàng chẵn/lẻ hoặc hover đổi màu nhẹ `#f8fafc`.
- **Status Badge (Huy hiệu trạng thái)**:
  - Bo góc `6px` hoặc `8px`, chữ in hoa nhẹ, màu nền nhạt kèm chữ đậm màu tương ứng.

## 4. Bố cục không gian làm việc (App Layout)
- **Top Navigation Bar**: Logo NEXUS, hotline hỗ trợ, thanh tìm kiếm nhanh, nút đổi vai trò (Role Switcher tiện kiểm thử demo) và thông tin tài khoản.
- **Role Switcher**: Cho phép dễ dàng chuyển đổi nhanh giữa 5 đối tượng:
  1. `PUBLIC`: Khách vãng lai xem gói cước, tra cứu đơn/kết nối.
  2. `RETAIL`: Nhân viên quầy tạo đơn và thu tiền.
  3. `TECHNICAL`: Kỹ thuật viên đo kiểm và cấp nối mạng.
  4. `ACCOUNTS`: Kế toán lập hóa đơn và quản lý thu chi.
  5. `ADMIN`: Quản trị viên hệ thống.
  6. `CUSTOMER`: Cổng thông tin khách hàng.
- **Sidebar**: Danh mục chức năng chuyên biệt theo vai trò hiện tại.
- **Main Content**: Không gian làm việc rộng rãi, thoáng đãng, phân chia lưới (Grid/Flex) rõ ràng.

