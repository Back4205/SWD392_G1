---
title: Goods Receipt Backend theo cấu trúc dễ học
date: 2026-11-04
scope: backend
---

# Goods Receipt Backend — ghi nhận triển khai

## Bối cảnh

Nhánh `feature/inbound-request` đã merge vào `Bacsk/goods-receipt` ở commit `e523a49`. Người dùng mới học ASP.NET Core Web API, yêu cầu code Backend Người 5 theo phong cách dễ đọc của module Inbound. Các thay đổi `appsettings.json` có trước được giữ nguyên.

## Thay đổi chính

- Controller gọi Service; Service dùng DbContext trực tiếp theo yêu cầu đơn giản hóa. DTO class, constructor thường, map thủ công, if/foreach và try/catch; không dùng AutoMapper/CQRS/Unit of Work.
- Hoàn thiện create Draft, scan staging, chọn bin, save, submit và Manager approve/reject theo các use case và sequence SDS 3.3.2.1–6.
- Approval giữ thứ tự Serial/Movement → Stock → Approved → Inbound → commit, dùng transaction rõ ngay trong hàm. Shared DbContext, Serializable và khóa SQL bảo vệ các thao tác đồng thời.
- Bổ sung migration tối thiểu cho staging, bin nháp nullable, rowversion/UpdatedAt và reason Unicode. Không sửa migration cũ; không cập nhật database ứng dụng người dùng.
- Tiến độ Inbound được nối với receipt Approved thật. MockStockService tạm thay Người 2 nhưng ghi SQL development thật để kiểm tra rollback; chưa ghép implementation Stock/Auth của nhóm.

## Quyết định và rà soát

Giữ swimlane Import Goods cùng sequence mới khi UC-17/confirmReceipt cũ mô tả Staff tăng tồn ngay. Receipt mới là Draft; Completed thuộc Inbound. Chỉ creator sửa draft; không tự thêm reopen/cancel hoặc reservation serial toàn cục ở draft.

Review phát hiện cần recheck format serial sau khi policy thay đổi và cần chặn rollback schema còn bin nháp null. Cả hai được xử lý. Program báo rõ chưa có StockService production thay vì để lỗi DI xảy ra trong request. Kết quả cuối: **46/46 test qua, 0 lỗi/skip**, SQL Server thật và HTTP/Kestrel, khoảng 37 giây. Gồm ba điểm fault rollback, các race, policy serial đổi trước Submit/Approve, process Production/Staging guard và scan còn sau khi dừng/tạo lại API host. Xem [bàn giao](../goods-receipt-backend.md).

Đã chạy quy trình migration/seed bằng Program thật trên database demo riêng mới tạo; list receipt rỗng và Inbound expected 10/received 0/remaining 10 đúng. Giữ dataset demo nguyên vẹn để người dùng thực hành.

## Bài học và bước ghép nhóm

Code ngắn nhất chưa chắc dễ đọc nhất. Viết rõ các bước và map từng thuộc tính giúp người mới lần theo request đến database và giải thích được vì sao transaction cần rollback. Những bảo vệ cần thiết như rowversion, unique index và khóa SQL vẫn được giữ và chú thích.

Người 2 thay đăng ký IStockService bằng service thật dùng chung DbContext. Người 3 cung cấp principal đã xác thực. Sau khi ghép, chạy lại test quyền, remaining/capacity, duplicate, concurrency và rollback. Không triển khai frontend trong lần này.
