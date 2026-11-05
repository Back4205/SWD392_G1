---
title: React Goods Receipt theo cách viết dễ học
date: 2026-11-05
scope: frontend
---

# React Goods Receipt — ghi nhận triển khai

## Bối cảnh

Sau khi hoàn thành Backend, người dùng chọn làm tiếp giao diện React. Giữ React 18/Vite/TypeScript đã có và phong cách dễ đọc của module Inbound: component thường, state/effect, fetch và CSS; không thêm router hoặc thư viện UI.

## Thay đổi

- Thêm workspace nhận hàng với danh sách/lọc/phân trang, tạo từ Inbound còn thiếu, chi tiết/scan/bin/save/submit và dialog Manager approve/reject.
- Giữ các màn hình Inbound cũ trong InboundPages; App trở thành shell hai mục điều hướng. Hash URL giữ màn hình và bộ lọc khi refresh/back.
- Bổ sung session và lookup Inbound/bin có scope trong module Backend. Role, số lượng và quyết định không được giả lập ở React.
- Tách lựa chọn bin khỏi dữ liệu receipt để scan không làm mất vị trí chưa lưu. Submit lưu draft trước rồi dùng Version mới. Stale reload dữ liệu, không retry quyết định tự động.
- Thêm cấu hình proxy và actor demo opt-in chỉ trong Development. Production vẫn chờ Auth/Stock thật.

## Rà soát và kiểm chứng

Đã sửa skip link xung đột hash router, đổi ngày lọc địa phương sang UTC và cho phép xem phiếu lịch sử khi lookup bin lỗi. Step “Quét & xếp bin” chỉ hoàn tất khi bin của các dòng nhận đã được lưu.

Build TypeScript/Vite đạt. Backend mở rộng lookup đạt 54/54 test SQL/HTTP thật. Hoàn tất 44 kiểm tra browser với API thật, gồm nhận 8 rồi 2 đến Completed, reject, invalid/duplicate, stale, giữ bin chưa lưu, reload, offline/retry, Enter/submit nhanh và Inbound cũ. SQL xác nhận tồn 10, reserved 0 và đúng 10 movements. Đã sửa mobile overflow từ nhãn screen reader; viewport 375px không còn tràn trang. Console cuối không lỗi. Bản production không chứa header user demo; báo cáo ở [hướng dẫn React](../goods-receipt-frontend.md).

## Bài học và bàn giao

Một response thành công mới là căn cứ cập nhật giao diện. UI giữ Version và lựa chọn đang nhập, còn Backend chịu trách nhiệm quyền, scan count, capacity và transaction. Cách tách types → api → page → component giúp sinh viên lần theo từng thao tác mà không cần framework bổ sung.

Dataset development đã có thêm phiếu QA, không reset dữ liệu cũ. Khi thực hành, chọn Inbound còn thiếu hoặc tạo yêu cầu mới bằng module Inbound. Không deploy hoặc commit/push trong bước này.
