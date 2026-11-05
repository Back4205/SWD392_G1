# Hướng dẫn triển khai phần Người 5 trong EWMS

Tài liệu làm việc cho coding agent và người triển khai, ngày 03/11/2026.

**Mục tiêu:** hoàn thành module Goods Receipt, Serial/IMEI và Manager Approval trên nền entity và migrations đã có; tuân theo kiến trúc SDS và luồng Import Goods. Có thể xây dựng, chạy và kiểm thử module bằng dữ liệu phát triển độc lập, sau đó thay các điểm kết nối bằng implementation của thành viên khác.

**Trạng thái xác minh:** đã đọc nội dung và xem hình swimlane, class diagram, sequence liên quan trong các tài liệu đính kèm. Chưa được cung cấp repository nên chưa xác minh namespace, framework version trong `.csproj`, entity thực tế, migration, enum, route và cấu trúc frontend. Những tên file bổ sung, interface và DTO bên dưới là hướng dẫn triển khai, không phải khẳng định code hiện tại đã có.

## 1. Nguồn yêu cầu và phạm vi chính xác

### 1.1 Tài liệu đầu vào

1. `SWD392 - Report 1 (Overview & SRS) - SE1972-NET  - Group 1 _v1.1(1).docx`, gọi là SRS.
2. `SWD392 - Report 2 (SDS) - SE1972-NET - Group 1 _v1.0(1).docx`, gọi là SDS.
3. `Văn bản đã dán (1)(1).txt`, gọi là phân công. File này chứa **hai phương án phân công**. Áp dụng phương án cuối, có tiêu đề “Bảng phân công mới cho 5 người”.

Không lấy dòng số 5 trong danh sách thành viên để suy ra danh tính người dùng. “Người 5” trong tài liệu này là vai trò triển khai module theo phân công cuối.

### 1.2 Phân công cuối đang áp dụng

| Người | Module sở hữu | Phụ thuộc của Người 5 |
| --- | --- | --- |
| 1 | Product và Supplier | ProductVariant ID, SKU, WeightKg, sản phẩm hợp lệ |
| 2 | Warehouse, Section, Rack, Bin, Stock | Vị trí, sức chứa, nghiệp vụ cộng Stock |
| 3 | Authentication và User | Current user, role, warehouse access |
| 4 | Inbound Request và Supplier Approval | Inbound được duyệt, expected quantity, cập nhật tiến độ nhận |
| **5** | **Goods Receipt, Serial/IMEI, Receipt Approval** | **Tạo phiếu → quét → chọn bin → gửi duyệt → Manager duyệt/từ chối** |

Người 5 sở hữu FE và BE của module nhận hàng; `GoodsReceipt`, `GoodsReceiptItem`, dữ liệu scan và đăng ký Serial thuộc luồng này. Việc duyệt là một phần của module Người 5. Không chỉ triển khai approve/reject như phương án đầu của file phân công.

Không xây CRUD Product, Supplier, Warehouse, User hay toàn bộ Inbound Workflow. Không xây Outbound, Transfer, Inventory Count. Không triển khai in tem hay email yêu cầu in tem trong phạm vi phân công này.

### 1.3 Bản đồ truy vết bắt buộc

| Chức năng | Use case trong SRS | SDS liên quan | Kết quả bắt buộc |
| --- | --- | --- | --- |
| Danh sách phiếu | UC-15 | 3.3.2.1 | Danh sách hoặc empty state |
| Chi tiết phiếu | UC-16 | 3.3.2.2 | Dữ liệu phiếu hoặc lỗi không tìm thấy |
| Tạo phiếu | UC-17 | 3.3.2.3 | Phiếu DRAFT từ Inbound còn được nhận |
| Lưu nháp và gửi duyệt | UC-18 | 3.3.2.4 | SAVE_DRAFT hoặc SUBMIT, chưa tăng Stock |
| Nhập và kiểm tra Serial | UC-50 | 3.3.2.5 | Scan hợp lệ, duy nhất; đếm lại số lượng |
| Manager duyệt/từ chối | UC-56 | **3.3.2.6** | Approved với cập nhật nguyên tử, hoặc Rejected có lý do |
| Tiến độ nhận của Inbound | UC-53 còn có trong SRS đính kèm | Không có sequence riêng trong 3.3.2 của bản SDS này | Phần đọc tiến độ thuộc Người 4; Người 5 gọi refresh sau approve |

**Số thứ tự trong file SDS đính kèm là 3.3.2.6 cho Approve or Reject Goods Receipt. Không tự ghi là 3.3.2.7.** Nếu repository có tài liệu mới hơn, xác minh bằng tiêu đề và nội dung sequence, không chỉ dựa vào số mục.

Track Goods Receipt Progress không phải bước trung gian để chuyển phiếu sang Manager. Sau SUBMIT, Manager xem ngay danh sách Pending Approval. Dù không tạo màn hình UC-53 ở module Người 5, hệ thống vẫn phải tính remaining và cập nhật trạng thái Inbound sau approve.

## 2. Các chỗ tài liệu chưa đồng bộ và cách triển khai

Không thể khẳng định mọi câu trong SRS và SDS hiện tại đồng thời nhất quán. Agent phải giữ bảng đối chiếu này trong báo cáo triển khai; không âm thầm chọn luồng cũ.

| Điểm chưa đồng bộ | Bằng chứng | Hướng xử lý cho module này |
| --- | --- | --- |
| Staff confirm tăng Stock ngay | SRS UC-17 main sequence bước 9, postconditions; Non-UI Inbound Stock Increment; SDS class specification `confirmReceipt` cũ | Dùng swimlane Import Goods, UC-56 và sequence 3.3.2.4/3.3.2.6: Staff chỉ submit; Manager approve mới ghi tồn |
| Receipt Completed ngay khi tạo | SRS UC-17 postcondition cũ | Sequence 3.3.2.3 tạo DRAFT; trạng thái hoàn tất nhận đủ là của **InboundRequest**, không gán Completed cho receipt mới |
| Incomplete và Partially Received | SRS BR-09 so với swimlane và phân công cuối | Ngữ nghĩa là nhận một phần. Map sang enum/string thực có; dùng một giá trị chuẩn, không lưu hai cách viết song song |
| In tem và email in tem | SRS UC-17, BR-13 và Non-UI còn mô tả | Phân công đính kèm ghi không triển khai in tem. Loại khỏi phạm vi này và ghi cần cập nhật mô tả cũ |
| Scan tạm chưa có bảng trong database design | Sequence 3.3.2.5 có `GoodsReceiptScanRepository`, trả `GoodsReceiptSerial`; mục database chỉ mô tả SerialNumber chính thức | Kiểm tra code có staging tương đương chưa. Thiếu thì thực hiện bổ sung tối thiểu theo mục 5; không đưa scan nháp vào IN_STOCK |
| Version chưa được mô tả trong bảng GoodsReceipt | Sequence 3.3.2.4 có `saveDraftWithVersion`, `submitWithVersion` | Kiểm tra concurrency token hiện hữu. Thiếu thì bổ sung có kiểm soát; không bỏ kiểm tra stale write |
| Lý do từ chối receipt chưa có trong bảng GoodsReceipt | UC-56 bắt buộc reason nhưng SDS 2.2.18 chỉ có 5 field | Tái sử dụng field/audit cấu trúc đã có; nếu thiếu cần bổ sung lưu bền, đọc lại được |
| Read sequence bỏ qua service | 3.3.2.1/3.3.2.2 vẽ Controller → Repository; package diagram là Controller → Service → Repository | Giữ đúng actor, API, truy vấn, kết quả và nhánh alt. Dùng service mỏng cho read để theo kiến trúc phân lớp; ghi rõ đây là bước chi tiết hóa kiến trúc cần đồng bộ hình |
| Manager UC diagram chưa thể hiện Receipt Approval | SRS hình UCs for Manager cũ so với UC-56 và swimlane | Giữ Manager là người duyệt theo UC-56; ghi nợ cập nhật diagram, không chuyển quyền duyệt cho Staff |

Quy tắc áp dụng cho yêu cầu này: giữ kiến trúc phân lớp SDS; với vòng đời nhận hàng, lấy swimlane Import Goods và sequence Goods Receipt mới làm luồng thi hành. Các chỗ bổ sung kỹ thuật trong file này không được giới thiệu là nguyên văn tài liệu gốc.

Nếu code hiện tại có quy tắc nghiệp vụ trái với luồng đã xác định, cô lập thay đổi trong module này, báo rõ tác động. Chỉ dừng phần thực sự không thể xác định như chính sách tái sử dụng serial của phiếu bị từ chối; tiếp tục các phần độc lập còn lại. Không tự thêm Cancel/Reopen/Resubmit từ Rejected vì nguồn không vẽ các nhánh đó.

## 3. Chỉ thị mở đầu cho coding agent

Bạn là coding agent triển khai module Người 5. Hãy thực hiện công việc, không chỉ trả về kế hoạch. Dùng tài liệu này như checklist nghiệp vụ và luôn kiểm tra repository trước khi tạo file.

1. Đọc `AGENTS.md`, README, solution, `.csproj`, frontend `package.json`, cấu hình và tài liệu dự án có trong repository.
2. Xác định entry project, namespace, `DbContext`, cấu trúc controller/service/repository, response/error convention, authorization convention, test project và cách chạy hiện hữu.
3. Đọc entity, EF configuration, migration snapshot và migration history liên quan. Người dùng cho biết đã migrations toàn bộ entity: **không scaffold lại, không xóa database, không tạo lại toàn bộ entity, không sửa migration đã áp dụng**.
4. Tạo bảng mapping “thiết kế → class/property/enum hiện có → thiếu gì”. Tối thiểu kiểm tra tất cả entity ở mục 5.
5. Giữ nguyên framework version repo đang sử dụng; SDS yêu cầu React 18, ASP.NET Core Web API, EF Core, SQL Server, không có yêu cầu chuyển sang framework khác.
6. Trước khi ghi code, ghi ngắn gọn kế hoạch theo phase và các sai lệch nguồn đã phát hiện. Sau đó triển khai từng phase có xác minh.
7. Không ghi secret, connection string thật, token thật vào source. Dùng cấu hình local của dự án.
8. Chỉ sửa file chung khi thật sự cần DI, routing, DbContext hoặc cấu hình. Giữ diff nhỏ, không format lại cả file; gom đăng ký module thành extension riêng.
9. Không đổi public contract của module khác để tiện cho mình. Tạo interface và adapter trong module Người 5 nếu dependency chưa sẵn sàng.
10. Khi xong, báo file thay đổi, cách chạy, test đã chạy, kết quả thật/fake và phần chưa thể tích hợp. Không báo hoàn thành end-to-end production nếu còn fake hoặc schema gap.

## 4. Kiến trúc triển khai theo SDS

### 4.1 Backend

Luồng write bắt buộc: React → API Services → HTTP API → Controller → Service → Repository → Data/DbContext → SQL Server.

Package diagram SDS có `Controllers`, `DTOs`, `Services`, `Repositories`, `Data`, `Entities`, `Integrations`, `Common`. Dùng những package này trong project hiện hữu; không bắt buộc tách thêm nhiều assembly, không tự đưa CQRS, event bus hoặc microservice vào dự án.

| Thành phần | Trách nhiệm | Không đặt ở đây |
| --- | --- | --- |
| GoodsReceiptController | Bind request, authorization, gọi service, trả response | Công thức Stock, EF query trực tiếp, tự commit nghiệp vụ |
| GoodsReceiptService | List/detail/create/update/submit, điều kiện phiếu | Tự authenticate user hoặc quản trị Inbound |
| SerialNumberService | Kiểm tra scan, uniqueness, recount scan hợp lệ | Đưa serial nháp vào tồn chính thức |
| GoodsReceiptApprovalService | Điều phối approve/reject và transaction | CRUD Stock của toàn hệ thống |
| StockService hoặc adapter tương ứng | Cộng on-hand, kiểm tra capacity thông qua Người 2 | Commit transaction riêng giữa chừng |
| InboundRequestService hoặc adapter tương ứng | Refresh tiến độ sau receipt approved | Nhận ReceivedQty thiếu kiểm chứng từ FE |
| Repository | Đọc/ghi entity, query scope và persistence | Quyết định actor nào được duyệt |
| Data | DbContext và EF mapping | Xử lý UI |
| Common | Error/result, current-user contract, hằng status khi repo đã dùng | Nhân bản entity và DTO của module khác |

Trong class diagram SDS có `StockUpdateService`; sequence mới gọi `StockService`. Giữ một implementation cập nhật tồn được dùng chung. Có thể adapter `StockUpdateService` ủy quyền vào `StockService`, không tạo hai đường cộng tồn độc lập.

Các method kiểu Java như `findById`, `Optional<T>`, `saveAndFlush` trong hình được chuyển sang C# async và kiểu nullable phù hợp. Giữ ngữ nghĩa và thứ tự nghiệp vụ, không sao chép cú pháp Java vào C#.

### 4.2 Cấu trúc file gợi ý

Áp dụng tên thư mục hiện có nếu khác. Cây dưới đây chỉ biểu diễn vị trí trách nhiệm.

```text
Backend/
  Controllers/GoodsReceiptController.cs
  DTOs/GoodsReceipts/
    CreateGoodsReceiptRequest.cs
    UpdateGoodsReceiptRequest.cs
    SubmitGoodsReceiptRequest.cs
    AddReceiptSerialRequest.cs
    ApproveGoodsReceiptRequest.cs
    RejectGoodsReceiptRequest.cs
    GoodsReceiptResponse.cs
    ReceiptSerialResponse.cs
  Services/GoodsReceipts/
    GoodsReceiptService.cs
    SerialNumberService.cs
    GoodsReceiptApprovalService.cs
    GoodsReceiptModuleRegistration.cs
  Services/GoodsReceipts/Contracts/
    ICurrentReceivingUser.cs
    IInboundReceivingReader.cs
    IStockReceivingWriter.cs
    IInboundReceivingProgressWriter.cs
    IReceivingCapacityChecker.cs
  Repositories/
    GoodsReceiptRepository.cs
    GoodsReceiptScanRepository.cs
    SerialNumberRepository.cs
  Integrations/GoodsReceipts/
    ExistingInboundAdapter.cs
    ExistingStockAdapter.cs
  Data/                     # Tái sử dụng DbContext và mapping đã có
  Entities/                 # Tái sử dụng entity đã có
  Common/                   # Tái sử dụng response, errors, authorization
Tests/
  GoodsReceiptUnitTests/
  GoodsReceiptSqlServerIntegrationTests/
Frontend/src/
  pages/goods-receipts/
  components/goods-receipts/
  hooks/
  services/goodsReceiptService.js
  api/                      # Dùng HTTP client sẵn có
  utils/
```

Interface đặt theo convention của repo. Không tạo class mới trùng tên chỉ vì ví dụ trên khác cấu trúc repo.

### 4.3 Frontend

Theo SDS: `Pages`, `Components`, `Hooks`, `API Services`, `API`, `Utils`. Giữ logic nghiệp vụ quyết định ở backend. UI nền sáng, tiếng Việt mặc định; dùng cơ chế chuyển ngôn ngữ có sẵn nếu repo đã triển khai. Không mở rộng thành dự án redesign toàn hệ thống.

Tên `.html` trong sequence là boundary logic của màn hình; hiện thực bằng React page/component, không tạo ứng dụng HTML thứ hai.

## 5. Entity và khoảng trống cần kiểm tra trước khi code

### 5.1 Mapping theo database design SDS

| Entity | Field liên quan trong SDS | Cách dùng |
| --- | --- | --- |
| GoodsReceipt | Id, InboundRequestId, ReceivedByUserId, ReceivedDate, Status | Header và state phiếu |
| GoodsReceiptItem | Id, GoodsReceiptId, ProductVariantId, BinId, ReceivedQty | Dòng nhận và vị trí |
| InboundRequest | Id, WarehouseId, SupplierId, Status | Xác nhận kho và điều kiện được nhận |
| InboundRequestItem | Id, InboundRequestId, ProductVariantId, ExpectedQty | Expected của từng variant |
| ProductVariant | Id, ProductId, SKU, WeightKg | SKU và tải trọng |
| StorageBin | Id, RackId, MaxWeightKg | Kiểm tra sức chứa bin |
| StorageRack | Id, SectionId, MaxWeightKg | Kiểm tra sức chứa rack |
| StorageSection | Id, WarehouseId | Truy ra warehouse của bin |
| Warehouse | Id, MaxWeightKg, Status | Quyền kho, sức chứa tổng nếu module kho thực thi |
| Stock | Id, ProductVariantId, BinId, OnHandQty, ReservedQty | Tăng OnHandQty, giữ ReservedQty |
| SerialNumber | Id, ProductVariantId, OriginGoodsReceiptItemId, CurrentBinId, SerialCode, CurrentStatus | Serial chính thức sau approve |
| SerialMovement | SerialNumberId, FromBinId, ToBinId, PerformedByUserId, MovementType, RefType, RefId, MovementDate | Lịch sử nhập từng serial |
| User và UserWarehouse | UserId, WarehouseId, trạng thái và role liên quan | User đang thao tác, phạm vi kho |
| AuditLog | UserId, Action, EntityName, EntityId, OldValue, NewValue, CreatedAt | Lưu dấu quyết định theo khả năng repo |

SDS dùng ID `int` trong bảng; một số class diagram ghi `long`. **Lấy kiểu entity/DB thực tế làm chuẩn**, không đổi tất cả ID theo hình. DTO, route binding, interface phải thống nhất với kiểu đó.

### 5.2 Dữ liệu scan tạm

Sequence 3.3.2.5 lưu qua `GoodsReceiptScanRepository`; sequence approve mới gọi `saveApprovedSerialsAndMovements`. Vì vậy cần phân biệt dữ liệu đã scan và serial đã nhập kho.

- Nếu repo đã có `GoodsReceiptSerial`, `GoodsReceiptScan` hoặc bảng tương đương: dùng lại.
- Nếu chỉ có `SerialNumber`, kiểm tra nó có cơ chế staging thật sự phù hợp với sequence không. Không mặc định rằng thêm một status tùy ý vào bảng chính là đủ.
- Nếu thiếu: bổ sung entity/mapping/migration **tối thiểu** cho staging. Tên khuyến nghị `GoodsReceiptSerial` để khớp response trong hình; repository có thể là `GoodsReceiptScanRepository`.
- Các field đề xuất, không phải nguyên văn SDS: ID, GoodsReceiptItemId, SerialCode, giá trị chuẩn hóa nếu cần, thời điểm scan và user scan nếu cần audit.
- Persist bền trên SQL Server để refresh trang hoặc restart API không mất scan. Không dùng dictionary singleton thay cho staging trong đường chạy thật.
- Mỗi mã scan phải thuộc đúng receipt item. ReceivedQty được tính từ số scan được chấp nhận, không cộng theo mỗi lần FE gửi request.

### 5.3 BinId không nullable trong SDS

Swimlane cho quét trước rồi chọn bin, nhưng `GoodsReceiptItem.BinId` trong database design là bắt buộc. Đây là điểm cần xử lý, không được chèn BinId = 0 hoặc ID giả.

1. Kiểm tra entity thực tế đã cho nullable khi DRAFT chưa.
2. Nếu có staging scan gắn với draft item, draft item phải có cách tồn tại trước khi chọn bin.
3. Nếu code chưa hỗ trợ: bổ sung tối thiểu `BinId` nullable cho trạng thái nháp trong migration bổ sung, hoặc sử dụng staging draft-item đã có và chỉ tạo dòng chính khi chọn bin. Chọn một cách, ghi rõ mapping và kiểm thử.
4. Điều kiện submit/approve vẫn bắt buộc mọi dòng có số lượng nhận > 0 phải có bin thật, đúng kho.

Không đổi thứ tự nghiệp vụ thành “bắt buộc chọn bin trước khi quét” chỉ để né thiếu hụt schema. Tách lưu nháp chưa hoàn chỉnh khỏi validation gửi duyệt hoàn chỉnh.

### 5.4 Lưu quyết định và concurrency

- Kiểm tra `RejectionReason`, approver và timestamp hiện có. Nếu không có, có thể dùng audit cấu trúc hiện hữu để lưu và đọc lại quyết định; nếu audit không đáp ứng thì bổ sung field tối thiểu cho receipt. Không lưu reason chỉ trong FE hoặc log console.
- Sequence update/submit yêu cầu version. Dùng rowversion/concurrency token đã có; nếu thiếu, bổ sung token và trả về dạng mà frontend gửi lại được, ví dụ Base64.
- Quét serial và chỉnh bin phải làm thay đổi version của aggregate receipt, hoặc cơ chế version phải bao trùm thay đổi con. Chỉ có rowversion ở header nhưng không update header khi sửa item/scan sẽ không đủ chống ghi đè.
- Unique cho SerialNumber.SerialCode đã được SDS chỉ ra. Stock nên có uniqueness trên `(ProductVariantId, BinId)` nếu chưa có; đây là ràng buộc kỹ thuật bổ sung để tránh tạo hai dòng cùng khóa nghiệp vụ.
- Thêm unique cho scan trong cùng receipt và chiến lược ngăn hai scan đồng thời. Việc khóa serial giữa các draft khác nhau và việc giải phóng khi reject cần một chính sách rõ ở mục 8.

Nếu cần bổ sung migration, đặt tên riêng cho phần nhận hàng, xem SQL/diff trước khi áp dụng trên DB development. Không drop/recreate DB; không sửa các migration lịch sử; không tự động chạy migration vào môi trường dùng chung. Không thêm field RemainingQty nếu có thể tính từ dữ liệu gốc.

## 6. Vòng đời và quyền thao tác

Tên trạng thái dưới đây là ký hiệu logic. Map một lần sang enum/string thật trong repo.

| Đối tượng | Từ | Hành động | Đến | Hiệu ứng |
| --- | --- | --- | --- | --- |
| Receipt | Chưa có | Staff tạo từ Inbound được nhận | DRAFT | Chưa tăng Stock |
| Receipt | DRAFT | Lưu, scan, chọn bin | DRAFT | Chưa có Serial IN_STOCK |
| Receipt | DRAFT | Staff submit hợp lệ | PENDING_APPROVAL | Khóa chỉnh sửa; Manager có thể xem duyệt |
| Receipt | PENDING_APPROVAL | Manager reject với reason | REJECTED | Không tăng Stock, không ghi serial chính thức |
| Receipt | PENDING_APPROVAL | Manager approve hợp lệ | APPROVED | Ghi Serial, movement, Stock, tiến độ trong một transaction |
| Inbound | APPROVED | Phiếu đầu được duyệt nhưng còn thiếu | PARTIALLY_RECEIVED | Cho phép nhận tiếp số còn thiếu |
| Inbound | PARTIALLY_RECEIVED | Duyệt thêm nhưng vẫn thiếu | PARTIALLY_RECEIVED | Tính lại remaining |
| Inbound | APPROVED hoặc PARTIALLY_RECEIVED | Mọi dòng nhận đủ | COMPLETED | Chặn tạo/duyệt thêm vượt expected |

**Không có đường Staff SUBMIT → tăng Stock. Không có đường REJECTED → APPROVED hoặc REJECTED → DRAFT trong nguồn hiện tại.**

| Actor | Hành động được thực hiện trong phạm vi kho được phép |
| --- | --- |
| Warehouse Staff | Xem danh sách/chi tiết, tạo nháp, sửa nháp được phép, scan, chọn bin, submit |
| Manager | Xem pending list và detail để review; approve hoặc reject |
| Purchasing Manager | Đọc tiến độ qua phần Inbound nếu được cấp quyền; không duyệt Receipt |

Quyền Manager xem receipt pending là điều kiện cần của swimlane Review Goods Receipt, dù read sequence cũ minh họa actor Staff. Không cấp Admin hoặc Sales Staff quyền duyệt mặc định nếu permission của hệ thống không quy định.

Mỗi API kiểm tra server-side: user tồn tại/active, role/permission, warehouse membership, quyền thao tác receipt và trạng thái hiện tại. Không lấy `ReceivedByUserId`, `ApprovedByUserId` hoặc role từ request FE. Dùng current user của request.

SDS không xác định rõ Staff được sửa mọi draft cùng kho hay chỉ draft mình tạo. Dùng ownership policy hiện có. Khi chưa có, có thể cấu hình mặc định phát triển chỉ creator được sửa và ghi là giả định cần xác nhận; không âm thầm coi đây là business rule gốc.

## 7. Luồng chi tiết đúng theo sequence

### 7.1 UC-15 và UC-16

1. UI gọi GET danh sách hoặc GET chi tiết đúng route trong mục 9.
2. Backend lọc warehouse access trước khi trả dữ liệu; không tải toàn bộ rồi chỉ ẩn trên UI.
3. Danh sách hỗ trợ paging/filter theo convention SRS và repo; giữ search/filter trong URL FE.
4. Không có dữ liệu: trả danh sách rỗng hợp lệ và hiển thị empty state; không phải lỗi 500.
5. ID không tồn tại: trả lỗi 404. ID ngoài quyền kho: trả theo convention authorization của repo, không rò rỉ chi tiết.
6. Detail gồm items, SKU, bin, scan count, expected/approved-received/remaining, status, version, quyết định nếu có.

### 7.2 UC-17 Create Goods Receipt

1. Staff kiểm tra hàng thực tế, chọn Inbound đã được duyệt và còn thiếu. “Hàng đã đến kho” là tiền điều kiện thực tế, không tự suy ra bằng thời gian hệ thống.
2. `POST /api/goods-receipts` với `CreateGoodsReceiptRequest`.
3. Controller gọi `GoodsReceiptService.CreateReceiptAsync(request, currentUser)`.
4. Service đọc Inbound qua repository/adapter tương đương `findById`.
5. Không tồn tại hoặc status không được nhận: lỗi MSG91.
6. Hợp lệ nhưng mọi remaining = 0: lỗi MSG92; không tạo phiếu.
7. Có remaining > 0: lưu header DRAFT, user nhận từ current user, liên kết đúng Inbound.
8. Tải/cấu tạo draft lines theo khả năng schema; giữ nguyên ProductVariant đã yêu cầu, không cho FE tùy ý thêm SKU khác.
9. Trả `GoodsReceiptResponse`; hiển thị thành công MSG98.

Cho phép nhận tiếp từ PARTIALLY_RECEIVED vì swimlane có nhánh nhận một phần và phân công có giao hàng nhiều đợt. Nếu enum hiện tại chưa hỗ trợ, bổ sung mapping; không đổi status Inbound về Approved để lách kiểm tra.

### 7.3 UC-50 Enter and Validate Serial

1. Staff scan hoặc nhập mã trong một dòng sản phẩm thuộc receipt DRAFT.
2. `POST /api/goods-receipts/{id}/items/{itemId}/serials` với `AddReceiptSerialRequest`.
3. Controller gọi `SerialNumberService.AddSerialAsync(id, itemId, request, currentUser)`.
4. Đọc receipt; kiểm tra draft, quyền user, item thuộc receipt, variant thuộc Inbound và SKU đúng.
5. Validate mã theo chính sách sản phẩm/repo. SDS chưa quy định regex serial, độ dài serial tối thiểu hay thuật toán IMEI; không tự bắt tất cả serial phải là 15 chữ số. Nếu thiếu policy, tạo validator có thể cấu hình, ghi rõ rule dùng trong demo.
6. Mã sai: trả validation error; không lưu scan, không tăng count.
7. Mã hợp lệ: repository thực hiện `addUniqueSerialAndRecount` nguyên tử.
8. Mã trùng scan hoặc trùng SerialNumber chính thức: lỗi duplicate, không tăng count.
9. Thành công: persist staging, cập nhật ReceivedQty từ count, thay đổi aggregate version; trả `ReceiptSerialResponse` cùng count/version.

Trong swimlane, invalid SKU/Serial đi tới **Reject Goods** và kết thúc nhánh đó. Không nhầm với Manager Reject Goods Receipt. API phải từ chối dữ liệu không hợp lệ; UI không được cho lô lỗi tiếp tục submit như thể hợp lệ. Chính sách sửa một scan lỗi hoặc nhận riêng phần hàng hợp lệ chưa được mô tả đầy đủ: dùng rule đã có nếu có, không tự thêm acceptance workflow cho lô lỗi. Có thể tiếp tục kiểm thử bằng một phiếu/dữ liệu hợp lệ khác.

### 7.4 Chọn bin và lưu nháp

1. Sau scan hợp lệ, Staff chọn bin cho từng dòng có nhận hàng.
2. Kiểm tra đường dẫn Bin → Rack → Section → Warehouse đúng Inbound warehouse.
3. Validate tải trọng dự kiến của cả receipt, có cộng gộp các dòng cùng bin/rack.
4. `PUT /api/goods-receipts/{id}` với `UpdateGoodsReceiptRequest` và expected version.
5. Controller → `UpdateDraftAsync` → đọc phiếu → `validateEditConditions`.
6. Chỉ DRAFT được sửa. Version cũ trả conflict, không ghi đè âm thầm.
7. Lưu nháp hợp lệ với `saveDraftWithVersion`; giữ DRAFT, chưa cộng tồn.

Draft có thể chưa đủ dữ liệu để submit, nhưng dữ liệu đã nhập không được sai kiểu, sai quan hệ, mã trùng hoặc quantity âm. FE không được điều khiển ReceivedQty; server trả count đúng từ scan.

SDS không chốt cách chia một variant vào nhiều bin trong một receipt. Nếu repo hỗ trợ nhiều dòng cùng variant thì tổng remaining được kiểm tra theo variant, staging gắn đúng dòng. Nếu repo chỉ một dòng/variant, không tự mở rộng chức năng chia bin; ghi rõ giới hạn đó.

### 7.5 UC-18 Submit

1. UI phải lưu thành công thay đổi draft trước khi submit, hoặc backend có contract rõ để lưu và submit nguyên tử. Sequence hiện tại validate submission trên receipt đã lưu; không giả định payload submit tự lưu mọi thay đổi trên màn hình.
2. `POST /api/goods-receipts/{id}/submit` với expected version.
3. Controller → `SubmitReceiptAsync` → load → `validateEditConditions`.
4. `validateSubmission`: ít nhất một dòng có nhận; count serial khớp; mỗi dòng nhận có bin; SKU/serial hợp lệ; quantity không vượt remaining; capacity hợp lệ.
5. Dữ liệu sai: trả lỗi và giữ DRAFT, không tăng tồn.
6. Đúng: `submitWithVersion` chuyển PENDING_APPROVAL, khóa edit/scan, trả response.
7. Phiếu xuất hiện ngay trong pending list của Manager. Không cần Purchasing Manager bấm Track Progress hoặc chuyển tay.

### 7.6 UC-56 Manager Approve

Thứ tự dưới đây được chép lại theo ý nghĩa các message trong sequence SDS 3.3.2.6. Toàn bộ write phải nằm trong cùng transaction, kể cả refresh Inbound.

1. Manager xem detail, gửi `POST /api/goods-receipts/{id}/approve` với `ApproveGoodsReceiptRequest`.
2. Controller gọi `GoodsReceiptApprovalService.ApproveReceiptAsync(id, request, currentUser)`.
3. Bắt đầu transaction rồi thực hiện `findByIdForUpdate`. Khóa/kiểm soát đồng thời trước khi validate quyết định.
4. `validateDecision`: tồn tại, Manager có quyền kho, status PENDING_APPROVAL, version hợp lệ nếu contract dùng version.
5. Sai: trả ReceiptApprovalException theo error convention; rollback mọi thay đổi.
6. `validateApproval`: tái kiểm tra tất cả items, staging serial, remaining và capacity ngay trong transaction. Dữ liệu hợp lệ tại thời điểm submit chưa bảo đảm vẫn hợp lệ lúc approve.
7. `SerialNumberRepository.saveApprovedSerialsAndMovements(receipt)`: tạo serial chính thức IN_STOCK, gắn origin item/current bin, tạo receipt movement.
8. `StockService.applyApprovedReceipt(receipt)`: tăng on-hand từng `(variant, bin)`, không thay reserved.
9. `GoodsReceiptRepository.saveApprovedAndFlush(receipt, currentUser)`: đánh dấu APPROVED và persist trong transaction để query tiến độ thấy receipt này.
10. `InboundRequestService.refreshReceivingProgress(receipt.InboundRequestId)`: tính lại theo các receipt APPROVED; cập nhật PARTIALLY_RECEIVED hoặc COMPLETED.
11. Commit transaction; sau đó trả `GoodsReceiptResponse` và UI refresh detail/list.

`Flush`/`SaveChanges` tại bước 9 không có nghĩa commit transaction. Nếu bước 10 lỗi, phải rollback cả Serial, movement, Stock và trạng thái receipt. Không trả success trước commit.

### 7.7 UC-56 Manager Reject

1. Manager mở hộp thoại bắt buộc nhập reason.
2. `POST /api/goods-receipts/{id}/reject` với `RejectGoodsReceiptRequest`.
3. Approval service mở transaction, load for update và `validateDecision` như approve.
4. Trim reason; reject nếu rỗng hoặc vượt độ dài field thực tế. Không sao chép giới hạn của Inbound RejectionReason sang receipt khi chưa xem schema.
5. `saveRejected(receipt, reason, currentUser)` chuyển REJECTED và lưu quyết định bền vững.
6. Commit, trả receipt response.

Nhánh reject **không gọi** `saveApprovedSerialsAndMovements`, `applyApprovedReceipt` hay refresh làm tăng received quantity. Giữ scan phục vụ audit; không có serial IN_STOCK từ phiếu này. Không tự cho sửa/gửi lại receipt bị reject nếu chưa có yêu cầu mới.

## 8. Các bất biến dữ liệu bắt buộc

### 8.1 Quantity và tiến độ nhận

Đối với mỗi InboundRequest và ProductVariant:

```text
Expected(v) = tổng ExpectedQty của InboundRequestItem thuộc variant v
ApprovedReceived(v) = tổng ReceivedQty của GoodsReceiptItem thuộc v
                      trong các GoodsReceipt APPROVED của đúng Inbound
Remaining(v) = Expected(v) - ApprovedReceived(v)
CurrentReceiptQty(v) = tổng số scan hợp lệ của receipt đang duyệt cho v
```

- Không cộng DRAFT, PENDING_APPROVAL hoặc REJECTED vào ApprovedReceived.
- Khi approve, `CurrentReceiptQty(v) <= RemainingBeforeApproval(v)` với mọi v.
- Remaining âm là lỗi integrity, không dùng `max(0, ...)` để che lỗi nhận vượt.
- COMPLETED chỉ khi mọi variant remaining = 0; không so tổng số lượng chung để bù thiếu SKU này bằng dư SKU khác.
- PARTIALLY_RECEIVED khi đã nhận chính thức một phần và ít nhất một variant còn thiếu.
- Không tạo field ReceivedQty do người dùng nhập độc lập với scan count.
- Không tạo cột remaining chỉ để khớp câu “Update Remaining Quantity” trong swimlane; có thể tính động. Nếu repo đã có cột tổng hợp thì cập nhật đồng bộ trong cùng transaction.

### 8.2 Stock và tải trọng

```text
NewOnHand(v, b) = OldOnHand(v, b) + ReceiptQty(v, b)
NewReserved(v, b) = OldReserved(v, b)
CurrentBinWeight(b) = tổng Stock.OnHandQty(v, b) * ProductVariant.WeightKg(v)
IncomingBinWeight(b) = tổng ReceiptQty(v, b) * WeightKg(v)
CurrentRackWeight(r) = tổng CurrentBinWeight của mọi bin trong rack r
IncomingRackWeight(r) = tổng IncomingBinWeight của receipt trong rack r
```

- Current + incoming phải không vượt MaxWeightKg của bin và rack. Dùng decimal tương thích DB.
- Tính tải trọng từ on-hand, không lấy available = on-hand - reserved; hàng reserved vẫn nằm trong kho.
- Nếu module Warehouse kiểm tra MaxWeightKg cấp warehouse, dùng cùng policy khi nhận; không bỏ qua giới hạn tổng đã thực thi ở module khác.
- ReceivedQty của các dòng chung bin/rack phải cộng trước khi validate, tránh mỗi dòng đều qua nhưng tổng vượt.
- Không tính sức chứa bằng số lượng bin/rack tối đa: đó là giới hạn cấu trúc, không phải tải trọng hàng.
- Khi stock key chưa tồn tại, tạo một dòng với ReservedQty = 0; khi đã tồn tại, cộng an toàn, tránh lost update.
- Revalidate trong approval transaction và bảo vệ tài nguyên bin/rack/warehouse được kiểm tra để hai receipt đồng thời không cùng chiếm chỗ trống.

### 8.3 Serial và movement

- Uniqueness trên serial chính thức có phạm vi toàn hệ thống.
- Chuẩn hóa mã bằng rule thống nhất với collation DB. Không tự uppercase nếu serial có phân biệt hoa/thường; không tự bỏ số 0 đầu.
- Check trước ở service để thông báo dễ hiểu, và constraint/locking ở DB để chống race. Chỉ `AnyAsync` rồi insert là chưa đủ.
- Trong cùng receipt, scan trùng không tạo dòng mới và không tăng ReceivedQty.
- Serial đã thuộc thiết bị chính thức không được nhập lại như thiết bị mới.
- Scan staging của hai receipt khác nhau: cần policy rõ. Đề xuất độc lập để triển khai ban đầu là khóa mã trong các receipt DRAFT/PENDING_APPROVAL đang hoạt động, giữ lịch sử khi reject và giải phóng quyền giữ mã nếu policy nhóm cho phép. Đây là **đề xuất cần đối chiếu code/nhóm**, không phải chi tiết được SDS xác định.
- Nếu policy staging chưa được chốt, agent vẫn triển khai uniqueness trong receipt và uniqueness chính thức; ghi rõ kiểm tra cross-receipt lúc approve là chốt cuối, không tuyên bố đã có reservation toàn cục ở draft.
- Movement nhập hàng: FromBinId = null, ToBinId = bin đã duyệt, RefId = GoodsReceipt ID, RefType/MovementType theo constant repo, PerformedByUserId là actor của approval operation. Thông tin Staff nhận hàng vẫn nằm ở ReceivedByUserId.
- Không ghi movement nhập kho khi mới scan hoặc submit.

### 8.4 Transaction và xử lý đồng thời

Approval service là transaction coordinator. Các adapter Stock và Inbound phải dùng **cùng DbContext/connection và transaction** trong request scope. Không gọi HTTP sang một API khác để cộng Stock rồi giả định SQL transaction có thể rollback nó.

Với SQL Server, triển khai theo infrastructure hiện hữu: transaction đủ isolation kết hợp row/range locking hoặc một cơ chế đồng thời tương đương có test thật. `findByIdForUpdate` là ý định khóa, không phải method EF tự sinh. Không nghĩ `AsTracking()` tự cấp khóa cập nhật.

Tối thiểu phải bảo vệ:

1. Receipt header để approve/reject cạnh tranh chỉ có một quyết định thắng.
2. Inbound hoặc tập item liên quan để hai receipt không cùng dùng một remaining cũ.
3. Stock key để hai phép cộng không ghi đè nhau; kể cả khi key chưa tồn tại.
4. Bin/rack/warehouse liên quan để kiểm tra tải trọng vẫn đúng khi commit.
5. Serial key/range bằng unique constraint và cơ chế locking thích hợp.

Giữ thứ tự lấy khóa ổn định, chẳng hạn các ID sắp tăng dần trong cùng loại tài nguyên; phối hợp với module Stock/Inbound để không tạo thứ tự khóa ngược. Nếu repo bật execution strategy retry, dùng transaction theo cơ chế retry của repo và bảo đảm retry không cộng tồn hai lần. Không retry mù mọi BusinessRuleException.

Hành vi mặc định đề xuất cho duplicate decision: receipt đã terminal thì trả 409, không chạy lại write. Có thể dùng idempotent response nếu nhóm đã có convention, nhưng dù trả 200 hay 409 thì tác dụng nghiệp vụ chỉ xảy ra đúng một lần. Test phải xác minh dữ liệu, không chỉ HTTP code.

## 9. Hợp đồng API và DTO

### 9.1 Route có bằng chứng trong sequence

| Method | Route | Service/ý nghĩa |
| --- | --- | --- |
| GET | `/api/goods-receipts` | List; query pending dùng chung route |
| GET | `/api/goods-receipts/{id}` | Detail |
| POST | `/api/goods-receipts` | CreateReceipt |
| PUT | `/api/goods-receipts/{id}` | UpdateDraft |
| POST | `/api/goods-receipts/{id}/submit` | SubmitReceipt |
| POST | `/api/goods-receipts/{id}/items/{itemId}/serials` | AddSerial |
| POST | `/api/goods-receipts/{id}/approve` | ApproveReceipt |
| POST | `/api/goods-receipts/{id}/reject` | RejectReceipt |

Không tự thêm endpoint confirm để Staff cập nhật Stock. Không tách pending list thành controller approval khác nếu cùng route đã đủ.

Endpoint đọc approved Inbound, bins và tiến độ của Người 4/2 chưa được xác định bởi sequence Goods Receipt. Dùng API sẵn có khi tích hợp; nếu chưa có thì tạo query adapter/backend lookup tối thiểu hoặc mock FE theo contract đề xuất và đánh dấu rõ. `GET /api/inbound-requests/{id}/receiving-progress` chỉ là đề xuất trong file phân công, không phải route đã xác minh trong repo.

### 9.2 Nội dung DTO đề xuất

Đây là contract cần map với code, không phải bản sao chính xác của class specification.

| DTO | Trường tối thiểu/ý nghĩa |
| --- | --- |
| CreateGoodsReceiptRequest | inboundRequestId; ngày nhận nếu policy cho phép nhập, còn lại lấy server |
| UpdateGoodsReceiptRequest | expectedVersion; các dòng đã có và bin được chọn; không nhận Status/ReceivedByUserId tùy ý |
| AddReceiptSerialRequest | serialCode; expectedVersion nếu aggregate dùng optimistic concurrency |
| SubmitGoodsReceiptRequest | expectedVersion; không truyền số lượng làm nguồn chính thức |
| ApproveGoodsReceiptRequest | expectedVersion nếu dùng; không nhận StockDelta do FE tính |
| RejectGoodsReceiptRequest | reason và expectedVersion nếu dùng |
| ReceiptSerialResponse | staging scan ID, item ID, mã, count hiện tại, version mới |
| GoodsReceiptResponse | ID, inbound ID, warehouse, người/ngày nhận, status, items/serials, version, quyết định được phép xem |

Nếu repo dùng `ApiResponse<T>`, dùng lại đúng cấu trúc; nếu chưa có, định nghĩa một response thống nhất. Không return EF entity/navigation graph trực tiếp.

Mã lỗi nghiệp vụ phải phân biệt invalid format, duplicate serial, vượt remaining, thiếu bin, vượt capacity, stale version và sai state. HTTP đề xuất: 400 validation, 401 chưa đăng nhập, 403 sai quyền, 404 không có dữ liệu, 409 state/concurrency/duplicate, 500 lỗi không dự kiến đã rollback. Dùng convention có sẵn của dự án nếu khác nhưng giữ mã nghiệp vụ và khả năng FE xử lý.

### 9.3 Thông báo cần map

| Message nguồn SRS | Ý nghĩa | Áp dụng |
| --- | --- | --- |
| MSG91 | Inbound không hợp lệ/chưa được duyệt | Create guard |
| MSG92 | Inbound đã nhận đủ | Create/approval guard |
| MSG93 | Chưa có quantity nhận | Submit yêu cầu ít nhất một scan hợp lệ |
| MSG94 | Chưa chọn bin | Submit guard |
| MSG95 | Nhận vượt remaining | Submit và approve |
| MSG96 | Bin không đủ sức chứa | Chọn bin, submit, approve |
| MSG97 | Nội dung cũ nói confirm cập nhật Stock | Sửa text submit thành gửi Manager duyệt; approval có confirm cập nhật tồn riêng |
| MSG98 | Tạo receipt thành công | Create DRAFT thành công, không tuyên bố nhập tồn xong |
| MSG133 | Không có Goods Receipt | Empty list |
| MSG157 | Thiếu serial cho thiết bị nhận | Submit validation |
| MSG158 | Serial/IMEI đã tồn tại | Duplicate error |

Mã/thông báo mới cho pending/approved/rejected/stale version dùng namespace của module nếu repo có; không tự nhận đó là MSG số gốc của SRS. Lỗi format và lỗi duplicate không dùng chung thông báo gây hiểu nhầm.

## 10. Các màn hình Người 5 cần hoàn thành

| Màn hình hoặc vùng UI | Actor | Nội dung và thao tác |
| --- | --- | --- |
| Inbound được phép nhận | Staff | Inbound ID, kho, SKU, expected, đã nhận chính thức, remaining; nút tạo phiếu |
| Goods Receipt List | Staff và Manager theo quyền | Filter status/warehouse/date, paging, tạo mới cho Staff, mở detail |
| Create/Edit Draft | Staff | Header Inbound, dòng SKU, scan count, danh sách mã, chọn bin, lưu nháp, gửi duyệt |
| Scan panel trong draft | Staff | Nhập tay/máy scan dạng keyboard input, Enter để gửi, báo mã sai/trùng và count do server trả |
| Goods Receipt Detail | Staff/Manager theo quyền | Items, serial, bin, status, version, lý do reject; read-only khi không còn draft |
| Pending Approval List | Manager | Có thể dùng cùng list với filter pending; mở review |
| Review và quyết định | Manager | So expected/remaining với receipt; approve; reject bắt buộc reason; refresh sau thành công |

Sau submit không cần một màn hình “chuyển phiếu” nữa. Pending list đọc chính trạng thái đã lưu trong DB.

Frontend phải có loading, empty, validation error, network failure và stale version state. Disable nút đang gửi để tránh bấm lặp, nhưng backend vẫn phải chống double write. Không hiển thị thành công/cập nhật Stock giả trước khi server commit. Sau 409, tải lại detail và yêu cầu người dùng xem dữ liệu mới, không tự gửi lại quyết định cũ.

Không thiết kế nút xóa serial, hủy receipt hoặc mở lại rejected như chức năng chính thức nếu chưa có contract trong nguồn hoặc code đã được chấp thuận. Có thể ghi là mở rộng sau, không lén thêm vào acceptance criteria.

## 11. Làm độc lập mà không chờ người khác

### 11.1 Phân biệt ba loại kiểm thử

| Cách chạy | Mục đích | Có thể kết luận |
| --- | --- | --- |
| Unit test với fake interface | Tự code service và các nhánh nghiệp vụ ngay | Logic điều phối, nhánh alt, thứ tự call |
| Module integration trên SQL Server local với adapter EF | Tự test tạo/scan/submit/approve/reject và rollback | Persistence, constraints, transaction thực trong môi trường development |
| Tích hợp với implementation thành viên khác | Đổi adapter, dùng Auth/Stock/Inbound thật | Khả năng ghép toàn hệ thống khi đã chạy test tích hợp |

Fake Stock trả success không chứng minh Stock thật đã tăng. Không dùng test với EF InMemory để kết luận transaction SQL Server, unique index hay locking đã đúng.

### 11.2 Các điểm kết nối cần có

Interface có thể giữ trong `Services/GoodsReceipts/Contracts` hoặc vị trí theo repo. Đoạn C# dưới đây là contract đề xuất; kiểu ID và model phải map sang entity thật.

```csharp
public interface IInboundReceivingReader
{
    Task<InboundReceivingSnapshot?> GetAsync(
        int inboundRequestId, CancellationToken cancellationToken);
}

public interface IReceivingCapacityChecker
{
    Task ValidateAsync(
        ReceiptPostingPlan plan, CancellationToken cancellationToken);
}

public interface IStockReceivingWriter
{
    // Tham gia transaction của caller, không tự commit transaction khác.
    Task ApplyApprovedReceiptAsync(
        ReceiptPostingPlan plan, CancellationToken cancellationToken);
}

public interface IInboundReceivingProgressWriter
{
    // Query phải thấy receipt vừa save APPROVED trong transaction hiện tại.
    Task<ReceivingProgressSnapshot> RefreshAsync(
        int inboundRequestId, CancellationToken cancellationToken);
}
```

`InboundReceivingSnapshot`, `ReceiptPostingPlan`, `ReceivingProgressSnapshot` là model đề xuất cần định nghĩa, không coi như type đã tồn tại. Plan gồm receipt/inbound ID, warehouse, các dòng variant/bin/quantity và các key tải trọng cần kiểm tra. Không lấy plan do FE gửi làm nguồn đã tin cậy; service dựng từ DB.

`ICurrentReceivingUser` nên lấy authenticated claims/context của repo. Test dùng fake có UserId, role, warehouse IDs. Ở runtime development có thể dùng auth handler riêng, nhưng authorization vẫn được thực thi.

### 11.3 Không đợi Người 1

1. Dùng Product/ProductVariant/Supplier đã seed trong DB development; không cần màn hình CRUD Product.
2. Dùng một variant SKU `DEMO-LAPTOP-01`, WeightKg = 2 làm fixture.
3. Nếu API Product chưa xong, backend query adapter đọc entity hiện có; FE dùng dữ liệu lookup từ backend hoặc fixture trong chế độ mock.
4. Không hardcode ID sản phẩm trong service production. Seed tìm theo code rồi lấy ID được tạo.

### 11.4 Không đợi Người 2

1. Tạo Warehouse/Section/Rack/Bin fixture trực tiếp bằng dev seeder dùng entity đã có; không cần CRUD Warehouse.
2. Tạo fake `IReceivingCapacityChecker` và `IStockReceivingWriter` cho unit test. Fake ghi lại tham số để assert, không sửa DB production.
3. Để chạy module thật end-to-end local trước khi Người 2 xong, tạo **development EF adapter** trong namespace module Người 5. Adapter query capacity và cộng Stock bằng DbContext dùng chung theo mục 8.
4. Adapter này phải được test transaction thật; không chỉ trả success. Không chỉnh `StockService` của Người 2.
5. Khi implementation của Người 2 sẵn sàng, đổi DI sang adapter gọi nó; kiểm tra nó không tự commit transaction riêng và không cộng lại tồn lần hai.

Không có Stock API từ Người 2 vẫn tự làm được; điều kiện là có schema entity và DB development tương thích. Không gọi HTTP qua StockController trong cùng quy trình approval.

### 11.5 Không đợi Người 3

1. Unit test inject current-user fake với Staff, Manager, user sai kho và user inactive.
2. Module integration dùng test authentication scheme hoặc development authentication handler riêng.
3. Chỉ đăng ký handler này khi môi trường Development/Testing và cờ cấu hình bật rõ ràng; production phải không đăng ký và phải từ chối cấu hình bypass.
4. Mọi API vẫn đi qua permission/warehouse checks. Không bỏ `[Authorize]` để test thuận tiện, không tin `X-Role`/`X-UserId` tùy ý ở production.
5. FK ReceivedByUserId/PerformedByUserId cần user thật trong DB test: seed user/role/UserWarehouse phù hợp. Với password field bắt buộc, dùng hasher hiện có hoặc dữ liệu test không đăng nhập qua đường thật, không tạo endpoint đăng nhập bỏ kiểm tra mật khẩu.
6. Khi JWT thật sẵn sàng, chỉ thay current-user/auth adapter và chạy lại matrix quyền.

### 11.6 Không đợi Người 4

1. Seed Inbound APPROVED có 10 thiết bị, đúng warehouse/supplier/product variant. Không cần tự xây các bước Supplier Commitment và Manager Approve Inbound.
2. Reader development đọc Inbound bằng EF qua repository/adapter, không gọi API chưa tồn tại.
3. Fake progress writer phục vụ unit test; development EF progress writer phục vụ test persistence thật.
4. Progress writer thật phải query receipt APPROVED, tính per variant và cập nhật Inbound trong transaction của approval. Không cộng received từ lời gọi event nhiều lần.
5. Khi Người 4 sẵn sàng, đổi DI gọi phương thức refresh tương đương, giữ nguyên receipt/stock logic.

### 11.7 Quy tắc DI và thay adapter

- Dùng một extension đăng ký module, ví dụ `AddGoodsReceiptModule(...)`, để giảm sửa `Program.cs`.
- Config phát triển có thể phân biệt fake unit test, standalone EF development, integrated. Đây là cấu hình kỹ thuật đề xuất, không đưa vào UI sản phẩm.
- Mỗi contract chỉ có **một** implementation được chọn cho runtime đó.
- Fake/test auth/dev seeder không tự chạy ở production.
- Nếu integrated dependency chưa được đăng ký, fail rõ ở startup/validation; không fallback âm thầm sang fake trả thành công.
- Lưu tài liệu hợp đồng trong module của mình để người khác đọc được. Chỉ chia sẻ contract và fixture ID/code cần thiết, không cần đợi service của họ viết xong mới bắt đầu.

## 12. Seed dữ liệu và kịch bản tự chạy

### 12.1 Dataset độc lập

Tạo seeder development có thể chạy lại mà không nhân bản dữ liệu, theo thứ tự FK thật. Không hardcode identity ID cố định. Dùng code prefix `GR-DEMO-` để dễ nhận biết trong DB development.

| Dữ liệu | Giá trị phục vụ demo |
| --- | --- |
| User/Role | Một Staff, một Manager, một user sai kho; active và warehouse membership phù hợp |
| Warehouse | Kho A được cấp quyền; Kho B không được cấp cho Staff A |
| Location | Section A → Rack A → Bin A; thêm Bin B thuộc kho khác |
| Capacity | Bin A đủ 20 kg cho bài test 10 máy × 2 kg; rack/warehouse đủ lớn; fixture riêng cho quá tải |
| Supplier/Product/Variant | Supplier demo, category/product bắt buộc theo FK, một variant WeightKg = 2 |
| Inbound | Approved, expected 10; có fixture Pending, Rejected và Completed riêng |
| Stock | Không có dòng hoặc OnHandQty = 0 và ReservedQty = 0 |
| Serial | Mã duy nhất cho 8 máy đầu và 2 máy sau; một mã tồn tại chính thức cho test duplicate |

Các fixture negative test phải độc lập, không làm hỏng dataset happy path. Nếu FK bắt buộc manager user ở Warehouse và UserWarehouse tạo vòng phụ thuộc, seed theo thứ tự hợp lệ với schema thực tế, không tắt toàn bộ constraints.

### 12.2 Kịch bản chính 10 → 8 → 2

1. Với Staff A, xem Inbound Approved expected 10, remaining 10.
2. Tạo GR1: DRAFT; Stock vẫn 0.
3. Scan 8 mã hợp lệ thuộc SKU đó: ReceivedQty = 8; chưa có 8 serial IN_STOCK và chưa có receipt movement chính thức.
4. Chọn Bin A, lưu draft với version mới nhất.
5. Submit GR1: PENDING_APPROVAL; Stock vẫn 0; Inbound approved-received vẫn 0.
6. Với Manager A, xem pending list và detail GR1.
7. Approve GR1: GR1 APPROVED, Stock tăng 8, 8 Serial IN_STOCK ở Bin A, 8 receipt movements; remaining = 2; Inbound PARTIALLY_RECEIVED.
8. Staff tạo GR2 từ Inbound đó, scan 2 mã mới, chọn Bin A, submit.
9. Manager approve GR2: Stock tổng 10, remaining = 0, Inbound COMPLETED.
10. Gọi approve lại GR1: không tăng thêm Stock/Serial/Movement.
11. Thử tạo receipt tiếp cho Inbound này: MSG92, không thêm receipt.

### 12.3 Kịch bản reject riêng

1. Dùng Inbound fixture khác expected 10, tạo và submit receipt 8 máy.
2. Manager reject không có reason: lỗi; phiếu vẫn pending.
3. Reject có reason: phiếu REJECTED, reason đọc lại được sau restart.
4. Stock không thay đổi; không có Serial IN_STOCK/movement từ receipt đó; tiến độ Inbound không tăng.
5. Thử approve receipt rejected: bị chặn; không phát sinh tồn.

### 12.4 Cách chạy agent phải hoàn thiện theo repository

Agent tìm tên solution/project và ghi lệnh cụ thể đã kiểm chứng trong báo cáo bàn giao. Không bịa tên project hoặc script.

```bash
# Chạy từ thư mục solution thực tế
dotnet restore
dotnet build
dotnet test

# Chỉ dùng sau khi thay placeholder bằng project thật và DB local đã cấu hình
dotnet ef database update --project <data-project> --startup-project <api-project>
dotnet run --project <api-project>

# Frontend: chạy từ thư mục chứa package.json, dùng package manager/lockfile của repo
npm ci
npm run dev
```

Nếu Data và API cùng project, sửa lệnh EF cho phù hợp. Chỉ chạy database update khi có migration chưa áp dụng trên DB development; không cần tạo migration mới cho các entity đã có. Nếu EF tool chưa sẵn, dùng tool manifest/version tương thích mà repo quy định, không nâng EF chỉ để lệnh chạy.

Frontend dùng pnpm/yarn nếu đó là convention repo; `npm ci` là ví dụ cho repo có package-lock. Agent bổ sung cách bật standalone development mode, chạy seed, lấy user test và API base URL thực tế. Chạy Swagger/Postman/curl theo hạ tầng hiện hữu; không giả định Swagger đã bật ở mọi environment.

## 13. Các phase triển khai và điều kiện qua phase

### Phase 0 — Kiểm tra code và chốt mapping

**Làm:** xác định scope cuối; lập bảng entity/status/property; xác định schema gap scan, bin draft, version, reason; ghi route thực có và contract còn thiếu. Chụp trạng thái build/test hiện tại để phân biệt lỗi sẵn có.

**Hoàn thành khi:** biết chính xác sẽ dùng DbContext nào, ID type nào, map trạng thái ra sao, phần nào cần migration bổ sung. Không tạo lại entity đã migration.

### Phase 1 — Dựng điểm kết nối và dữ liệu development

**Làm:** tạo contract, current-user test, seeder và các adapter development; DI cô lập. Hoàn thành schema gap tối thiểu nếu thực sự cần.

**Hoàn thành khi:** tự có Inbound Approved, ProductVariant, Bin và hai actor hợp lệ mà không cần API của thành viên khác. Test/standalone dùng SQL Server riêng.

### Phase 2 — Read và Create DRAFT

**Làm:** UC-15/16/17, query scoping, DTO mapping, empty/not-found, tạo DRAFT.

**Hoàn thành khi:** tạo receipt từ Approved được, Pending/Completed/sai kho bị chặn, không có Stock tăng.

### Phase 3 — Scan, chọn bin, lưu nháp

**Làm:** UC-50, persist staging, uniqueness, recount, bin mapping, optimistic concurrency.

**Hoàn thành khi:** scan hợp lệ tăng đúng một count, trùng/sai không tăng; refresh/restart không mất dữ liệu; chọn bin sau scan được; version stale bị từ chối.

### Phase 4 — Submit và Manager review

**Làm:** UC-18 nhánh SUBMIT, validation đủ dữ liệu, chuyển PENDING_APPROVAL, pending list/detail Manager.

**Hoàn thành khi:** Manager thấy phiếu ngay sau submit; Staff không sửa/scan pending; Stock chưa thay đổi.

### Phase 5 — Approve/Reject và tính nhất quán

**Làm:** UC-56 đúng thứ tự sequence, transaction, Serial + Movement, Stock writer, progress writer, capacity recheck, reason và concurrency.

**Hoàn thành khi:** happy path 8 rồi 2 chạy trên SQL Server; reject không cộng tồn; lỗi giữa chừng rollback toàn bộ; approve lặp/đồng thời không nhân đôi tồn.

### Phase 6 — Hoàn thiện React

**Làm:** pages/components/hooks/API services theo mục 10; quyền UI; loading/error/empty; version; dùng backend thật của module.

**Hoàn thành khi:** Staff → Manager hoàn thành được vòng đời trên UI với dữ liệu development; thông báo phân biệt gửi duyệt và duyệt thành công.

### Phase 7 — Thay adapter và bàn giao

**Làm:** khi dependency thật sẵn sàng, đổi DI; giữ nguyên API module; chạy lại auth/capacity/rollback/progress test; loại fake khỏi production registration.

**Hoàn thành khi:** các implementation thật tham gia transaction đúng và test tích hợp đạt. Nếu dependency chưa sẵn sàng, báo “hoàn tất module độc lập, chờ tích hợp adapter X”; không dừng việc triển khai module ở Phase 1.

Mỗi phase để lại diff tập trung và kết quả kiểm chứng. Không bắt người dùng xác nhận lại các bước đã nằm trong phạm vi này. Chỉ hỏi khi thiếu quyết định nghiệp vụ mà không có nguồn/code để suy ra; tiếp tục các phase độc lập trong lúc chờ.

## 14. Ma trận kiểm thử bắt buộc

| ID | Tình huống | Kết quả cần kiểm tra |
| --- | --- | --- |
| GR-01 | List rỗng | 200 với collection rỗng, empty state |
| GR-02 | Detail ID không có | 404, không lỗi null reference |
| GR-03 | Create từ Approved còn thiếu | DRAFT, đúng Inbound/user, Stock không tăng |
| GR-04 | Create từ Pending/Rejected/Cancelled | Bị chặn, không thêm receipt |
| GR-05 | Inbound đã nhận đủ | MSG92, không thêm receipt |
| GR-06 | Staff sai kho hoặc user inactive | Bị chặn server-side |
| GR-07 | Scan mã hợp lệ | Persist staging, count và version đổi đúng |
| GR-08 | Scan format sai hoặc SKU sai | Không persist thành công, không tăng count |
| GR-09 | Scan trùng cùng phiếu | Không thêm dòng, count không đổi |
| GR-10 | Mã đã có trong SerialNumber | Duplicate; không đăng ký lại |
| GR-11 | Hai scan đồng thời cùng mã | Tối đa một scan hợp lệ theo scope policy |
| GR-12 | ItemId thuộc receipt khác | Bị chặn, không sửa item khác |
| GR-13 | Nháp chưa chọn bin | Có thể tồn tại theo draft schema; submit bị chặn |
| GR-14 | Bin khác warehouse | Bị chặn |
| GR-15 | Nhiều dòng chung rack tổng vượt | Bị chặn dù từng dòng riêng đủ |
| GR-16 | ReceivedQty payload khác scan count | Bỏ qua/từ chối payload, không tin quantity FE |
| GR-17 | Submit 0 scan/thiếu bin | Vẫn DRAFT, Stock không đổi |
| GR-18 | Submit hợp lệ | PENDING_APPROVAL, chưa IN_STOCK/movement |
| GR-19 | Edit/scan khi pending/approved/rejected | Bị chặn, dữ liệu không đổi |
| GR-20 | Stale version khi update/submit | Conflict, không lost update |
| GR-21 | Staff/Purchasing gọi approve | Forbidden |
| GR-22 | Manager approve đúng | Serial + movements + Stock + receipt + Inbound cùng thành công |
| GR-23 | Reject reason rỗng | Vẫn pending, reason chưa ghi sai |
| GR-24 | Reject có reason | REJECTED bền vững, không tăng Stock/progress |
| GR-25 | Remaining bị receipt khác tiêu thụ sau submit | Approve recheck và chặn vượt |
| GR-26 | Capacity hết sau submit | Approve recheck và rollback |
| GR-27 | Approve lặp | Không cộng lần hai, không movement/serial dư |
| GR-28 | Approve và reject đồng thời cùng receipt | Chỉ một quyết định thắng; hiệu ứng khớp trạng thái |
| GR-29 | Hai receipt cùng Inbound cạnh tranh remaining | Không nhận vượt ExpectedQty |
| GR-30 | Hai receipt khác Inbound cạnh tranh bin/rack | Không vượt tải trọng tổng |
| GR-31 | Stock key chưa tồn tại và concurrent insert | Một key duy nhất, tổng on-hand đúng |
| GR-32 | Stock có ReservedQty > 0 | Receipt cộng on-hand, reserved giữ nguyên |
| GR-33 | Lỗi sau ghi Serial hoặc sau cộng Stock | Toàn bộ rollback, không dữ liệu nửa chừng |
| GR-34 | Progress refresh ném exception sau save Approved | Receipt không Approved sau rollback; Stock/Serial/Movement không đổi |
| GR-35 | Nhận 10 → 8 → 2 | Partial còn 2, sau đó Completed còn 0 |
| GR-36 | Hai SKU, một SKU thiếu | Inbound chưa Completed dù tổng nhận bằng tổng expected do dữ liệu lỗi |
| GR-37 | Restart API giữa draft và submit | Scan và bin đã lưu còn nguyên |
| GR-38 | Production config còn fake auth/writer | Không khởi động với bypass/fake âm thầm |

Unit test kiểm tra service và alt branches. Integration test dùng SQL Server kiểm tra FK, uniqueness, transaction, locking và concurrency thực. Dùng fault injection qua adapter test để gây lỗi giữa chuỗi write; không làm hỏng service production để thử rollback. Concurrency test dùng request scope/DbContext riêng cho từng task; không dùng một DbContext chung chạy song song.

## 15. Checklist bàn giao cho nhóm

- [ ] Backend theo Controllers → Services → Repositories → Data; FE đúng package SDS.
- [ ] UC-15, 16, 17, 18, 50, 56 có implementation và các nhánh alt tương ứng.
- [ ] Approve/Reject bám SDS 3.3.2.6; không tự nhận bản đính kèm có mục 3.3.2.7.
- [ ] Manager review nhận phiếu ngay sau submit, không chờ UC-53.
- [ ] Chỉ Manager approve mới làm phát sinh nhập tồn chính thức.
- [ ] Serial staging và SerialNumber chính thức được phân biệt; scan → chọn bin hoạt động với schema thật.
- [ ] ReceivedQty từ scan; remaining từ receipt APPROVED; tính theo từng variant.
- [ ] Receipt, Serial, Movement, Stock và Inbound commit/rollback cùng nhau.
- [ ] Capacity và remaining được tái kiểm tra khi approve và bảo vệ khi concurrent.
- [ ] Reject reason đọc lại được, không tăng Stock, không tự reopen.
- [ ] Entity/migrations cũ không bị tái tạo; schema bổ sung nếu có được liệt kê rõ.
- [ ] Có dữ liệu dev và hướng dẫn chạy không cần API của Người 1–4.
- [ ] Test báo rõ cái nào dùng fake, cái nào dùng SQL Server thật.
- [ ] Adapter tích hợp chỉ đăng ký một implementation, không cộng tồn hai lần.
- [ ] Có danh sách tài liệu cần đồng bộ: UC-17 cũ, confirmReceipt cũ, label/email, Manager UC diagram, staging/version/bin draft/reason.

## 16. Mẫu yêu cầu để giao cho agent thực hiện

```text
Đọc NGUOI_5_GOODS_RECEIPT_AGENT_GUIDE.md và AGENTS.md của repository.
Tôi là Người 5 theo phương án phân công cuối: Goods Receipt, Serial/IMEI
và Receipt Approval. Entity và migrations đã có; hãy kiểm tra, tái sử dụng.

Triển khai FE và BE cho module theo từng phase trong tài liệu. Giữ kiến trúc
SDS và các bước swimlane/sequence. Staff chỉ lập, scan, chọn bin và gửi duyệt.
Manager approve mới tạo nhập tồn chính thức. Reject phải có reason.

Trước khi code, lập mapping nguồn → code thực tế và chỉ ra schema gap.
Không tạo lại toàn bộ database/entity. Nếu thiếu scan staging, draft bin,
concurrency hoặc nơi lưu quyết định, bổ sung tối thiểu và ghi rõ migration.

Không cần chờ module khác: tạo contract cùng fake cho unit test và adapter
development dùng DbContext chung cho integration test SQL Server.
Fake không được dùng để tuyên bố Stock/progress production đã đúng.

Approval phải giữ thứ tự: validate → đăng ký Serial/Movement → cộng Stock
→ save receipt Approved → refresh Inbound → commit. Tất cả trong một transaction.
Chống double approval, stale edit, duplicate serial, vượt remaining và capacity.

Hoàn thành các phase có thể làm độc lập, chạy kiểm thử phù hợp, rồi bàn giao
diff, lệnh chạy thực tế, dataset 10 → 8 → 2 và kết quả kiểm thử. Báo trung thực
dependency còn fake và điểm tài liệu chưa đồng bộ; không dừng ở việc lập kế hoạch.
```

File này đủ để agent bắt đầu độc lập với repository hiện tại. Những chi tiết mà tài liệu nguồn chưa định nghĩa được đánh dấu rõ để agent kiểm tra và triển khai có kiểm soát, thay vì tự nhận là yêu cầu gốc hoặc bỏ bước nghiệp vụ.
