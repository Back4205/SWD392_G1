# 05/11/2026 — Đăng nhập và màn hình theo vai trò

Người dùng yêu cầu web vận hành theo tài khoản và không chấp nhận trải nghiệm chỉ phục vụ demo. Bộ chọn actor/header và các user ID hardcode cản trở yêu cầu đó, nên thay đổi phải đi từ API tới giao diện.

## Thay đổi và quyết định

- Hoàn thiện Auth đơn giản theo JWT trong SDS: kiểm tra password hash, cookie HttpOnly, CSRF, phiên lưu database, logout và đổi mật khẩu. API kiểm tra phiên và user Active/role hiện tại.
- Tách URL/menu cho Staff, Manager và Purchasing. Staff nhận hàng/serial; Manager duyệt Receipt và quản lý Inbound; Purchasing xử lý supplier commitment/tiến độ. Quyền Receipt giữ theo phạm vi guide Người 5.
- Form Inbound chọn dữ liệu thật qua API lookup. React dùng fetch chung, History API, state/effect thường để sinh viên dễ đọc.
- Đổi tên bản cộng tồn đang ghi SQL thành `StockReceivingService`, đăng ký cho nghiệp vụ nhận hàng. Giữ transaction và toàn bộ quy tắc count/remaining/capacity/Version.
- Thêm migration `AuthSession`, giữ migration Receipt cũ. Bootstrap local yêu cầu mật khẩu người chạy tự chọn, không có default password trong source/UI; dữ liệu nghiệp vụ không bị reset.
- Chỉnh script start/stop để chạy nền, ghi log/PID trong `.local/` và dừng đúng tiến trình đã tạo. Reviewer phát hiện Windows PowerShell làm mất độ chính xác khi serialize DateTime; đổi sang lưu UTC ticks để so sánh.

## Kiểm chứng và giới hạn

Backend build 0 warning/error; 61/61 SQL/HTTP test qua, không skip. React TypeScript/Vite build thành công. Browser đã xác nhận đăng nhập/menu riêng, URL sai quyền bị chặn, logout và luồng Manager tạo yêu cầu → Purchasing cam kết → Manager duyệt → Staff scan/bin/submit → Manager duyệt nhận một phần rồi nhận đủ. Chi tiết và số lượng kiểm tra cuối do báo cáo QA của giai đoạn ghi riêng.

Chỉ áp migration cho database Development riêng và database test. Chưa deploy hoặc ghép đầy đủ các phân hệ khác của nhóm. Tài liệu chạy được viết lại theo đăng nhập thật; các báo cáo demo cũ giữ làm lịch sử, không dùng làm cấu hình hiện tại.

Chốt kiểm chứng: React lint/build qua; start/reset/restart giữ mật khẩu/stop đã chạy thật. Chrome kiểm tra thêm đổi mật khẩu, phiên thu hồi, từ chối với lý do tiếng Việt và layout mobile. [Báo cáo đầy đủ](../../plans/20261105-role-based-web/reports/verification-20261105.md).
