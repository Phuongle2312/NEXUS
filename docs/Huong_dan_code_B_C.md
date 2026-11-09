# Hướng dẫn code backend — NEXUS (.NET 10)

Bản ngắn để cả nhóm code song song trên khung đã có. Backend chính thức là **ASP.NET Core Web API / .NET 10**
trong `backend/`; phần Java/JSP cũ vẫn giữ để tham chiếu nhưng không phát triển tiếp.

## 1. Chạy trên máy

```powershell
# 1. Tạo CSDL (một lần)
sqlcmd -S .\SQLEXPRESS -E -C -b -i database\schema.sql
sqlcmd -S .\SQLEXPRESS -E -C -b -f 65001 -i database\seed.sql

# 2. Cấu hình khoá JWT cho máy dev
cd backend\src\Nexus.Api
dotnet user-secrets set "Jwt:Key" "NexusLocalDevSigningKey-AtLeast-32-Characters"

# 3. Chạy API
cd ..\..\..
dotnet run --project backend\src\Nexus.Api
```

- API: <http://localhost:5080/api> · Health: <http://localhost:5080/api/health>
- Tài khoản demo: `admin`, `accounts`, `tech`, `retail`, `retail2`, `customer1`, `customer2` — mật khẩu `Nexus@2026`.
- Frontend: `cd frontend; npm install; npm run dev` → <http://localhost:5173> (Vite proxy `/api` sang 5080).

Nạp lại dữ liệu mẫu: chạy `database/reset_data.sql` rồi `database/seed.sql`.

## 2. Thêm một module

1. **Domain:** thêm/sửa entity trong `backend/src/Nexus.Api/Domain/`. Giữ tên thuộc tính khớp cột trong
   `database/schema.sql`; `NexusDbContext` map tường minh nên đổi tên cột phải sửa cả hai nơi.
2. **Service:** viết lớp trong `Services/`, nhận `NexusDbContext` và phụ thuộc khác qua constructor
   (`public class XxxService(NexusDbContext db)`). **Toàn bộ nghiệp vụ nằm ở Service**, controller không
   truy vấn trực tiếp nếu là luồng nghiệp vụ.
3. **Transaction:** thao tác nhiều bước dùng `await using var tx = await db.Database.BeginTransactionAsync(ct);`
   rồi `await tx.CommitAsync(ct)`. Xem `PaymentService.RecordAsync` làm mẫu.
4. **Controller:** kế thừa `ApiControllerBase`, trả qua `OkResponse(...)`. Kiểm tra quyền bằng
   `RequireRoles(Roles.Xxx)`; người dùng hiện tại đọc từ `Current` (claims JWT), **không** nhận `customerId`
   hay `employeeId` từ body.
5. **Lỗi nghiệp vụ:** `throw new BusinessException(ErrorCode.X, "thông báo")`. Middleware tự đổi sang mã HTTP
   và vỏ JSON lỗi; không tự `return BadRequest(...)` cho lỗi nghiệp vụ.
6. **Đăng ký DI:** thêm `builder.Services.AddScoped<XxxService>();` trong `Program.cs`.
7. **Test:** thêm test vào `backend/tests/Nexus.Tests/`. Công thức cước và quy tắc thuần nghiệp vụ phải test
   được **không cần CSDL** — tách phần tính toán ra khỏi phần truy vấn (xem `BillingCalculator`).

Chạy kiểm tra:

```powershell
dotnet build backend\Nexus.slnx
dotnet test  backend\Nexus.slnx
```

## 3. Danh mục Admin

Mọi thực thể quản trị dùng chung khuôn trong `AdminService` + `AdminController`:

`GET /api/admin/{res}` · `POST /api/admin/{res}` · `PUT /api/admin/{res}/{id}` ·
`POST /api/admin/{res}/{id}/deactivate`

Thêm thực thể mới = thêm `SaveXxxRequest` + 3–4 method trong `AdminService` + 4 action trong
`AdminController` + đăng ký hàm tương ứng ở `frontend/src/api/apiClient.js`.
Không xoá cứng: dùng `Status = INACTIVE` để giữ lịch sử.

## 4. Điểm nối giữa các module

| Người | Việc | Cần biết |
|---|---|---|
| Giỏi | Tạo đơn | `Orders.PlanFee`, `DepositAmount`, `LandlinePlanFee`, `LandlineDepositAmount` lưu **giá trước ưu đãi**; `DiscountPercent` lưu riêng, `BillingService` tự giảm sau |
| Giỏi | Sinh ID | `IdGenerator` dùng bảng `Sequences`, khoá `ORDER_D`, `ORDER_T`, `ORDER_B`, `ACCOUNT` (seed đang ở 2, 2, 4, 6). Đọc bằng `FromSqlInterpolated` + `UPDLOCK, ROWLOCK` **trong transaction đang chạy** |
| Giỏi | Giao diện | `frontend/src/views/`, `components/`, `index.css`. Gọi API qua `frontend/src/api/apiClient.js`, không gọi `fetch` rải rác |
| Cường | Tạo kết nối | Dial-up có `LandlinePlanId`: tạo kết nối `T` trước rồi `D`, gán `D.LandlineAccountId`. `ConnectedQty` chỉ đếm kết nối chính của đơn |
| Cường | Tạm ngưng | Đề nghị của kế toán nằm ở `Connections.SuspendRequested`; kỹ thuật xử lý xong thì đặt lại `false` |
| Cường | Thay thiết bị | Thêm dòng `ReplacementCharges` với `Status = 'UNBILLED'`; kỳ hóa đơn sau tự lấy và chuyển `BILLED` |

Quy ước schema sau khi chốt: chỉ thêm cột, không đổi tên.

## 5. Quy ước bắt buộc (đề bài)

- **Mỗi khối code có comment** giải thích logic.
- Truy vấn **tham số hoá**: dùng LINQ của EF Core; SQL thô phải qua `FromSqlInterpolated`, không nối chuỗi.
- Mật khẩu **băm BCrypt**, không log, không trả lại ngoài lần cấp tài khoản đầu tiên.
- Giá, ưu đãi, thuế 12,24%, tiền cọc, hạn thanh toán là **dữ liệu cấu hình** (`SystemConfig`,
  `BulkDiscounts`), không hard-code trong service.
- Không xoá cứng đơn/kết nối/hóa đơn đã phát sinh.
- Mọi thay đổi API phải cập nhật `API_Design_NEXUS.md` và `frontend/src/api/apiClient.js` cùng lúc.