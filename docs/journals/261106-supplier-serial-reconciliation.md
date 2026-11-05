# Bảng kê nhà cung cấp và đối chiếu thiết bị

## Quyết định nghiệp vụ

Người dùng yêu cầu Purchasing nhập đủ SKU/serial nhà cung cấp xác nhận để Staff so sánh với hàng thật, và chọn chặn mã sai mà không tăng số lượng. RP1 UC12 có confirmed product details; đối chiếu từng serial được ghi là yêu cầu mở rộng lần này, không gán ngược vào sequence cũ.

Giữ SKU và số lượng từ kế hoạch Manager. Purchasing chỉ khai báo serial, không tạo serial chính thức/tồn kho. Chỉ cho duyệt Inbound mới khi bảng kê Confirmed đầy đủ; khóa sau xử lý. Staff quét thực tế, server kiểm tra ở Scan/Save/Submit/Approve và vẫn chỉ cộng tồn sau Manager duyệt Receipt.

## Thay đổi

- Thêm cờ `HasSupplierManifest`, bảng `InboundExpectedSerial`, composite FK và unique trong migration bổ sung; không sửa migration cũ.
- Commitment và bảng kê lưu nguyên tử dưới transaction/khóa dùng chung. Validation đủ dòng, đúng SKU/count, format, duplicate và mã đã được sử dụng.
- Purchasing nhập nhiều dòng với bộ đếm/lỗi rõ ràng; Staff/Manager có bảng dự kiến cạnh mã quét và trạng thái nhận qua nhiều đợt.
- Tách mã khai báo và `ScannedSerialCode`; phiếu bị từ chối giữ lịch sử scan nhưng không tính đã nhận, có thể tiếp nhận lại bằng phiếu khác.
- Giữ yêu cầu đã duyệt trước migration là dữ liệu lịch sử chưa có bảng kê, không backfill bằng serial thực nhận.

## Kiểm chứng và giới hạn

80/80 SQL/HTTP tests qua, gồm 61 cũ và 19 mới; build API/project test không warning/error. React lint/build qua, 44 modules. Chrome kiểm tra ba role, đủ/thiếu/trùng bảng kê, mã ngoài danh sách/sai SKU không đổi count/Version, trạng thái nhận qua hai đợt và từ chối/nhận lại; mobile không tràn ngang. Đã sửa trùng key component progress trong chi tiết Inbound. [Báo cáo kiểm chứng](../../plans/20261105-supplier-serial-reconciliation/reports/verification.md).

Tài liệu [bảng kê và đối chiếu](../supplier-serial-reconciliation.md) giải thích payload, dữ liệu, luồng và giới hạn. Không sửa tài liệu RP1/RP2 gốc, không tạo PO độc lập hoặc import Excel, không commit/push/deploy.
