# Bảng kê nhà cung cấp và đối chiếu serial khi nhận hàng

Purchasing nhập danh sách thiết bị nhà cung cấp xác nhận giao. Staff vẫn quét thiết bị thực tế; hệ thống so sánh hai danh sách theo SKU và serial. Theo lựa chọn của người dùng, **serial ngoài bảng kê hoặc sai SKU bị chặn, không lưu scan và không tăng số lượng nhận**.

RP1 UC12 đã nêu thông tin sản phẩm được nhà cung cấp xác nhận. Việc lưu đầy đủ serial dự kiến và đối chiếu từng serial là chi tiết bổ sung theo yêu cầu lần này; không khẳng định toàn bộ chi tiết này đã có trong sequence SDS cũ. Nguồn yêu cầu gốc không bị sửa. Phạm vi vẫn là Inbound hiện có và module Người 5, chưa tạo một phân hệ Purchase Order độc lập.

## Luồng để bảo vệ đồ án

1. **Manager lập yêu cầu nhập:** chọn SKU trong danh mục và số lượng dự kiến từng dòng.
2. **Purchasing cập nhật cam kết:** chọn nhà cung cấp, ngày giao và nhập đủ serial cho từng SKU. SKU và số lượng lấy từ kế hoạch, hiển thị chỉ đọc để tránh gõ nhầm hoặc tự đổi sản phẩm đã đặt.
3. **Manager duyệt Inbound:** chỉ duyệt khi nhà cung cấp Confirmed và bảng kê đầy đủ. Sau khi yêu cầu được xử lý, Purchasing không sửa bảng kê.
4. **Staff nhận thực tế:** tạo Draft, chọn SKU rồi quét serial. Mã hợp lệ được lưu vào `GoodsReceiptSerial`; mã không có trong bảng kê hoặc thuộc SKU khác bị chặn.
5. **Staff chọn bin và gửi duyệt:** API kiểm tra lại bảng kê, số lượng, serial và sức chứa.
6. **Manager duyệt Receipt:** API tiếp tục đối chiếu; chỉ lúc này mới tạo serial chính thức, movement, cộng tồn và cập nhật tiến độ Inbound trong cùng transaction.

Khai báo serial là dự kiến từ nhà cung cấp. Nó không chứng minh hàng đã đến kho và không tạo `SerialNumber`, `Stock` hoặc `SerialMovement`.

## Purchasing nhập thế nào?

Mở chi tiết tại `/purchasing/inbound-requests/{id}`. Với phản hồi **Xác nhận cung cấp**, mỗi SKU có một ô serial riêng:

- Dán mỗi serial trên một dòng; dòng trống và khoảng trắng đầu/cuối dòng được bỏ qua.
- Giữ serial dưới dạng chuỗi để không mất số 0 đầu hoặc tự đổi chữ hoa/thường.
- Số mã phải đúng bằng số lượng dự kiến của từng SKU; không lấy mã thừa của SKU này bù SKU khác.
- Giao diện báo số đã nhập, còn thiếu/thừa và serial trùng. API kiểm tra lại toàn bộ dữ liệu.
- Khi chọn **Từ chối cung cấp**, không cần serial; lưu phản hồi này sẽ xóa bảng kê cũ của yêu cầu còn Pending.

Serial mặc định gồm 1–100 ký tự chữ ASCII, số, dấu `.`, `_`, `-`. Quy tắc phía API dùng chung `GoodsReceiptSerialRules`; có thể cấu hình bằng `GoodsReceipts:SerialPattern`. API còn chặn serial đã đăng ký trong kho hoặc được khai báo ở một Inbound khác đang hiệu lực. Trùng mã được so sánh không phân biệt hoa/thường, phù hợp cách kiểm tra đang dùng với SQL.

## Staff nhìn bảng đối chiếu thế nào?

Chi tiết Receipt có phần **Đối chiếu SKU & Serial**, đặt serial nhà cung cấp và serial thực tế đã quét cạnh nhau. Có tìm kiếm theo SKU/serial và lọc trạng thái.

| Giá trị API | Ý nghĩa trên màn hình |
|---|---|
| `AwaitingReceipt` | Chờ tiếp nhận; chưa được một phiếu đang xử lý hoặc đã duyệt giữ mã này |
| `ScannedThisReceipt` | Khớp và đã quét ở phiếu đang mở; chưa đồng nghĩa với tăng tồn |
| `ScannedOtherReceipt` | Đang được xử lý ở phiếu khác; hiển thị mã phiếu để đối chiếu |
| `Received` | Đã nhận chính thức qua Receipt Approved, kể cả đợt trước |

DTO giữ `SerialCode` từ bảng kê và `ScannedSerialCode` từ scan thực tế riêng biệt; không sao chép mã dự kiến thành kết quả đã quét. `ReceiptId` cho biết phiếu ghi nhận.

Nhận nhiều đợt vẫn dùng một bảng kê: serial của đợt đã duyệt hiển thị Received; mã còn lại tiếp tục chờ nhận. Phiếu Rejected không được tính đã nhận, không cộng tồn và không giữ serial cho phiếu đó. Khi xem lịch sử phiếu từ chối, scan cũ vẫn hiện để tra cứu nhưng có nhãn **Không tính · Phiếu từ chối**; Staff có thể tiếp nhận lại bằng phiếu khác.

## API và dữ liệu

`PUT /api/inbound-requests/{id}/supplier-commitment` giữ endpoint cũ, bổ sung `items` khi Confirmed:

```json
{
  "supplierId": 1,
  "commitmentStatus": "Confirmed",
  "expectedDeliveryDate": "2026-12-01T00:00:00",
  "items": [
    { "itemId": 12, "sku": "PHONE-128-BLACK", "serialCodes": ["000SN-A1", "000SN-A2"] }
  ]
}
```

Ví dụ yêu cầu có đúng một dòng `itemId=12`, SKU như trên và `ExpectedQty=2`. ID/SKU phải lấy từ detail thật, không dùng các giá trị minh họa để gửi vào database khác. Request ghi vẫn cần cookie đăng nhập và `X-CSRF-TOKEN`; Controller chỉ cho Purchasing trong kho được phân công.

`GET /api/inbound-requests/{id}` bổ sung `hasSupplierManifest` và `items[].sku`, `productName`, `expectedSerials` dạng danh sách chuỗi. `GET /api/goods-receipts/{id}` bổ sung `hasSupplierManifest` và `items[].expectedSerials` dạng danh sách `{serialCode,status,receiptId,scannedSerialCode}` để Staff/Manager đối chiếu. Những field cũ vẫn được giữ.

Migration `20261105165917_AddInboundSupplierManifest` bổ sung:

- `InboundRequest.HasSupplierManifest`: đánh dấu yêu cầu có bảng kê đã được xác nhận.
- `InboundExpectedSerial`: serial dự kiến và liên kết đến dòng Inbound.
- Composite FK `(InboundRequestItemId, InboundRequestId)` bảo đảm dòng hàng thuộc đúng yêu cầu.
- Unique `(InboundRequestId, SerialCode)` bảo đảm một mã chỉ xuất hiện một lần trong một bảng kê.

Không sửa migration cũ hoặc tự tạo lại các bảng tồn kho. `start-dev.cmd` gọi script build/migration trên database Development riêng; không cần reset mật khẩu hay xóa Receipt để nâng cấp.

## Transaction và cách đọc code

Code giữ DTO class, `if`, `foreach`, map thủ công và DbContext; không thêm AutoMapper, CQRS, MediatR hoặc Unit of Work. Module Inbound giữ Repository đã có của nhóm; phần bảng kê cần cập nhật nhiều bảng dùng DbContext trực tiếp trong Service.

Trong `InboundRequestService`, lưu commitment và thay toàn bộ bảng kê nằm trong một transaction với khóa SQL dùng chung với nhận hàng. Nếu validation hoặc lưu một bước lỗi, tất cả thay đổi được rollback; không để header Confirmed nhưng serial chỉ lưu một phần. Manager review cũng kiểm tra trạng thái/bảng kê dưới khóa đó.

Trong `SerialNumberService`, scan thất bại sẽ rollback và xóa entity đang theo dõi. Trong `GoodsReceiptService.ValidateSupplierManifestAsync`, mọi scan phải thuộc đúng Inbound và SKU. Hàm này được gọi lại khi scan, lưu, submit và approve; tránh việc client bỏ qua giao diện hoặc dữ liệu thay đổi giữa các bước. Transaction Approve Receipt vẫn giữ thứ tự đăng ký serial → movement/tồn → Approved → tiến độ Inbound.

| File cần đọc | Trách nhiệm |
|---|---|
| `Entities/InboundExpectedSerial.cs`, `Data/EWMSDbContext.cs` | Bảng dự kiến và quan hệ dữ liệu |
| `DTOs/InboundRequests/UpdateSupplierCommitmentDto.cs` | Payload bảng kê theo dòng hàng |
| `Services/InboundRequestService.cs` | Validation và lưu bảng kê, điều kiện duyệt Inbound |
| `Services/SerialNumberService.cs` | Quét thật, chặn mã sai/đang ở phiếu khác |
| `Services/GoodsReceiptService.cs` | Kiểm tra lại và map trạng thái đối chiếu |
| `features/inbound/SupplierCommitmentForm.tsx` | Nhập nhiều serial theo SKU |
| `features/inbound/SupplierManifestPanel.tsx` | Bảng kê đã lưu, tìm kiếm và xem lịch sử |
| `features/goods-receipts/ReceiptSerialComparison.tsx` | So sánh dự kiến với thực nhận |

## Lỗi thường gặp và dữ liệu cũ

| Mã | Cách xử lý |
|---|---|
| `IN_MANIFEST_REQUIRED` | Purchasing phải xác nhận và khai báo đủ bảng kê trước khi duyệt |
| `IN_MANIFEST_ITEMS`, `IN_MANIFEST_SKU` | Kiểm tra đúng dòng hàng/SKU thuộc yêu cầu |
| `IN_MANIFEST_COUNT` | Nhập đúng số serial cho từng SKU |
| `IN_MANIFEST_DUPLICATE`, `IN_MANIFEST_SERIAL_IN_USE` | Bỏ mã trùng hoặc kiểm tra đơn/tồn kho đang giữ mã |
| `GR_SERIAL_NOT_EXPECTED` | Serial không có trong bảng kê của Inbound; không được lưu scan |
| `GR_SERIAL_WRONG_SKU` | Chọn đúng SKU cho serial rồi quét lại |
| `GR_SERIAL_IN_ANOTHER_RECEIPT` | Kiểm tra phiếu khác đang xử lý serial |

Inbound đã Approved/PartiallyReceived/Completed từ trước migration có cờ false. Hệ thống hiển thị rõ chưa có bảng kê để đối chiếu, giữ lịch sử và việc nhận tiếp của các yêu cầu này. Không tạo danh sách dự kiến giả từ scan thực tế. Yêu cầu mới phải qua điều kiện xác nhận bảng kê trước khi Manager duyệt.

## Kiểm chứng

API và project test build không warning/error. **80/80 test SQL/HTTP qua**, gồm 61 test cũ và 19 ca mới cho bảng kê/đối chiếu; log tại `EWMS/EWMS.Api.Tests/obj/manifest-full-result.log`. Đã kiểm tra rollback, mã sai không đổi số lượng/Version, SKU sai, serial ở phiếu khác, nhận hai đợt, từ chối/nhận lại và quyền API. React build/lint qua, bản build gồm 44 modules.

Browser QA đã chạy đủ ba role, quét mã sai/đúng, nhận hai đợt, khóa bảng kê và từ chối/nhận lại; xem [báo cáo kiểm chứng](../plans/20261105-supplier-serial-reconciliation/reports/verification.md). Migration đã áp vào database Development riêng; bản local5173/5085 đã chạy lại, giữ dữ liệu và mật khẩu. Chưa commit, push hoặc deploy tính năng này.
