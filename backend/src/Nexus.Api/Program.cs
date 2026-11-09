using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexus.Api.Common;
using Nexus.Api.Data;
using Nexus.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Cấu hình: connection string SQL Server và khoá JWT lấy từ appsettings/env.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Nexus")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Nexus trong appsettings.");

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key phải có ít nhất 32 ký tự (đặt trong user-secrets hoặc biến môi trường).");
}

builder.Services.AddSingleton(jwtOptions);

// ---------------------------------------------------------------------------
// EF Core: database do database/schema.sql tạo, ứng dụng chỉ đọc/ghi.
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<NexusDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(3);
        sql.CommandTimeout(30);
    }));

// ---------------------------------------------------------------------------
// Nghiệp vụ. Đăng ký theo interface ngầm định qua lớp cụ thể; mọi phụ thuộc
// được inject qua constructor để test thay thế được.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<IdGenerator>();
builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<PortalService>();

// ---------------------------------------------------------------------------
// JWT Bearer: token chứa userId, role, employeeId/customerId.
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Frontend Vite chạy khác cổng khi dev, cần CORS cho các origin được khai báo.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware: đổi BusinessException thành JSON lỗi thống nhất.
// ---------------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Kiểm tra nhanh khi deploy: ứng dụng sống và kết nối được CSDL.
// Trả 503 khi mất CSDL để Azure/container probe coi là không khoẻ.
app.MapGet("/api/health", async (NexusDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);

    return canConnect
        ? Results.Ok(ApiResponse<object>.Ok(new { status = "healthy", database = "up" }))
        : Results.Text(ApiJson.Error(ErrorCode.SYSTEM_ERROR, "Không kết nối được CSDL."),
            "application/json; charset=utf-8", statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.Run();