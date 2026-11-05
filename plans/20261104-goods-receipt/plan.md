---
title: Goods Receipt Backend theo cách viết Inbound
status: completed
priority: P1
effort: large
branch: Bacsk/goods-receipt
tags: [backend, goods-receipt, beginner]
created: 2026-11-04
---

# Goods Receipt Backend theo cách viết Inbound — 2026-11-04

Status: completed — module Backend độc lập, chờ ghép Auth/Stock thật. Người dùng đã chốt chỉ Backend Người 5 (UC-15/16/17/18/50/56), không làm React.

## Cơ sở đã kiểm tra

- Đã merge origin/feature/inbound-request vào Bacsk/goods-receipt, commit e523a49; build 0 warning/error.
- Inbound dùng DTO class, interface trong Interfaces, constructor thường, map thủ công, if/foreach, try/catch.
- Giữ yêu cầu người dùng: không AutoMapper/CQRS/MediatR/generic Repository/Unit of Work; Goods Receipt dùng Controller → Service → DbContext, đăng ký AddScoped trực tiếp Program.cs. Giữ phong cách code Inbound để dễ học.
- Entity/migration cũ có đầy đủ bảng nghiệp vụ; thiếu staging scan, Version, BinId nullable cho nháp. GoodsReceipt đã có người duyệt/ngày/lý do, tái sử dụng; reason đổi nvarchar để lưu tiếng Việt.
- appsettings.json là thay đổi có sẵn của người dùng, không sửa/đưa secret vào code. File hướng dẫn đầu vào giữ nguyên.

## Mapping và quyết định

| Nguồn | Hiện trạng | Hiện thực |
|---|---|---|
| UC15/16 | Chưa có API receipt | List/detail có scope kho, DTO class |
| UC17 | Inbound có Approved | Chỉ tạo Draft từ Approved/PartiallyReceived còn thiếu |
| UC18 | Thiếu Version/bin nháp | Rowversion + UpdatedAt, lưu bin và submit riêng |
| UC50 | Chỉ có SerialNumber chính thức | Bổ sung GoodsReceiptSerial; đếm số scan, không tin ReceivedQty frontend |
| UC56 / SDS 3.3.2.6 | Có field decision | Transaction trực tiếp: validate → Serial/Movement → Stock → Approved → Inbound → commit |
| Inbound progress | API đã có, đang trả NotAvailable | Tính Approved-only theo variant trong contract hiện tại |
| Người 2 | Chưa có Stock service | IStockService + MockStockService ghi SQL development chung transaction |
| Người 3 | Auth chưa có, Inbound đang hardcode 1 | Receipt dùng principal; tùy chọn header user chỉ Development, luôn kiểm tra DB user/role/kho |

Trạng thái receipt dùng Draft/PendingApproval/Approved/Rejected để đồng bộ kiểu viết Approved của module Inbound. Inbound dùng Approved/PartiallyReceived/Completed. DRAFT/PENDING/REJECTED không tính vào đã nhận. Không tạo Cancel/Reopen/Resubmit. Một dòng/variant, creator sửa draft. Không giữ serial toàn cục ở draft; unique trong receipt và unique chính thức toàn hệ thống.

SRS UC17 bước 9/Completed, non-UI stock increment, confirmReceipt cũ và label/email mâu thuẫn swimlane + UC56 + sequence mới: thực thi Staff submit, Manager approve mới nhập tồn. Sơ đồ kỹ thuật Repository được giản lược theo yêu cầu trực tiếp của người dùng; thứ tự nghiệp vụ/usecase/nhánh alt vẫn giữ. Các bổ sung schema là chi tiết hiện thực, không nhận là cột gốc SDS.

## Các bước đã rà soát trước khi code

1. [x] Schema tối thiểu + DTO/interface + DI cơ bản.
2. [x] List/detail/create/scan/bin/save/submit, quyền/state/version, remaining/capacity.
3. [x] Approve/reject nguyên tử, IStockService thay thế và tích hợp tiến độ Inbound.
4. [x] Dataset dev, HTTP mẫu, test SQL Server riêng (10→8→2, rollback, race, GR01–38).
5. [x] Review, build/migration checks, hướng dẫn đọc code/bảo vệ, bàn giao phạm vi thật/mock.

Giữ SQL transaction SERIALIZABLE + khóa SQL chung module trong một method DbContext có chú thích (cần để chống nhận vượt khi concurrent). Không tạo transaction callback/generic Unit of Work. Mock không tự commit. Không bật retry tự động. Không áp migration vào database đang dùng của người dùng.

## Kết quả và bàn giao

- 46/46 test SQL Server thật + HTTP Kestrel pass, 0 skip; có fault injection và race, kiểm tra đổi serial policy, khởi động lại API, Program Production/Staging.
- Review xác nhận thứ tự approval, scope quyền và phép tính; đã sửa ba điểm được phát hiện: recheck format khi submit/approve, guard rollback bin null, báo lỗi startup khi chưa có Stock thật.
- Build 0 warning/error, EF không có model change thiếu migration; chỉ thêm migration AddGoodsReceiptWorkflow, không sửa hai migration cũ.
- Đã tạo database demo riêng EWMS_GoodsReceipt_Development, áp migrations và seed bằng Program thật. GET receipts rỗng; Inbound1 expected10/received0/remaining10. API đã dừng sau smoke read-only; dataset còn nguyên.
- Hướng dẫn đọc/chạy và mapping nguồn: docs/goods-receipt-backend.md; request mẫu EWMS/EWMS.Api/GoodsReceipt.http. Journal docs/journals/261104-goods-receipt-backend.md.
- Không có phase-XX-*.md riêng; toàn bộ năm mục của plan đã đối chiếu và hoàn tất. Plan entity trước đó vẫn completed, không thay đổi phạm vi.
- Chưa ghép Auth Người3/Stock Người2; production bị chặn rõ cho đến khi thay DI Stock thật. Không triển khai frontend, không commit/push.
