# Algorithms (bản nháp phần của Phương, task 27)

Giỏi và Cường bổ sung thuật toán sinh ID, ưu đãi, kiểm tra khả thi, cấp kết nối.

## 1. Lập hóa đơn (`BillingService.generate`, `BillingCalculator`)

```
BEGIN TRANSACTION
  khóa dòng kết nối (UPDLOCK)                     -- hai người lập cùng lúc phải xếp hàng
  kiểm tra kỳ: start <= end <= start + 1 tháng, end >= ngày kích hoạt, chưa hoàn cọc
  nếu start <= kỳ cuối cùng đã lập: BILL_EXISTS   -- chặn cả trùng lẫn chồng lấn
  kỳ đầu = kết nối chưa có hóa đơn
  đầu chu kỳ = số tháng từ kỳ đầu chia hết cho hiệu lực của gói
  phí gói = kỳ đầu: giá × (1 − % ưu đãi); đầu chu kỳ: giá; còn lại: 0
  cước gọi = tổng làm tròn(số phút × đơn giá) theo từng loại
  vượt gói = (vượt lũy kế sau kỳ này − vượt lũy kế trước kỳ này) × giá vượt giờ
  phần chịu thuế = phí gói + cước gọi + vượt gói + phí thay thế UNBILLED + điều chỉnh
  thuế = làm tròn HALF_UP(phần chịu thuế × thuế suất)
  cọc = kỳ đầu: cọc × (1 − % ưu đãi); còn lại: 0
  tổng = phần chịu thuế + thuế + cọc
  nợ kỳ trước = tổng số còn nợ của các hóa đơn khác (chỉ hiển thị)
  lưu hóa đơn; đánh dấu phí thay thế BILLED
COMMIT
```

Xem trước (`preview`) gọi cùng hàm tính nhưng không lưu.

**Ví dụ (đã có unit test):** Broadband 225$, cọc 500$: thuế 27,54$, tổng 752,54$. Đơn 10 kết nối giảm 25%: advance 168,75$, thuế 20,66$, cọc 375$, tổng 564,41$.

## 2. Ghi nhận thanh toán (`PaymentService.record`)

```
kiểm tra: số tiền > 0, hình thức hợp lệ, ngày không ở tương lai
BEGIN TRANSACTION
  khóa dòng hóa đơn (UPDLOCK)
  nếu số tiền > còn nợ: PAYMENT_EXCEEDS_DUE
  thêm thanh toán; đã trả += số tiền; còn nợ −= số tiền
  trạng thái = còn nợ = 0 ? PAID : PARTIAL
COMMIT
```

## 3. Hoàn cọc (`PaymentService.refundDeposit`)

```
BEGIN TRANSACTION
  khóa dòng kết nối; phải PERM_INACTIVE và chưa hoàn
  hoàn = max(0, cọc đã lập hóa đơn − nợ còn lại − phí thay thế UNBILLED)
  còn lại = cọc đã lập hóa đơn
  với mỗi hóa đơn còn nợ, cũ nhất trước: trả bằng thanh toán DEPOSIT_OFFSET, trừ vào còn lại
  với mỗi phí UNBILLED: đủ thì chuyển DEPOSIT_OFFSET, thiếu thì giảm số tiền của khoản đó
  ghi số tiền hoàn và ngày hoàn lên kết nối
COMMIT
```

## 4. Đăng nhập (`AuthService.login`)

```
tìm tài khoản theo username
không có: vẫn chạy BCrypt một lần, báo AUTH_INVALID  -- thời gian phản hồi không lộ username
sai mật khẩu hoặc bị ngưng: AUTH_INVALID
đúng: đổi session id, cấp token CSRF mới
nếu đang dùng mật khẩu tạm: chỉ cho đổi mật khẩu hoặc đăng xuất
```
