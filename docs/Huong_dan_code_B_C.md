# Hợp đồng code cho Giỏi và Cường

Bản ngắn để hai bạn code song song trên khung đã có.

## Chạy trên máy

```bash
sqlcmd -S .\SQLEXPRESS -E -C -b -i database\schema.sql
sqlcmd -S .\SQLEXPRESS -E -C -b -f 65001 -i database\seed.sql
mvn test-compile dependency:build-classpath -Dmdep.outputFile=target/cp.txt -Dmdep.includeScope=test
mvn dependency:copy -Dartifact=com.microsoft.sqlserver:mssql-jdbc_auth:12.10.2.x64:dll -DoutputDirectory=target/native
```

Chạy `com.nexus.DevServer` với `-Djava.library.path=target/native`, mở <http://localhost:8081/nexus>. Không cần cài Tomcat. Tài khoản demo: `admin`, `accounts`, `tech`, `retail`, `retail2`, `customer1`, `customer2`, mật khẩu `Nexus@2026`.

Máy không dùng Windows Authentication: tạo `src/main/resources/db.local.properties` (không commit), mẫu trong `db.properties`. Nạp lại dữ liệu mẫu: chạy `database/reset_data.sql` rồi `seed.sql`.

## Thêm một module

1. **DAO:** interface `XxxDao` + bản `JdbcXxxDao`. Dùng `Sql.list/one/update/insert`, nhận `Connection` từ Service.
2. **Service:** nhận `TxRunner` và DAO interface qua constructor; không `new` lớp Jdbc*. Mọi thao tác nhiều bước nằm trong `tx.inTransaction(c -> ...)`.
3. **Lắp ráp:** khai báo DAO và Service trong `AppContext`. Đây là nơi duy nhất biết lớp Jdbc*.
4. **Servlet:** kế thừa `BaseServlet`, đăng ký `get("/list", this::list)`; chỉ đọc request, gọi Service, chọn JSP.
5. **Phân quyền:** URL đã khai báo trong `AccessPolicy`, URL mới chưa khai báo sẽ bị 403. Dữ liệu của khách: kiểm tra sở hữu ở Service, xem `BillingService.view`.
6. **JSP:** `<%@ include file="/WEB-INF/views/layout/header.jspf" %>`, mọi dữ liệu người dùng qua `<c:out>`, mọi form POST có `<t:csrf/>`.
7. **Lỗi nghiệp vụ:** `throw new BusinessException(ErrorCode.X, "thông báo")`; BaseServlet đổi thành mã HTTP và trang lỗi, DbUtil tự rollback.

Không kiểm tra được bằng test nếu không có CSDL là dấu hiệu Service đang phụ thuộc sai: xem `PaymentServiceTest` làm mẫu test với DAO giả.

## Danh mục Admin (task 10, 11 của Cường)

Làm theo mẫu `cities`: model có getter và setter (EL trong JSP không đọc được record) → `XxxDao extends CrudDao<Xxx>` → `XxxService extends AbstractCrudService<Xxx>` → `AdminXxxServlet extends BaseAdminServlet<Xxx>` → `admin/xxx-list.jsp`, `admin/xxx-form.jsp`.

## Điểm nối giữa các module

| Người | Việc | Cần biết |
|---|---|---|
| Giỏi | Tạo đơn (task 15) | `Orders.PlanFee`, `DepositAmount`, `LandlinePlanFee`, `LandlineDepositAmount` lưu **giá trước ưu đãi**; `DiscountPercent` lưu riêng. Billing tự tính sau giảm |
| Giỏi | IdGenerator (task 7) | Bảng `Sequences`, khóa `ORDER_D`, `ORDER_T`, `ORDER_B`, `ACCOUNT`; seed đang ở 2, 2, 4, 6. Phải dùng `Connection` của transaction đang chạy |
| Giỏi | Layout (task 6) | Thay `layout/header.jspf`, `footer.jspf`, `assets/css/app.css`; giữ các biến `ctx`, `role`. Khi có `/public/home`, đổi `AFTER_LOGOUT` trong `AuthServlet` và `index.jsp` |
| Cường | Tạo kết nối (task 17) | Dial-up có `LandlinePlanId`: tạo `T` trước rồi `D`, gán `D.LandlineAccountId`. `ConnectedQty` chỉ đếm kết nối chính của đơn |
| Cường | Tạm ngưng (task 18) | Đề nghị của kế toán nằm ở `Connections.SuspendRequested`; thực hiện xong thì đặt lại 0 |
| Cường | Thay thiết bị (task 19) | Thêm dòng `ReplacementCharges` với `Status = 'UNBILLED'`; Billing tự lấy ở kỳ sau |

Quy ước schema sau khi chốt: chỉ thêm cột, không đổi tên.
