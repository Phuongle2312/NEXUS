# KỊCH BẢN KIỂM THỬ TỔNG THỂ (E2E TEST SCENARIOS)

## Kịch bản 1: Luồng Đặt Đơn Khách Hàng Doanh Nghiệp (Bulk Order)
1. **Bước 1 (Retail Staff)**: Vào Cửa hàng bán lẻ -> Tạo đơn mới với 15 đường Broadband.
   - Kiểm tra: Tự động áp dụng chiết khấu 25% cho cả tiền cọc ($500) và tiền cước trả trước ($350).
   - Mã Order ID sinh ra dạng `B000000000x`. Trạng thái: `SUBMITTED`.
2. **Bước 2 (Technical Staff)**: Chuyển sang vai trò Kỹ thuật -> Xác nhận khả thi `FEASIBLE`.
3. **Bước 3 (Technical Staff)**: Thi công đấu nối -> Kích hoạt và cấp `Account ID` 16 ký tự `B00100000000000x`, gán thiết bị Router.
4. **Bước 4 (Accounts)**: Chuyển sang vai trò Kế toán -> Lập hóa đơn cước tự động tính thuế dịch vụ 12.24%.
5. **Bước 5 (Customer)**: Đăng nhập Cổng khách hàng -> Kiểm tra thấy đường truyền và hóa đơn tương ứng.

