# Project Plan và Task Sheet (cập nhật 04/11/2026, task 27)

Kế hoạch gốc: `NEXUS – Phân chia công việc.docx`. Dự án 30/10–28/11/2026 (mốc nhóm chốt, chờ mentor xác nhận).

## Mốc thời gian

| Ngày | Mốc | Trạng thái |
|---|---|---|
| 02/11 | Gửi email `DOUBT:` | **Trễ, chưa gửi** |
| 04/11 | ER và schema lên repo cho B, C | Xong, đã push nhánh `Phuonglt`, chưa merge vào `main` |
| 08/11 | Status 1 | Có bản nháp, chưa gửi |
| 15/11 | Xong task 9–14 | Task 9 xong |
| 19/11 | Status 2 | Chưa |
| 22/11 | Xong task 16–22 | Task 20, 21 xong phần đặc tả, chờ task 17 để test nối thật |
| 25/11 | Chạy thử toàn luồng | Chưa |
| 26/11 | Rà bảo mật, đóng băng code | Đã rà sơ bộ phần của Phương |
| 28/11 | Nộp bài | Chưa |

## Task Sheet (phần của Phương)

| # | Task | Trạng thái | Minh chứng |
|---|---|---|---|
| 1 | ER diagram | Xong | `docs/ER_Diagram.md` |
| 2 | schema.sql | Xong, đã chạy trên SQL Server 2022 | `database/schema.sql` |
| 3 | seed.sql | Xong | `database/seed.sql` |
| 4 | Maven, DbUtil, PasswordUtil, Validator | Xong | `pom.xml`, `util/` |
| 5 | 4 filter, đăng nhập, đăng xuất, đổi mật khẩu | Xong | `filter/`, `AuthServlet` |
| 8 | Email `DOUBT:` | **Chờ Phương gửi** (cần điền Batch, Group) | `DOUBT_Email_NEXUS.md` |
| 9 | Admin employees, shops, cities | Xong | `servlet/Admin*`, `service/` |
| 20 | Billing | Xong với kết nối mẫu trong seed | `BillingService`, `BillingCalculatorTest` |
| 21 | Payment, hoàn cọc | Xong với kết nối mẫu trong seed | `PaymentService`, `PaymentServiceTest` |
| 26 | Rà bảo mật | Sơ bộ; **làm lại sau task 25** | Mục dưới |
| 27 | Tổng hợp tài liệu | Đang làm | `docs/report/` |
| 28 | Sao lưu, đóng gói ZIP, Feedback Form | Có script sao lưu; đóng gói làm tuần 4 | `database/backup.ps1` |

Task của Giỏi (6, 7, 12–15, 22, 23) và Cường (10, 11, 16–19, 24) chưa bắt đầu trên repo.

## Rà bảo mật sơ bộ (task 26, chỉ phần của Phương)

| Hạng mục | Kết quả | Cách kiểm tra |
|---|---|---|
| SQL Injection | Mọi truy vấn qua PreparedStatement | Rà mã; thử chuỗi tấn công ở đăng nhập |
| XSS | Dữ liệu người dùng qua `<c:out>` | Thử `<script>` qua form thành phố, đầu ra đã escape |
| CSRF | Token cho mọi POST, cookie SameSite=Lax | POST thiếu token trả 403 |
| Phân quyền | Bảng quy tắc, mặc định từ chối | Unit test và thử HTTP từng vai trò |
| Sở hữu dữ liệu | Khách xem hóa đơn người khác trả 404 | Thử HTTP |
| Mật khẩu | BCrypt, đổi mật khẩu tạm bắt buộc | Kiểm tra CSDL và thử HTTP |
| Phiên | Đổi session id khi đăng nhập, HttpOnly, 30 phút; tài khoản bị ngưng mất phiên ngay | Thử HTTP |

Chưa làm: giới hạn số lần đăng nhập sai; cờ `Secure` cho cookie (cần HTTPS khi triển khai thật).
