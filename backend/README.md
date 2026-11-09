# NEXUS API — ASP.NET Core Web API (.NET 10)

Backend monolith của **NEXUS Service Marketing System**, thay thế bản Java/JSP/Servlet/JDBC.
Không API gateway, không microservice: một tiến trình ASP.NET Core phục vụ toàn bộ `/api/*`.

| Hạng mục | Nội dung |
|---|---|
| Runtime | .NET 10 (`net10.0`) |
| Web | ASP.NET Core Web API + Controller |
| ORM | EF Core 10 + SQL Server provider |
| Xác thực | JWT Bearer (thay session/Tomcat), BCrypt cho mật khẩu |
| CSDL | SQL Server, tạo bằng `database/schema.sql` + `database/seed.sql` |
| Kiểm thử | xUnit — `backend/tests/Nexus.Tests` |

## 1. Yêu cầu

- .NET SDK 10.0 trở lên (`dotnet --version` phải ra `10.x`).
- SQL Server 2019/2022 hoặc Docker.
- Node.js 20+ chỉ khi chạy frontend.

## 2. Tạo cơ sở dữ liệu

```sql
-- Trong SQL Server Management Studio hoặc sqlcmd
CREATE DATABASE NEXUS;
GO
-- Chạy lần lượt, chọn database NEXUS:
--   database/schema.sql
--   database/seed.sql
```

Schema là nguồn chuẩn 25 bảng. Ứng dụng **không** tự tạo DB và không dùng migration.

## 3. Cấu hình

Connection string và khoá JWT đọc từ `appsettings.Development.json`, biến môi trường hoặc user-secrets.
Không commit khoá thật vào `appsettings.json`.

```powershell
cd backend/src/Nexus.Api

# Khoá JWT tối thiểu 32 ký tự
dotnet user-secrets set "Jwt:Key" "NexusLocalDevSigningKey-AtLeast-32-Characters"

# Đổi chuỗi kết nối nếu SQL Server không phải .\SQLEXPRESS
dotnet user-secrets set "ConnectionStrings:Nexus" "Server=.\SQLEXPRESS;Database=NEXUS;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Hoặc dùng biến môi trường (tiện cho CI/CD):

```powershell
$env:ConnectionStrings__Nexus = "Server=localhost,1433;Database=NEXUS;User Id=sa;Password=...;TrustServerCertificate=True"
$env:Jwt__Key = "NexusLocalDevSigningKey-AtLeast-32-Characters"
```

## 4. Chạy

```powershell
# Từ thư mục gốc repo
dotnet run --project backend/src/Nexus.Api

# Hoặc build/test toàn solution
dotnet build backend/Nexus.slnx
dotnet test  backend/Nexus.slnx
```

- API: <http://localhost:5080/api>
- Kiểm tra sống + kết nối DB: <http://localhost:5080/api/health>
- OpenAPI (Development): <http://localhost:5080/openapi/v1.json>

Tài khoản mẫu (mật khẩu `Nexus@2026`): `admin`, `retail`, `tech`, `accounts`, `customer1`.

## 5. Cấu trúc mã nguồn

```
backend/
├─ Nexus.slnx                 # solution (.NET 10)
├─ Dockerfile                 # multi-stage build, image aspnet:10.0
├─ src/Nexus.Api/
│  ├─ Program.cs              # DI, JWT, CORS, middleware
│  ├─ Common/                 # ErrorCode, BusinessException, ApiResponse
│  ├─ Domain/                 # entity ánh xạ 25 bảng
│  ├─ Data/NexusDbContext.cs  # mapping EF Core
│  ├─ Services/               # nghiệp vụ: đơn, cước, thanh toán, danh mục...
│  └─ Controllers/            # /api/auth, /api/public, /api/orders, ...
└─ tests/Nexus.Tests/         # unit test công thức cước, quy tắc nghiệp vụ
```

## 6. Quy ước phản hồi

Mọi endpoint trả cùng một vỏ JSON:

```jsonc
// Thành công
{ "success": true, "data": { }, "message": "OK" }

// Lỗi nghiệp vụ
{ "success": false, "error": { "code": "ORDER_NOT_FOUND", "message": "Không tìm thấy đơn ..." } }
```

Mã lỗi giữ nguyên bộ cũ trong `Common/ErrorCode.cs` và được middleware đổi sang HTTP status phù hợp
(400/401/403/404/409). Chi tiết endpoint: `API_Design_NEXUS.md`.

## 7. Docker

Từ thư mục gốc repo:

```powershell
docker compose up --build
```

- Web client (Nginx): <http://localhost:3000>
- API: <http://localhost:5080/api>
- SQL Server: `localhost:1433`, user `sa`

Service `db-init` chạy `schema.sql` + `seed.sql` một lần sau khi SQL Server healthy.

## 8. Triển khai Azure (CI/CD)

Gợi ý cấu hình tối thiểu:

1. **Azure SQL Database** — chạy `database/schema.sql` và `seed.sql`.
2. **Azure App Service (Linux)** hoặc **Container Apps** — deploy image từ `backend/Dockerfile`.
3. Đặt cấu hình trong App Service → Configuration:
   - `ConnectionStrings__Nexus`
   - `Jwt__Key` (≥ 32 ký tự)
   - `Cors__AllowedOrigins__0` = domain web client
4. **Azure Static Web Apps** hoặc App Service thứ hai cho `frontend/` (build `npm run build`).
5. GitHub Actions: `dotnet publish` cho API, `npm ci && npm run build` cho frontend; lưu bí mật ở
   repository secrets và dùng OIDC/federated credential thay vì mật khẩu.