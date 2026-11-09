# NEXUS Service Marketing System

eProject (Aptech): ứng dụng web quản lý dịch vụ viễn thông và Internet cho **Nexus Communication System**, thay thế quy trình giấy tờ bằng hệ thống tự động hóa đơn hàng, kết nối, hóa đơn và thanh toán.

> Tài liệu đặc tả chi tiết: `SRS_NEXUS_Service_Marketing_System.docx`. Phân công và mốc thời gian: `NEXUS – Phân chia công việc.docx`

| Mục | Nội dung |
|---|---|
| Batch / Group | _(điền)_ |
| Thành viên | Lê Trí Phương (trưởng nhóm), Lữ Văn Giỏi, Nguyễn Trần Xuân Cường |
| Công nghệ | **.NET 10** — ASP.NET Core Web API + EF Core, **React 19 + Vite 6**, CSDL **SQL Server 2019+** (thiết kế URL/API: `API_Design_NEXUS.md`) |
| Kiến trúc | **Monolith** một tiến trình API, không API gateway, không microservice |
| Ngày bắt đầu / kết thúc | 30/Oct/2026 / 28/Nov/2026 _(nhóm chốt 30 ngày, chờ mentor xác nhận)_ |

---

## 1. Tóm tắt bài toán

Nexus cung cấp 3 loại kết nối: **Dial-up (D)**, **Broadband (B)** và **điện thoại bàn / Landline (T)**, qua mạng lưới cửa hàng bán lẻ. Hệ thống cần:

- Tạo đơn đăng ký, kiểm tra khả thi, cấp kết nối.
- Quản lý khách hàng, nhân viên, cửa hàng, nhà cung cấp, thiết bị/kho.
- Quản lý gói cước, tính cước, lập hóa đơn, ghi nhận thanh toán (chỉ trả sau).
- Tra cứu trạng thái đơn/kết nối (cơ bản và nâng cao).
- Thu thập phản hồi khách hàng.

## 2. Vai trò và quyền

| Vai trò | Được làm |
|---|---|
| **Admin (Manager)** | CRUD nhân viên, cửa hàng, nhà cung cấp, đơn mua, danh mục thiết bị, gói cước, mã thành phố |
| **Retail Staff** | Tạo/cập nhật khách hàng, cấp tài khoản cho khách, **tạo đơn** (chỉ vai trò này), theo dõi đơn/kết nối/hóa đơn, ghi nhận thanh toán tại cửa hàng |
| **Technical** | Cập nhật trạng thái đơn (khả thi hay không), **tạo kết nối** (cấp Account ID), ngưng tạm thời/vĩnh viễn, cấp phát và thay thế thiết bị |
| **Accounts** | **Tính cước, lập và cập nhật hóa đơn** (chỉ vai trò này), ghi nhận thanh toán tại văn phòng, đề nghị tạm ngưng khi quá hạn |
| **Customer** | Xem thông tin của mình, theo dõi đơn (Order ID), xem kết nối (Account ID), xem hóa đơn, gửi phản hồi |

## 3. Quy tắc nghiệp vụ cốt lõi

### Mã định danh
- **Order ID (11 ký tự):** `[D|T|B]` + 10 chữ số tuần tự. Ví dụ `D0000000001`.
- **Account ID (16 ký tự):** `[D|B|T]` + mã thành phố 3 chữ số + 12 chữ số tuần tự. Ví dụ `B001000000000001`. Chỉ cấp sau khi kết nối được lắp đặt.

### Quy trình đơn
`Submitted → Feasibility check → Feasible → Connection provided` (hoặc `Not feasible` thì đóng đơn).

- Dial-up: nếu khách đăng ký cả landline và internet thì kiểm tra khả thi cả hai. Nếu đã có landline Nexus thì chỉ kiểm tra internet.
- Dial-up bắt buộc có landline Nexus. Đăng ký mới cả hai thì một đơn D tạo hai kết nối (T và D).
- Một đơn có thể gồm nhiều kết nối cùng loại, cùng gói; mỗi kết nối một Account ID.

### Trạng thái kết nối
`Active` ⇄ `Temporarily inactive` → `Permanently inactive`

### Ưu đãi số lượng lớn (áp dụng cho phí ứng trước và tiền đặt cọc)

| Số kết nối | Giảm |
|---|---|
| 10–15 | 25% |
| 16–25 | 50% |
| 26–50 | 75% |
| > 50 | 100% |

### Tiền đặt cọc (hoàn lại khi hủy)
Dial-up 325$ · Broadband 500$ · Landline 250$

### Hóa đơn
```
Phần chịu thuế = phí gói (kỳ đầu: advance sau giảm) + cước gọi/vượt gói + phí thay thiết bị
Thuế dịch vụ   = 12,24% × phần chịu thuế
Tổng kỳ này    = phần chịu thuế + thuế + đặt cọc kỳ đầu sau giảm
Còn nợ         = tổng kỳ này − đã thanh toán
Nợ kỳ trước    = chỉ hiển thị, không cộng vào tổng kỳ này
```

### Bảng giá gói cước (cấu hình trong DB, do Admin quản lý, không hard-code)

| Loại | Gói | Giá | Hiệu lực |
|---|---|---|---|
| Dial-up | Theo giờ 10 / 30 / 60 giờ | 50$ / 130$ / 260$ | 1 / 3 / 6 tháng |
| Dial-up | Unlimited 28 Kbps (tháng / quý) | 75$ / 150$ | 1 / 3 tháng |
| Dial-up | Unlimited 56 Kbps (tháng / quý) | 100$ / 180$ | 1 / 3 tháng |
| Broadband | Theo giờ 30 / 60 giờ | 175$ / 315$ | 1 / 6 tháng |
| Broadband | Unlimited 64 Kbps (tháng / quý) | 225$ / 400$ | 1 / 3 tháng |
| Broadband | Unlimited 128 Kbps (tháng / quý) | 350$ / 445$ | 1 / 3 tháng |
| Landline | Local Unlimited (rental) | 75$ | 1 năm; cước 0,55$/phút |
| Landline | Local Monthly (rental) | 35$ | 1 tháng; cước 0,75$/phút |
| Landline | STD Monthly | 125$ | 1 tháng; Local 0,70 · STD 2,25 · Mobile msg 1,00 |
| Landline | STD Half-yearly | 420$ | đề ghi "1 tháng" (cần xác nhận); Local 0,60 · STD 2,00 · Mobile msg 1,15 |
| Landline | STD Yearly | **đề bỏ trống** | 1 năm; Local 0,60 · STD 1,75 · Mobile msg 1,25 |

## 4. Điểm chưa rõ cần hỏi mentor (email tiêu đề `DOUBT:`)

1. Giá gói **STD Yearly** bị bỏ trống.
2. **Half-yearly** ghi "valid for a month".
3. Ai được đặt đơn: chỉ nhân viên cửa hàng hay cả khách hàng.
4. Ưu đãi ở giá trị biên (đúng 15, 25, 50) và khi dưới 10 kết nối.
5. Định nghĩa "advance" (phí ứng trước), và thuế có tính trên đặt cọc, nợ kỳ trước không.
6. Quy tắc chuyển trạng thái kết nối theo tình trạng thanh toán.
7. Giá vượt giờ của gói theo giờ.
8. Ngày kết thúc dự án.

Chi tiết và giả định tạm thời: xem SRS, phần 10. Nội dung email: `DOUBT_Email_NEXUS.md`.

## 5. Kiến trúc

```
web client (React 19 + Vite)          web admin (React, cùng SPA)
            │  fetch + JWT Bearer
            ▼
ASP.NET Core Web API (.NET 10) ── monolith, một tiến trình
   controllers  ── xác thực JWT + phân quyền theo vai trò
   services     ── nghiệp vụ: sinh ID, khả thi, tính cước, ưu đãi, thuế
   EF Core      ── truy vấn tham số hóa, transaction
            │
            ▼
   database (SQL Server)
```

Bản Java/JSP/Servlet cũ vẫn được giữ trong repo để tham chiếu, nhưng bản chạy chính thức là .NET 10.
Không có API gateway và không chia microservice.

### Cấu trúc thư mục

```
nexus/
├─ backend/
│  ├─ Nexus.slnx            # solution .NET 10
│  ├─ Dockerfile            # multi-stage build cho API
│  ├─ src/Nexus.Api/        # Program.cs, Common, Domain, Data, Services, Controllers
│  └─ tests/Nexus.Tests/    # xUnit: công thức cước, quy tắc nghiệp vụ
├─ frontend/
│  ├─ Dockerfile            # build Vite rồi phục vụ bằng Nginx
│  ├─ nginx.conf            # SPA fallback + proxy /api → backend
│  └─ src/
│     ├─ api/apiClient.js   # lớp gọi API .NET (JWT trong localStorage)
│     ├─ views/             # PublicView (web client)
│     └─ components/        # TopHeader, Navbar, HomeBanner, Footer
├─ database/
│  ├─ schema.sql            # 25 bảng, khoá, ràng buộc
│  └─ seed.sql              # dữ liệu mẫu và tài khoản demo
├─ docs/                    # tài liệu dự án
├─ status/                  # báo cáo tiến độ 1 và 2
├─ docker-compose.yml       # sqlserver + db-init + backend + frontend
└─ README.md
```

Chạy backend: `backend/README.md`. Chạy toàn hệ thống: `docker compose up --build`.
Hướng dẫn code chi tiết: `docs/Huong_dan_code_B_C.md`.

## 6. Quy ước phát triển

- **Mỗi khối code phải có comment** và giải thích logic (yêu cầu bắt buộc của đề).
- Dùng truy vấn **tham số hóa** (chống SQL Injection), escape đầu ra (chống XSS).
- Mật khẩu **băm**, không lưu thuần văn bản.
- Thao tác nhiều bước dùng **transaction** (tạo kết nối + cấp ID + trừ kho; ghi thanh toán + cập nhật nợ).
- Giá cước, ưu đãi, thuế 12,24% là **dữ liệu cấu hình**.
- Không xóa cứng đơn/kết nối/hóa đơn đã phát sinh, dùng trạng thái.
- **Sao lưu dữ liệu mỗi ngày** (virus, lỗi máy, file hỏng).

## 7. Tài liệu phải có (project report)

- [ ] Certificate of Completion
- [ ] Synopsis
- [ ] Table of Contents
- [ ] Problem Definition
- [ ] Customer Requirement Specification (= SRS)
- [ ] Project Plan
- [ ] E-R Diagrams
- [ ] Algorithms
- [ ] GUI Standards Document
- [ ] Interface Design Document
- [ ] Task Sheet
- [ ] Project Review and Monitoring Report
- [ ] Unit Testing Check List
- [ ] Final Check List

## 8. Quy trình nộp bài

**Báo cáo tiến độ (2 lần)**
- Cách nhau khoảng 10 ngày. Dự án dưới 30 ngày: lần 1 trong ngày 7–10, lần 2 trước ngày kết thúc 3 ngày.
- Bắt đầu từ 30/Oct/2026 nên **Status 1 gửi chậm nhất 08/Nov/2026, Status 2 gửi 19/Nov/2026, nộp bài chậm nhất 28/Nov/2026**.
- Nội dung: tiến độ, việc đã xong, khó khăn, minh chứng (code, script DB, ảnh chụp). Nén ZIP, gửi qua Query/Status.
- Dùng mẫu `eProject_Status_Report(V1).xls`.
- Tiêu đề email: `STATUS:` · hỏi đáp: `DOUBT:` · nộp bài: `PROJECT SUBMISSION:`.

**Nộp cuối (1 file ZIP, chỉ 1 lần mỗi nhóm, không sửa sau khi nộp)**
- [ ] Source code đầy đủ
- [ ] Tài liệu dự án, kèm thông tin đăng nhập (ứng dụng, DB)
- [ ] Bản sao lưu database
- [ ] `eProject_Feeback_Form.xls` đã điền
- [ ] Các file liên quan khác
- Tên ZIP ghi rõ **Batch – Group – Tên dự án**.

## 9. Tiến độ

| Hạng mục | Trạng thái |
|---|---|
| SRS, thiết kế API | Bản 1.1, chờ mentor xác nhận giả định |
| Email `DOUBT:` | Đã soạn, chưa gửi |
| ER diagram và schema DB | Xong: `docs/ER_Diagram.md`, `database/` |
| Thiết kế giao diện | Web client React: banner, bảng giá, công cụ chiết khấu, tra cứu, phản hồi |
| Backend .NET 10 | Xong: Domain, DbContext 25 bảng, Auth JWT, Order, Billing, Payment, Catalog, Customer, Admin, Portal |
| Frontend nối API | Web client gọi API thật (bảng giá, cửa hàng, ưu đãi, tra cứu đơn/tài khoản) |
| Web admin | Chưa làm giao diện; API quản trị đã sẵn |
| Kiểm thử | 27 unit test backend pass; chưa chạy toàn luồng với SQL Server thật |
| Docker / CI-CD | Có `docker-compose.yml`, Dockerfile API + web; chưa cấu hình pipeline Azure |
| Status 1 / Status 2 | Chưa gửi |
| Nộp cuối | Chưa |
