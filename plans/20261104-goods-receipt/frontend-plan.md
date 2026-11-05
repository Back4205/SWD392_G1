---
title: Giao diện React Goods Receipt
status: completed
created: 2026-11-04
updated: 2026-11-05
scope: frontend
---

# Giao diện React Goods Receipt

Ngày 2026-11-04, hoàn tất ngày 2026-11-05. Trạng thái: completed. Người dùng đã chuyển bước tiếp theo sang Frontend React.

## Cơ sở và yêu cầu

- Vite + React 18 + TypeScript đã có. Module Inbound nằm trong features/inbound, dùng fetch/useState/useEffect và CSS thường, chưa có router hay UI library.
- Backend Goods Receipt có 8 route, plain DTO, expectedVersion và kiểm tra quyền. UI phải gọi thật, không hiển thị tăng tồn giả.
- Thiếu API lookup bin/Inbound có scope và session role. Bổ sung ba query nhỏ trong module receipt để UI chọn dữ liệu thật, không bắt người dùng gõ ID. Đây là chi tiết tích hợp, không thêm nghiệp vụ ngoài nguồn.
- Giữ UI Inbound đang có. Dùng shell có hai mục điều hướng; hash URL cho refresh/back và filter. Không thêm framework/router/state library.
- Tiếng Việt, nền sáng, xanh cùng tông Inbound, bảng đọc rõ, badge có chữ, bố cục responsive; scan panel là điểm nhấn thao tác. Không ảnh trang trí hoặc thư viện icon mới.
- Development có bộ chọn Staff/Manager demo, chỉ xuất hiện khi Vite DEV và cấu hình opt-in. Role thật lấy từ session API; backend vẫn kiểm tra quyền. Không làm màn hình login giả.

## Kế hoạch đã rà soát

1. [x] Lookup/session API scoped + test; giữ nguyên 8 route/write contract.
2. [x] Types/API client + hàm thông báo lỗi + hash navigation đơn giản.
3. [x] Danh sách/paging/filter, chọn Inbound còn nhận được, tạo nháp.
4. [x] Chi tiết/scan/bin/save/submit, Manager approve/reject dialog, trạng thái read-only.
5. [x] Tích hợp shell giữ trang Inbound, proxy/env demo, responsive/accessibility.
6. [x] Build và test browser với backend thật: Staff→Manager, duplicate/invalid/stale/reject, reload/mobile, lỗi network, regression Backend.
7. [x] Hướng dẫn chạy/đọc code, kết quả test và điểm chờ Auth/Stock thật.

Scope: chức năng UI Người 5 theo mục 10 guide, không Cancel/Reopen/delete serial, không CRUD Product/Warehouse/Auth, không redesign module Inbound. Local repository implementation; không đăng ký/publish Sites.

Luồng bắt buộc: tạo Draft → scan hợp lệ → chọn bin → lưu thành công → submit PendingApproval → Manager duyệt/từ chối. Chỉ approve tăng tồn. Mỗi write dùng version mới nhất. 409 stale tải lại detail và yêu cầu người dùng xem lại, không tự retry quyết định. Không tin role/quantity từ frontend.

## Kết quả kiểm chứng

- TypeScript + Vite production build đạt, không thêm package. .NET build 0 warning/error; 54/54 SQL Server/HTTP tests sau bổ sung ba query lookup/session.
- Review sửa skip link/hash route, timezone bộ lọc, optional bin lookup cho phiếu lịch sử và step placement chỉ hoàn tất khi bin được lưu.
- 44 browser assertions với Chrome isolated và API thật; 10→8→2, reject, invalid/duplicate, stale, bin chưa lưu, refresh/back, Enter/submit nhanh, network retry, Inbound cũ đều đạt.
- Mobile overflow từ nhãn table screen reader được sửa bằng containing block; 375px document width bằng viewport. Đã kiểm tra ảnh desktop/mobile, console cuối không lỗi.
- SQL kiểm tra độc lập: tồn 10/reserved0/10 movements cho luồng nhận đủ; rejected không tạo serial chính thức.
- Production bundle không có header user demo. Không deploy/commit/push. Backend vẫn chờ Auth/Stock thật của nhóm trước production.
- Báo cáo reports/browser-qa-20261105.md; hướng dẫn docs/goods-receipt-frontend.md và frontend/README.md; journal docs/journals/261105-goods-receipt-frontend.md.
- Toàn bộ 7 mục được sync; không có phase-XX-*.md riêng. Kế hoạch demo script trước đó đã ghi thay thế bởi lựa chọn React, không còn hạng mục cần thực thi trong scope đó.
