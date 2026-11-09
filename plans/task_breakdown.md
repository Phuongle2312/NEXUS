# KẾ HOẠCH TRIỂN KHAI CHI TIẾT (TASK BREAKDOWN)

> Cập nhật 09/11/2026: backend chuyển sang **.NET 10 (ASP.NET Core Web API)**, bỏ API gateway,
> kiến trúc **monolith**, không microservice. Frontend giữ **React 19 + Vite 6**.
> Mã Java/JSP cũ vẫn trong repo để tham chiếu nhưng không phát triển tiếp.

## Giai đoạn 1: Kiến trúc & Giao diện web client

- [x] Tạo cấu trúc thư mục chuẩn (`backend/`, `frontend/`, `database/`, `docs/`, `plans/`, `srsdetails/`)
- [x] Thiết lập quy ước giao diện: nền sáng (light mode), border-radius = 8px
- [x] Cổng thông tin công khai: bảng giá gói cước, công cụ tính chiết khấu số lượng lớn,
      tra cứu Order ID / Account ID, form phản hồi
- [x] Nối dữ liệu thật từ API .NET: gói cước, cửa hàng, bậc ưu đãi, tra cứu đơn/kết nối
- [x] Xử lý trạng thái tải/lỗi, thông báo khi API chưa chạy hoặc cần đăng nhập
- [x] Dọn giao diện chết: xoá `CustomerRegistrationView.jsx`, `CustomerPortalView.jsx`, `mockData.js`

## Giai đoạn 2: Backend .NET 10

- [x] Solution `backend/Nexus.slnx` (`net10.0`) + project test xUnit
- [x] Domain model + EF Core map lên schema SQL Server có sẵn (25 bảng, không migration)
- [x] Bộ mã lỗi nghiệp vụ, vỏ JSON thống nhất, middleware xử lý lỗi
- [x] Xác thực JWT + BCrypt: đăng nhập, đăng xuất, đổi mật khẩu, cờ buộc đổi mật khẩu
- [x] Module khách hàng, đơn hàng, kỹ thuật, hóa đơn, thanh toán, danh mục, quản trị, cổng khách hàng
- [x] Công thức cước, ưu đãi, thuế 12,24%, hoàn cọc — có unit test (27/27 pass)
- [x] Endpoint `/api/health` để kiểm tra sống và kết nối CSDL

## Giai đoạn 3: Giao diện quản trị (web admin)

- [ ] Khung đăng nhập + điều hướng theo vai trò (ADMIN/RETAIL/TECH/ACCOUNTS)
- [ ] Retail: tạo đơn có chiết khấu tự động, quản lý khách hàng, thu cước
- [ ] Technical: khảo sát khả thi, cấp kết nối & sinh Account ID, thay thiết bị
- [ ] Accounts: lập hóa đơn, in biên lai, hoàn cọc
- [ ] Admin: danh mục nhân viên, cửa hàng, thành phố, thiết bị, gói cước
- [ ] Customer portal: hợp đồng, cước, phản hồi (API `/api/my/*` đã sẵn)

## Giai đoạn 4: Kiểm thử & triển khai

- [ ] Chạy toàn luồng với SQL Server thật (hiện chưa có SQL Server/Docker trên máy dev)
- [ ] Kiểm thử end-to-end: đăng ký → khảo sát → cấp kết nối → lập hóa đơn → thanh toán → hoàn cọc
- [ ] Docker: `docker compose up --build` (SQL Server + db-init + API + web client)
- [ ] CI/CD GitHub Actions → Azure App Service (API) + Azure Static Web Apps (web client)
- [ ] Rà bảo mật lần cuối, đóng băng code, đóng gói bài nộp