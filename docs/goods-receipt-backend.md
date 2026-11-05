# Backend Goods Receipt — hướng dẫn đọc và chạy

Module Người 5 gồm Goods Receipt, Serial/IMEI và Receipt Approval. Backend dùng .NET 8, EF Core 8 và SQL Server. Giai đoạn hiện tại đã bổ sung web React theo role; hướng dẫn tổng thể ở [Web EWMS theo vai trò](role-based-web.md). Code viết theo cách của module Inbound vừa merge: class DTO, constructor DI, `if`, `foreach`, `try/catch` và map thủ công. Không dùng AutoMapper, MediatR, CQRS hay Unit of Work.

## Bảng kê từ nhà cung cấp

Theo yêu cầu bổ sung, Purchasing Confirmed phải gửi đủ serial cho từng SKU trong Inbound; Manager chỉ duyệt yêu cầu mới khi bảng kê hợp lệ. Receipt đối chiếu thực nhận tại Scan/Save/Submit/Approve: mã ngoài bảng kê hoặc sai SKU không được lưu, số lượng/Version giữ nguyên khi lỗi. Chỉ Approve Receipt mới nhập kho. Dữ liệu đã duyệt từ trước migration được hiển thị rõ chưa có bảng kê và giữ khả năng đọc/nhận tiếp.

Xem [Bảng kê nhà cung cấp và đối chiếu](supplier-serial-reconciliation.md) để đọc `InboundExpectedSerial`, payload commitment, các mã lỗi và trạng thái so sánh theo từng đợt nhận. Đây là chi tiết mở rộng UC12 theo yêu cầu người dùng, không phải nhận định rằng sequence cũ đã có toàn bộ bước khai báo serial.

## Đọc code theo thứ tự nào?

1. [Program.cs](../EWMS/EWMS.Api/Program.cs): xem `AddScoped` đăng ký interface với class thực hiện. Mỗi HTTP request dùng một DbContext; Stock, Receipt và Inbound trong request cùng tham gia transaction đó.
2. [DTOs/GoodsReceipts](../EWMS/EWMS.Api/DTOs/GoodsReceipts): xem dữ liệu API nhận và trả. DTO là class có property; không trả trực tiếp entity kèm navigation.
3. [GoodsReceiptController.cs](../EWMS/EWMS.Api/Controllers/GoodsReceiptController.cs): nhận request, lấy user hiện tại, gọi Service và trả HTTP.
4. [GoodsReceiptService.cs](../EWMS/EWMS.Api/Services/GoodsReceiptService.cs): list, detail, create, lưu nháp và submit. Các hàm validate dùng chung nằm phía dưới. `MapToDtoAsync` dùng `new ... { ... }` để thấy rõ dữ liệu lấy từ đâu.
5. [SerialNumberService.cs](../EWMS/EWMS.Api/Services/SerialNumberService.cs): kiểm tra SKU/format/duplicate, lưu scan tạm, đếm lại số lượng.
6. [GoodsReceiptApprovalService.cs](../EWMS/EWMS.Api/Services/GoodsReceiptApprovalService.cs): đọc `ApproveAsync` từ trên xuống theo sáu bước có comment. `RejectAsync` chỉ lưu quyết định.
7. [Entities](../EWMS/EWMS.Api/Entities) và [EWMSDbContext.cs](../EWMS/EWMS.Api/Data/EWMSDbContext.cs): entity tương ứng bảng SQL; DbContext đọc/ghi bảng và cấu hình quan hệ.

Luồng kỹ thuật là **Controller → Service → DbContext → SQL Server** theo yêu cầu đơn giản hóa trực tiếp của người dùng. Các lifeline Repository trong SDS được gộp vào truy vấn DbContext ở Service; đây là thay đổi phân lớp kỹ thuật, không thay thứ tự nghiệp vụ trong use case/swimlane/sequence.

## Luồng nghiệp vụ và đối chiếu tài liệu

| Use case | API | Hàm chính | Sequence SDS |
|---|---|---|---|
| UC-15 danh sách | `GET /api/goods-receipts` | `GetAllAsync` | 3.3.2.1 |
| UC-16 chi tiết | `GET /api/goods-receipts/{id}` | `GetByIdAsync` | 3.3.2.2 |
| UC-17 tạo nháp | `POST /api/goods-receipts` | `CreateAsync` | 3.3.2.3 |
| UC-18 lưu nháp | `PUT /api/goods-receipts/{id}` | `UpdateDraftAsync` | 3.3.2.4 SAVE_DRAFT |
| UC-18 gửi duyệt | `POST /api/goods-receipts/{id}/submit` | `SubmitAsync` | 3.3.2.4 SUBMIT |
| UC-50 quét serial | `POST /api/goods-receipts/{id}/items/{itemId}/serials` | `AddAsync` | 3.3.2.5 |
| UC-56 duyệt | `POST /api/goods-receipts/{id}/approve` | `ApproveAsync` | **3.3.2.6** |
| UC-56 từ chối | `POST /api/goods-receipts/{id}/reject` | `RejectAsync` | **3.3.2.6** |

`Draft → scan → chọn bin/lưu nháp → PendingApproval → Approved hoặc Rejected`.

Staff chỉ tạo và sửa nháp của mình trong kho được gán. Manager xem danh sách `PendingApproval` ngay sau submit rồi quyết định. Chỉ **Manager approve** mới thực hiện:

1. Kiểm tra quyền, kho, trạng thái và Version.
2. Kiểm tra lại SKU/serial, số lượng còn thiếu, bin và tải trọng.
3. Ghi `SerialNumber` chính thức `IN_STOCK` cùng `SerialMovement`.
4. Cộng `Stock.OnHandQty`, giữ nguyên `ReservedQty`.
5. Lưu receipt `Approved`.
6. Tính tiến độ Inbound rồi commit.

`SaveChangesAsync` ghi dữ liệu bên trong transaction, chưa phải commit. Nếu bước cập nhật Inbound lỗi sau khi Stock đã tăng, `RollbackAsync` hủy cả Serial, Movement, Stock và trạng thái Approved. Code dùng trực tiếp `BeginTransactionAsync`, `try`, `CommitAsync`, `catch`, `RollbackAsync`.

Số lượng nhận luôn bằng số scan đã lưu. Số còn thiếu tính **riêng từng variant** bằng Expected trừ tổng receipt `Approved`. DRAFT/PENDING/REJECTED không được tính là đã nhập kho. Inbound chuyển `PartiallyReceived` sau nhận một phần và `Completed` khi mọi variant đã đủ. Reject bắt buộc lý do, giữ scan để xem lại và không sinh tồn chính thức. Không có Cancel/Reopen/Resubmit từ Rejected.

### Các điểm tài liệu gốc chưa đồng bộ

| Nguồn chưa đồng bộ | Cách hiện thực |
|---|---|
| SRS UC-17 bước 9, non-UI Stock Increment và `confirmReceipt` cũ cho Staff tăng tồn | Dùng swimlane Import Goods, UC-56 và sequence mới: Staff submit, Manager approve mới tăng tồn |
| UC-17 nói receipt vừa tạo là Completed | Receipt mới là Draft theo 3.3.2.3; Completed thuộc Inbound |
| BR-09 dùng Incomplete | Dùng một giá trị chuẩn `PartiallyReceived` theo nghĩa nhận một phần |
| Còn mô tả in tem/email | Ngoài phạm vi Người 5, cần cập nhật mô tả cũ |
| Manager UC diagram cũ thiếu receipt approval | Giữ Manager theo UC-56 và swimlane |
| SDS chưa có staging/version và bin nháp | Bổ sung tối thiểu để thực hiện scan trước chọn bin và chống ghi đè |

Tên trạng thái dùng kiểu viết cùng module Inbound: receipt `Draft/PendingApproval/Approved/Rejected`; Inbound `Approved/PartiallyReceived/Completed`. Các quyết định kỹ thuật gồm creator sửa draft, một dòng/variant, chưa chia một SKU ra nhiều bin và không giữ chỗ serial toàn cục khi còn nháp. Không giới thiệu chúng là yêu cầu nguyên văn SDS.

## Quyền và các phần tích hợp

Receipt kiểm tra database: user tồn tại/Active, role `Warehouse Staff` hoặc `Manager`, `UserWarehouse`, ownership và trạng thái. Không lấy role, người nhận, người duyệt hoặc số lượng từ payload.

Auth đã có đăng nhập username/password, JWT ký bởi máy chủ, cookie HttpOnly, CSRF, logout và đổi mật khẩu. Controller lấy user ID từ principal đã xác thực; không đọc user ID/role giả lập từ request. Mỗi request kiểm tra phiên, account Active/role hiện tại, rồi kiểm tra kho và nghiệp vụ. Chi tiết endpoint và vòng đời phiên ở [Web theo vai trò](role-based-web.md).

- `IInboundService → InboundReceivingService`: đọc database và tính tiến độ theo receipt Approved; giữ contract API Inbound đã có.
- `IStockService → StockReceivingService`: kiểm tra tải trọng và ghi Stock SQL thật bằng scoped DbContext chung. Đây là phần cộng tồn nhận hàng, không phải toàn bộ module kho của Người 2.
- `Program.cs` đăng ký service bằng `AddScoped` ở các môi trường. Ngoài Development cần signing key ngoài source và HTTPS; không bật seeder local. Service tích hợp phải dùng cùng DbContext, không tự commit hoặc gọi HTTP cộng tồn ngoài transaction.

Để bảo vệ các yêu cầu đồng thời, transaction dùng `Serializable`; `EWMSDbContext.LockGoodsReceiptAsync` lấy khóa SQL chung module trước khi đọc dữ liệu có thể sửa. Cách này dễ giữ đúng remaining/capacity nhưng làm các write chờ nhau. Unique index và rowversion là lớp bảo vệ dữ liệu bổ sung. Chưa bật retry tự động.

## Schema bổ sung

Migration `20261104063946_AddGoodsReceiptWorkflow` giữ nguyên các migration cũ và thêm:

- `GoodsReceiptSerial` lưu scan bền vững, unique `(GoodsReceiptId, SerialCode)`; composite FK bảo đảm item thuộc đúng receipt.
- `GoodsReceiptItem.BinId` nullable cho Draft. Submit/Approve vẫn bắt buộc bin cho dòng nhận hàng.
- `GoodsReceipt.Version` kiểu SQL `rowversion` và `UpdatedAt`; scan/bin/submit/quyết định đều làm đổi Version.
- `RejectionReason` thành `nvarchar(255)` để lưu tiếng Việt. Tái sử dụng `ApprovedByUserId/ApprovalDate` cho cả hai quyết định, DTO gọi là `DecidedByUserId/DecisionDate`.

Migration đã được thử trên database Development riêng và database SQL test; không áp vào database làm việc của nhóm trong đợt này. SQL test tự tạo database riêng và chạy migrations. Khi rollback schema, guard chặn nếu còn draft có BinId null. Rollback còn xóa staging/version và đổi reason về varchar có thể mất ký tự Unicode; phải xuất dữ liệu cần giữ trước khi quyết định rollback.

## Chạy development độc lập

Cần .NET 8 SDK, SQL Server local, dotnet-ef 8.x và Node.js/npm. Từ repository root:

```powershell
./start-dev.ps1
```

Script dùng database riêng `EWMS_GoodsReceipt_Development`, build/migrate trước khi seed, không sửa `appsettings.json` của nhóm. Lần đầu chọn mật khẩu 12–128 ký tự tại lời nhắc ẩn. Mở `http://127.0.0.1:5173/login`, đăng nhập `staff`, `manager` hoặc `purchasing` bằng mật khẩu đã chọn. Các lần sau Enter để giữ mật khẩu; dùng `-ResetPasswords` khi cần đặt lại cho tài khoản local. Dừng bằng `./stop-dev.ps1`.

Seeder chỉ chấp nhận Development và đúng tên database local; giữ dữ liệu đã thao tác. Mã `GR-DEMO-*` có thể còn trong dataset mẫu cũ. Không dựa vào ID cố định hoặc giả định tồn ban đầu bằng 0. UI chọn Inbound/bin từ API lookup. Muốn thử 8 rồi 2, Manager tạo Inbound mới dự kiến 10, Purchasing cập nhật cam kết, Manager duyệt rồi Staff nhận từng đợt.

[GoodsReceipt.http](../EWMS/EWMS.Api/GoodsReceipt.http) có ví dụ đăng nhập cookie/CSRF và các payload. Mỗi request thay đổi phải có CSRF token, cookie phiên hợp lệ và `expectedVersion` mới nhất. Chi tiết cấu hình và cách chạy riêng Backend/React ở [hướng dẫn web](role-based-web.md).

## Payload API và lỗi thường gặp

Create: `{"inboundRequestId": 1}`. Response 201 trả DTO chi tiết trực tiếp; lấy `id`, `items[0].id`, `items[0].sku`, `version`.

Scan:

```json
{"expectedVersion":"<version mới nhất>","serialCode":"DEMO-001","sku":"GR-DEMO-SKU"}
```

Lưu bin:

```json
{"expectedVersion":"<version sau scan>","items":[{"itemId":1,"binId":1}]}
```

Submit và Approve: `{"expectedVersion":"<version mới nhất>"}`. Reject thêm `"reason":"Hàng vỡ, thiếu phụ kiện"`. Không gửi `receivedQty`, `status`, `receivedByUserId` hay `approvedByUserId`; trường ngoài contract bị từ chối.

List trả `{ totalCount, page, pageSize, items }`. Detail/write trả DTO trực tiếp, không có response envelope. Lỗi nghiệp vụ trả `{ code, message }`; lỗi bind/DataAnnotations tự động trả HTTP 400 dạng ValidationProblemDetails của ASP.NET Core.

| HTTP / code | Ý nghĩa |
|---|---|
| 401 `AUTH_REQUIRED` | Chưa có identity hoặc phiên đã hết hiệu lực |
| 403 `GR_ROLE`, `MSG56`, `GR_OWNER`, `GR_USER_INACTIVE` | Sai vai trò/kho/chủ nháp hoặc user inactive |
| 404 `GR_NOT_FOUND` | Phiếu không tồn tại |
| 400/409 `MSG91`, `MSG92` | Inbound không được nhận/đã nhận đủ |
| 400 `MSG93`, `MSG94`; 409 `MSG157` | Chưa có scan/chưa chọn bin/count không khớp |
| 409 `MSG95`, `MSG96` | Vượt remaining/tải trọng |
| 400 `GR_SERIAL_FORMAT`, `GR_SKU`, `GR_ITEM` | Format/SKU/dòng phiếu sai |
| 409 `MSG158`, `GR_DUPLICATE` | Serial/khóa dữ liệu bị trùng |
| 409 `GR_STATE`, `GR_STALE_VERSION`, `GR_CONCURRENT_WRITE` | Trạng thái/Version/thao tác đồng thời không hợp lệ |

Serial mặc định nhận 1–100 ký tự ASCII chữ/số/dấu `.`, `_`, `-`; giữ case và số 0 đầu, không ép mọi serial thành IMEI 15 số. Có thể đổi regex qua `GoodsReceipts:SerialPattern`. Format được kiểm tra khi Scan và kiểm tra lại lúc Submit/Approve. Collation SQL quyết định mã có phân biệt hoa thường hay không. Sau lỗi stale, tải lại detail để xem dữ liệu mới trước khi thao tác tiếp.

## Kiểm thử và phạm vi kết luận

```powershell
dotnet test EWMS/EWMS.sln --no-restore --logger "trx;LogFileName=receiving.trx" --results-directory EWMS/EWMS.Api.Tests/obj/TestResults
dotnet ef migrations has-pending-model-changes --project EWMS/EWMS.Api/EWMS.Api.csproj
```

Test dùng SQL Server thật, tạo database `EWMS_GoodsReceiptTests_<guid>` riêng cho mỗi lần chạy; cleanup kiểm tra prefix và GUID trước khi xóa. Có thể đặt `EWMS_TEST_SQL_CONNECTION` nếu instance local khác. Không dùng connection database ứng dụng để thử.

Kết quả hiện tại: **80/80 test qua, 0 lỗi, 0 skip**: 61 ca Auth/Receipt đã có và 19 ca bảng kê/đối chiếu mới. Build 0 warning/error. Log lần chạy: `EWMS/EWMS.Api.Tests/obj/manifest-full-result.log`. Các kết quả 46/54/61 test trong journal cũ là mốc lịch sử.

| Checklist | Test hiện có |
|---|---|
| GR01–06 | Empty/not found, create, unapproved/completed inbound, scope kho/user |
| GR07–12 | Scan bền vững, format/SKU, duplicate scan/official, scan đồng thời, item của phiếu khác |
| GR13–18 | Scan trước bin, bin đúng kho, gộp tải trọng rack, cấm chèn ReceivedQty, submit không tăng tồn |
| GR19–24 | Không sửa pending/terminal, stale, chỉ Manager, reject có reason tiếng Việt |
| GR25–32 | Recheck remaining/capacity, approve lặp, race approve/reject/remaining/rack/stock, reserved giữ nguyên |
| GR33–34 | Fault injection sau Serial, sau Stock, sau lưu Approved; mọi bảng rollback |
| GR35–38 | Nhận 10→8→2, từng SKU không bù sai số, dừng/tạo lại API host vẫn còn scan, header giả lập không tạo được identity, môi trường ngoài Development yêu cầu signing key và không cho seed local |
| Ngoài checklist | Ownership cùng kho, double approve, shared serial giữa receipt, sức chứa gồm reserved, SQL unique/FK và 8 HTTP route |

Các test SQL chứng minh persistence/transaction của phần nhận hàng đang hiện thực; kiểm thử HTTP đi qua đăng nhập thật, CSRF và role/kho. Chưa tuyên bố đã tích hợp đầy đủ các phân hệ Stock, Auth Admin, in tem/email hoặc CRUD ngoài phạm vi.

## React và web theo role

Xem [Frontend Goods Receipt](goods-receipt-frontend.md) để đọc component, thực hành tạo/scan/submit/approve và lỗi Version; xem [Web theo vai trò](role-based-web.md) để chạy local, đăng nhập từng role và hiểu bảo vệ API. Báo cáo 44 browser checks ở kế hoạch cũ được giữ làm lịch sử. Giai đoạn mới kiểm tra lại login/role và luồng ba tài khoản; báo cáo cuối nằm trong `plans/20261105-role-based-web/reports/`.

Database local có thể đã có thêm phiếu từ kiểm thử; seeder không reset dữ liệu nghiệp vụ. Migration AuthSession chỉ bổ sung lưu phiên, giữ các migration Receipt đã có.
