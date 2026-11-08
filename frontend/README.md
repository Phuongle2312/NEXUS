# NEXUS TELECOM - WEB FRONTEND (REACT + JAVASCRIPT)

Ứng dụng Web Frontend hiện đại cho hệ thống **Nexus Service Marketing System**, xây dựng bằng **React (JavaScript / JSX)** và **Node.js / Vite**.

## 1. Tiêu chuẩn thiết kế (UI Standards)
- **Công nghệ**: React 19 + JavaScript (ES6+, JSX) + Vite + Lucide Icons.
- **Chế độ màu**: **Nền sáng (Light Mode)** với bảng màu viễn thông hiện đại (Sky Blue `#0284c7`, Slate `#0f172a`, border `#e2e8f0`).
- **Border Radius**: Sử dụng biến root chuẩn **`4px`** (`:root { --radius: 4px; }`) đồng nhất cho mọi nút bấm, ô nhập liệu, thẻ card, bảng biểu, modal và badge.

## 2. Cấu trúc thư mục
```
frontend/
├── package.json          # Quản lý dependencies (React 19, Lucide, Vite)
├── vite.config.js        # Cấu hình Vite React Plugin
├── index.html            # File HTML gốc (font Plus Jakarta Sans)
├── dist/                 # Bản build production (tối ưu hóa sẵn)
└── src/
    ├── main.jsx          # Entry point React
    ├── App.jsx           # App Component & Quản lý State phân hệ vai trò
    ├── index.css         # Stylesheet: Biến root 8px radius, light mode, typography
    ├── data/
    │   └── mockData.js   # Dữ liệu chuẩn theo schema.sql & seed.sql
    ├── components/
    │   ├── TopNoticeBar.jsx  # Thanh chuyển đổi vai trò (Role Switcher kiểm thử)
    │   ├── Navbar.jsx        # Thanh điều hướng chính & Hotline
    │   └── Footer.jsx        # Chân trang bản quyền eProject
    └── views/
        ├── PublicView.jsx     # Bảng giá 3 dịch vụ, Slider chiết khấu, Tra cứu đơn/tài khoản
        ├── RetailView.jsx     # Quầy bán lẻ: Tiếp nhận đơn, tính chiết khấu tự động, thu tiền
        ├── TechnicalView.jsx  # Kỹ thuật: Khảo sát khả thi, cấp Account ID 16 số, đổi thiết bị
        ├── AccountsView.jsx   # Kế toán: Lập hóa đơn tự động tính thuế 12.24%, in biên lai
        ├── AdminView.jsx      # Quản trị: Nhân viên, Điểm giao dịch, Mã TP 3 số, Gói cước
        └── CustomerView.jsx   # Cổng khách hàng: Đường truyền, hóa đơn, gửi ticket hỗ trợ
```

## 3. Cách chạy ứng dụng trên máy

Trong thư mục `frontend`:
```powershell
# Chạy môi trường phát triển (Live reload)
npm run dev

# Hoặc xem bản production đã build
npm run preview
```
Ứng dụng sẽ chạy tại: **`http://localhost:5173`**
