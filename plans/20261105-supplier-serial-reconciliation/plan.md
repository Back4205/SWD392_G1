---
title: Khai báo SKU/serial từ nhà cung cấp và đối chiếu khi nhận hàng
status: completed
created: 2026-11-05
---

# Phạm vi đã rà soát

Yêu cầu mới: Purchasing nhập đủ SKU/serial hàng nhà cung cấp xác nhận; Staff đối chiếu với phiếu nhận thực tế. RP1 UC12 có confirmed product details; lưu serial dự kiến và kiểm tra từng serial là phần mở rộng do người dùng yêu cầu lần này, không khẳng định đã có nguyên vẹn trong sequence cũ.

Code hiện tại: React18/TS/Vite, API.NET8/EFSQL; role JWT/cookie/CSRF và kho được phân công. SupplierCommitmentForm chỉ có supplier/status/date; InboundRequestItem chỉ có variant/expectedQty; Receipt scan lưu GoodsReceiptSerial tạm, chỉ Approve mới đăng ký SerialNumber/tồn kho. Không dùng AutoMapper/MediatR/CQRS.

## Thiết kế và tiêu chí chấp nhận

- Giữ SKU của dòng kế hoạch Manager đã lập, không tự tạo hoặc đổi master SKU trong form Purchasing. Hiển thị tên sản phẩm/SKU/số lượng; nhập nhiều serial mỗi dòng, giữ số 0 đầu và chữ hoa/thường. Confirmed yêu cầu đúng đủ số serial từng SKU; không trùng trong phiếu, không có serial đã nhập kho hoặc đang khai báo ở đơn đang hiệu lực khác. Rejected không yêu cầu serial.
- Thêm dữ liệu serial dự kiến riêng có FK đến InboundRequestItem, index/unique phù hợp; migration bổ sung, không sửa migration cũ. Khai báo không tạo SerialNumber/Stock/Movement. Lưu commitment + toàn bộ danh sách trong một transaction. Chỉ Purchasing trong kho được phép, Pending mới sửa được.
- Manager chỉ duyệt yêu cầu mới sau khi supplier Confirmed và danh sách SKU/serial đầy đủ. Sau Approved danh sách khóa. Transaction và khóa dùng chung với nhận hàng để không đua trạng thái.
- Staff nhìn danh sách serial dự kiến cạnh kết quả thực nhận: chưa quét, quét phiếu này, ở phiếu khác đang xử lý, đã nhận chính thức. Không lấy serial khai báo làm serial thực nhận. Phiếu Reject không được tính đã nhận; nhận nhiều đợt vẫn đúng.
- Người dùng đã chọn: chặn serial lệch, báo rõ không thuộc SKU/Inbound, không lưu scan và không tăng số lượng. Không tự tăng tồn ở bước scan.
- Bảo vệ Scan/Submit/Approve ở server, không chỉ ở React. Giữ quy tắc count/remaining/capacity/rowversion/rollback và role.
- Dữ liệu Inbound đã Approved/PartiallyReceived/Completed trước migration không được tự tạo serial dự kiến giả. Gắn thông báo lịch sử chưa có bảng kê; giữ việc đọc/nhận đang diễn ra của dữ liệu cũ, áp kiểm tra bắt buộc cho các yêu cầu xác nhận mới. Không reset dữ liệu đang chạy.
- Không làm PO độc lập, supplier portal, import Excel hay CRUD phân hệ khác. Gọi dữ liệu là bảng kê hàng nhà cung cấp xác nhận thuộc Inbound hiện có.

## Hợp đồng đã triển khai

`PUT /api/inbound-requests/{id}/supplier-commitment`: thêm `items:[{itemId,sku,serialCodes:string[]}]` cho Confirmed. Detail Inbound thêm `hasSupplierManifest`, `items[].sku/productName/expectedSerials`. Detail Receipt thêm `hasSupplierManifest`, `items[].expectedSerials:[{serialCode,status,receiptId,scannedSerialCode}]` (trạng thái từ server; giữ mã khai báo và mã thực tế riêng). Các field mới bổ sung, không đổi các field cũ. Confirmed không còn chấp nhận payload thiếu bảng kê theo yêu cầu mới.

## Công việc

1. [x] Entity/DTO/service/transaction/migration bảng kê và validation xác nhận/duyệt Inbound.
2. [x] Form Purchasing nhập đủ SKU/serial, kiểm tra lỗi/dán nhiều dòng, detail hiển thị bảng kê khóa.
3. [x] API scan/submit/approve kiểm tra đối chiếu, Receipt DTO và UI so sánh trực quan, xử lý dữ liệu lịch sử.
4. [x] SQL/HTTP regression + test nghiệp vụ mới; browser ba role, sai serial, nhận một phần/nhận đủ/reject.
5. [x] Review, build/lint, hướng dẫn thay đổi usecase/sequence/schema và báo cáo kiểm chứng; không commit/push/deploy.

Review kế hoạch: thay đổi schema tối thiểu; không nhập kho trước duyệt; số lượng serial được validate ở server; dữ liệu lịch sử không bị suy diễn; bảo toàn thay đổi chưa commit đang có trong workspace.

Hoàn tất phạm vi. [Báo cáo kiểm chứng](reports/verification.md): 80/80 SQL/HTTP, lint/build, Chrome đủ ba role, nhận hai đợt và từ chối/nhận lại. [Hướng dẫn nghiệp vụ và payload](../../docs/supplier-serial-reconciliation.md). Đã đồng bộ các mục của kế hoạch này; các kế hoạch Backend/React/role trước đó giữ trạng thái hoàn thành trong phạm vi cũ.
