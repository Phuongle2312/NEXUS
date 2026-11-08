# Synopsis và Problem Definition (bản nháp, task 27)

> Batch, Group, số báo danh: **chờ điền**. Certificate of Completion chờ mẫu và chữ ký của trung tâm.

## Synopsis

Nexus Service Marketing System là ứng dụng web thay thế quy trình giấy tờ của Nexus Communication System, nhà cung cấp dịch vụ điện thoại bàn, Dial-up và Broadband qua mạng lưới cửa hàng bán lẻ. Hệ thống quản lý đơn đăng ký, kiểm tra khả thi, cấp kết nối, thiết bị và kho, gói cước, hóa đơn và thanh toán trả sau, với đăng nhập riêng cho Admin, nhân viên cửa hàng, kỹ thuật, kế toán và khách hàng.

**Công nghệ:** Java 17, JSP/Servlets (Jakarta EE), JDBC, SQL Server 2019 trở lên, Tomcat 10.1.

**Kiến trúc:** phân tầng Servlet → Service → DAO → CSDL. Service phụ thuộc vào interface DAO và được lắp ráp tại một nơi duy nhất, nên nghiệp vụ test được không cần CSDL.

## Problem Definition

**Hiện trạng.** Hồ sơ khách hàng, thanh toán, nhà cung cấp, đơn hàng, giấy tờ địa chỉ và biên lai lưu trên giấy theo năm và khu vực. Khối lượng giấy tờ lớn dẫn đến sai sót; khó tra cứu trạng thái đơn, kết nối và công nợ.

**Vấn đề cần giải quyết.**

1. Tiếp nhận và theo dõi đơn bằng mã duy nhất (Order ID 11 ký tự).
2. Kiểm tra khả thi trước khi cấp kết nối; Dial-up phải có landline của Nexus.
3. Cấp Account ID 16 ký tự theo loại, thành phố và số thứ tự.
4. Tính cước theo gói, ưu đãi số lượng lớn, thuế dịch vụ 12,24%; ghi nhận rõ số đã trả và còn nợ.
5. Phân quyền chặt: chỉ cửa hàng tạo đơn, chỉ kế toán cập nhật hóa đơn, chỉ Admin quản lý nhân viên và danh mục.
6. Tra cứu cơ bản và nâng cao; thu thập phản hồi khách hàng.

**Ngoài phạm vi.** Cổng thanh toán trực tuyến, dịch vụ trả trước, ứng dụng di động.

Chi tiết yêu cầu: `SRS_NEXUS_Service_Marketing_System.docx`.
