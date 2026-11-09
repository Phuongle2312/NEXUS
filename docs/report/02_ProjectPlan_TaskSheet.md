# Project Plan và Task Sheet (cập nhật 09/11/2026, task 27)

Kế hoạch gốc: `NEXUS – Phân chia công việc.docx`. Dự án 30/10–28/11/2026 (mốc nhóm chốt, chờ mentor xác nhận).

> **Thay đổi kỹ thuật:** backend chuyển từ Java/JSP/Servlet/JDBC sang **ASP.NET Core Web API (.NET 10)**.
> Không dùng API gateway, không chia microservice. Frontend giữ **React 19 + Vite 6** và gọi API bằng JWT.
> Mã Java cũ vẫn trong repo để tham chiếu nhưng không phát triển tiếp.

## Mốc thời gian

| Ngày | Mốc | Trạng thái |
|---|---|---|
| 02/11 | Gửi email `DOUBT:` | **Trễ, chưa gửi** |
| 04/11 | ER và schema lên repo cho B, C | Xong, đã push nhánh `Phuonglt`, chưa merge vào `main` |
| 08/11 | Status 1 | Có bản nháp, chưa gửi |
| 09/11 | Chuyển backend sang .NET 10 | Xong: build sạch, 27 unit test pass |
| 15/11 | Xong task 9–14 | Task 9 xong; các module đã port sang .NET |
| 19/11 | Status 2 | Chưa |
| 22/11 | Xong task 16–22 | Task 20, 21 xong phần đặc tả, chờ task 17 để test nối thật |
| 25/11 | Chạy thử toàn luồng | Chưa — chưa có SQL Server trên máy dev |
| 26/11 | Rà bảo mật, đóng băng code | Đã rà phần xác thực/phân quyền của bản .NET |
| 28/11 | Nộp bài | Chưa |

## Task Sheet

### Hạ tầng và tài liệu

| # | Task | Trạng thái | Minh chứng |
|---|---|---|---|
| 1 | ER diagram | Xong | `docs/ER_Diagram.md` |
| 2 | schema.sql | Xong, đã chạy trên SQL Server 2022 | `database/schema.sql` |
| 3 | seed.sql | Xong | `database/seed.sql` |
| 4 | Khung backend .NET 10 (solution, DI, DbContext 25 bảng) | Xong | `backend/Nexus.slnx`, `Data/NexusDbContext.cs` |
| 4b | Bộ mã lỗi, vỏ JSON, middleware lỗi | Xong | `Common/` |
| 5 | Xác thực JWT, đăng xuất, đổi mật khẩu | Xong | `AuthService`, `AuthController` |
| 8 | Email `DOUBT:` | **Chờ Phương gửi** (cần điền Batch, Group) | `DOUBT_Email_NEXUS.md` |
| 9 | Admin employees, shops, cities | Xong | `AdminService`, `AdminController` |
| 20 | Billing | Xong, 2 ví dụ SRS có test | `BillingService`, `BillingCalculatorTests` |
| 21 | Payment, hoàn cọc | Xong | `PaymentService` |
| 26 | Rà bảo mật | Sơ bộ; **làm lại sau task 25** | Mục dưới |
| 27 | Tổng hợp tài liệu | Đang làm | `docs/report/` |
| 28 | Sao lưu, đóng gói ZIP, Feedback Form | Có script sao lưu; đóng gói làm tuần 4 | `database/backup.ps1` |

### Chuyển đổi sang .NET 10

| # | Task | Trạng thái | Minh chứng |
|---|---|---|---|
| N1 | Domain model + mapping EF Core lên schema có sẵn | Xong | `Domain/`, `Data/NexusDbContext.cs` |
| N2 | Port công thức cước, ưu đãi, sinh ID, cấu hình | Xong | `Services/BillingCalculator.cs` |
| N3 | Tạo đơn, khảo sát khả thi, cấp kết nối, đổi trạng thái, thay thiết bị | Xong | `Services/OrderService.cs` |
| N4 | Danh mục công khai + tra cứu công khai | Xong | `CatalogService`, `PortalService`, `PublicController` |
| N5 | Cổng khách hàng `/api/my/*`, yêu cầu đặt đơn, phản hồi | Xong | `PortalService`, `PortalController` |
| N6 | Quản trị danh mục `/api/admin/*` | Xong | `AdminService`, `AdminController` |
| N7 | Unit test xUnit | Xong, 27/27 pass | `backend/tests/Nexus.Tests` |
| N8 | Dọn code thừa: bỏ gateway, shared, dist, 2 view chết, mock data | Xong | Git |
| N9 | Web client React gọi API thật | Xong | `frontend/src/views/PublicView.jsx` |
| N10 | Docker: API + SQL Server + db-init + web client | Xong, chưa chạy thử | `docker-compose.yml`, `backend/Dockerfile` |
| N11 | Web admin (React) | **Chưa làm** | API quản trị đã sẵn |
| N12 | CI/CD Azure | **Chưa làm** | Kế hoạch trong `backend/README.md` |
| N13 | Chạy toàn luồng với SQL Server thật | **Chưa làm** (chưa có SQL Server/Docker) | – |

### Phần của Giỏi và Cường

Task 6, 7, 12–15, 22, 23 (Giỏi) và 10, 11, 16–19, 24 (Cường) đã có **service + API tương ứng** ở bản .NET,
nhưng **giao diện web admin chưa làm**. Phần còn lại là dựng màn hình React cho từng module.

## Rà bảo mật sơ bộ (task 26, bản .NET)

| Hạng mục | Kết quả | Cách kiểm tra |
|---|---|---|
| SQL Injection | EF Core LINQ; SQL thô dùng `FromSqlInterpolated` | Rà mã; thử chuỗi tấn công ở đăng nhập |
| XSS | React escape khi render; không dùng `dangerouslySetInnerHTML` | Thử `<script>` qua form phản hồi |
| CSRF | Không dùng cookie phiên; token JWT gửi qua header `Authorization` | Kiểm tra request không kèm cookie |
| Phân quyền | `[Authorize]` mặc định + `RequireRoles`, endpoint công khai đánh dấu `[AllowAnonymous]` | Thử HTTP từng vai trò |
| Sở hữu dữ liệu | Khách xem hóa đơn người khác trả 404 | Thử HTTP |
| Mật khẩu | BCrypt, cờ `MustChangePassword` bắt buộc đổi lần đầu | Kiểm tra CSDL và thử HTTP |
| Token | HS256, khoá ≥ 32 ký tự lấy từ secrets, hết hạn theo `Jwt:ExpiryMinutes` | Kiểm tra cấu hình; thử token hết hạn |
| Tra cứu công khai | Chỉ trả trạng thái, tên gói, thành phố, tổng nợ; không lộ tên/địa chỉ/thiết bị | Gọi `/api/public/track-account` |
| CORS | Chỉ domain khai báo trong `Cors:AllowedOrigins` | Thử origin lạ |

Chưa làm: giới hạn số lần đăng nhập sai (rate limit), thu hồi token khi đổi mật khẩu, HTTPS bắt buộc khi
triển khai thật (HSTS/redirect chỉ bật ở production).