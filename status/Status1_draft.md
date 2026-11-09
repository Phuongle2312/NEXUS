**Tiêu đề:** `STATUS: NEXUS SERVICE MARKETING SYSTEM - Batch (điền) - Group (điền) - Status 1`

> Bản nháp, hạn gửi 08/11/2026. Trước khi gửi: cập nhật theo tiến độ thực tế của Giỏi và Cường, điền `eProject_Status_Report(V1).xls`, kèm ảnh chụp, nén ZIP, gửi qua Query/Status.
>
> **Thay đổi kỹ thuật so với kế hoạch ban đầu:** backend chuyển từ Java/JSP/Servlet/JDBC sang
> **ASP.NET Core Web API (.NET 10)** theo yêu cầu môn học. Không dùng API gateway, kiến trúc monolith,
> không microservice. Frontend giữ React 19 + Vite 6, xác thực bằng JWT.

Dear eProjects Team,

**Overall progress.** The database design, the .NET 10 API backend, the public web client, and the
administration, order, billing and payment services are implemented and building cleanly on our
development machines. The admin UI and the end-to-end run against a live SQL Server are still in progress.

**Completed**

- Entity-relationship design and SQL Server database scripts (25 tables, sample data).
- Backend rewritten as an **ASP.NET Core Web API on .NET 10** (monolith, no API gateway, no microservices)
  using EF Core mapped onto the existing schema — no migrations, the schema script stays the source of truth.
- JWT authentication with BCrypt password hashing, role-based authorization for the five roles, and a
  forced password change for temporary accounts.
- Services for customers, orders, technical feasibility and connections, billing, payments,
  catalogue/administration, and the customer portal (`/api/my/*`), plus public tracking endpoints.
- Bill calculation, discounts, 12.24% tax and deposit refund with **27 passing unit tests**
  (including both worked examples from the SRS: $752.54 and $564.41).
- Public web client (React) now reads live API data: plan catalogue, shops, discount tiers,
  Order ID / Account ID tracking, feedback form with clear login/error states.
- Removed unused code: API gateway folder, shared folder, built assets, two dead views and mock data.
- Docker setup: SQL Server + one-shot schema/seed init + .NET API image + Nginx-served web client.

**In progress**

- (Điền theo tiến độ của Giỏi: giao diện retail — tạo đơn, khách hàng, thu cước.)
- (Điền theo tiến độ của Cường: giao diện admin/technical — danh mục, khảo sát, cấp kết nối, thiết bị.)
- Admin web UI and the end-to-end run against a live SQL Server instance.
- Azure CI/CD pipeline (GitHub Actions → App Service + Static Web Apps).

**Challenges**

- Several points in the specification are unclear. We sent them in a `DOUBT:` email and are working with stated assumptions. (Sửa lại nếu mentor đã trả lời.)
- No SQL Server/Docker available on the development machine yet, so integration testing is limited to
  unit tests and endpoint-level checks; the full flow still has to run against a real database.

**Attachments:** source code (`backend/`, `frontend/`), `database/schema.sql`, `database/seed.sql`, screenshots.

Le Tri Phuong (Team Leader)