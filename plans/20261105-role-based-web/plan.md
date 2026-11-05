---
title: Web EWMS phân khu theo role và đăng nhập thật
status: in-progress
created: 2026-11-05
scope: role-based-web
---

# Yêu cầu và cách triển khai đã rà soát

Người dùng không chấp nhận bộ chọn actor demo; yêu cầu các trang theo role và trải nghiệm web hoàn chỉnh. Giữ code C#/React dễ đọc, không CQRS/MediatR/AutoMapper, không làm CRUD ngoài nghiệp vụ hiện có.

## Baseline và nguồn

React Receipt đã nối Backend thật, 54 test SQL/HTTP + 44 browser checks ở giai đoạn trước. Tuy nhiên frontend dùng actor picker/header, Inbound còn hardcode user1, Auth chưa có và Program chặn Production vì Stock mang tên Mock. Các vấn đề này phải xử lý thật.

RP2 3.1/3.7.2.1/3.7.3 quy định username/password → kiểm tra user/Active/hash → JWT có role/permission. SRS UC Create Inbound: Manager tạo; Purchasing xử lý commitment; Manager review; Staff chỉ nhận từ Inbound được duyệt. Không cấp mọi trang cho mọi role.

## Quyết định

- AuthService/AuthController/TokenService class thường; PasswordHasher<User> chuẩn ASP.NET, JWT ký khóa ngoài source. JWT truyền qua HttpOnly SameSite cookie, request thay đổi có anti-CSRF token. Logout thu hồi session bền trên DB; account inactive/role đổi bị chặn ngay. Không plaintext password hoặc JWT trong localStorage.
- Tái sử dụng User/Role/UserWarehouse; thêm AuthSession bằng migration tối thiểu nếu cần thu hồi JWT. Chưa có BCrypt Auth trong repo để tương thích; không nhận placeholder hash là password.
- Bỏ actor header hoàn toàn khỏi Controller/frontend. Authorize và warehouse scope cho Receipt và Inbound; không chỉ ẩn nút.
- Promoting Stock receiving implementation đang ghi SQL thật thành StockReceivingService, đăng ký mọi environment; đây chỉ phần cộng tồn nhận hàng, không nhận là toàn bộ module kho của Người2.
- Bootstrap local có mật khẩu do người chạy cung cấp qua environment/prompt, chỉ database development riêng; không gán default password vào source/UI. Không reset dữ liệu nghiệp vụ.
- Browser URL thật: /login; /staff/...; /manager/...; /purchasing/.... Route guard ở React theo session API; 403/404, đăng xuất, hết phiên, loading/network recovery.
- Staff: danh sách nhận hàng/tạo/chi tiết và nguồn Inbound được phép nhận. Manager: chờ duyệt/lịch sử/chi tiết, tạo và duyệt Inbound. Purchasing: danh sách/chi tiết Inbound, supplier commitment, tiến độ; không duyệt Receipt. Profile/change password cho tài khoản đang đăng nhập.
- Những role Admin/Sales thuộc module khác không được cấp quyền vận hành nhận hàng mặc định. Không tự tạo CRUD phân hệ chưa có.

## Checklist

1. [ ] JWT/password/session/CSRF/rate limit, login/logout/me/password change và tests thật.
2. [ ] Loại bypass/header/hardcode, phân quyền+warehouse scope API, Stock receiving DI thật.
3. [ ] Login và session lifecycle; layout/menu/routes riêng Staff/Manager/Purchasing.
4. [ ] Chuyển Receipt/Inbound sang đúng trang role và request xác thực chung, profile/password, không demo UI.
5. [ ] Bootstrap tài khoản local an toàn, cập nhật start-dev/env/hướng dẫn.
6. [ ] SQL/HTTP tests, browser login từng role, URL/API forbidden, stale/receiving/reject/logout/expired/mobile.
7. [ ] Review, build, cập nhật docs/plan/journal, không claim deployment chưa thực hiện.

Hướng triển khai Auth bám hướng dẫn Microsoft chính thức về JWT validation, password hashing và antiforgery. Không dùng cookie-only identity thay cho JWT trong SDS; cookie chỉ là cách lưu/chuyển JWT.
