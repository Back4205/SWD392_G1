# Migration database EWMS

Các mục nền SDS dưới đây ghi nhận đợt entity ban đầu. Phần bổ sung Goods Receipt
ngày 04/11/2026 ở cuối tài liệu mô tả migration mới và trạng thái kiểm thử hiện tại.

Backend dùng .NET 8, EF Core 8 và SQL Server. Model có đủ 34 entity theo mục 2.2
của SDS. Migration `20261103095316_CompleteSdsEntities` bổ sung 13 bảng:

- `TransferOrder`, `TransferOrderItem`, `TransferOrderSerial`.
- `InventorySheet`, `InventorySheetItem`, `InventorySheetTrack`, `InventorySheetSerial`.
- `OutboundRequest`, `OutboundRequestItem`, `DeliveryOrder`, `DeliveryOrderItem`.
- `SerialMovement`, `AuditLog`.

## Chuẩn bị

Cài .NET 8 SDK, SQL Server và công cụ `dotnet-ef` tương thích EF Core 8.
Cấu hình `ConnectionStrings:EWMSDatabase` bằng user secrets, biến môi trường
`ConnectionStrings__EWMSDatabase`, hoặc cấu hình local của API. Không đưa mật khẩu
database vào Git.

Nếu database đã có dữ liệu, kiểm tra trước khi nâng cấp:

- Các giá trị `User.PhoneNumber` khác `NULL` phải duy nhất. Migration tạo unique
  index có điều kiện `PhoneNumber IS NOT NULL` theo cột UN trong SDS.
- Các giá trị `SerialNumber.ReservedForOutboundItemId` và
  `SerialNumber.DispatchedInDeliveryItemId` khác `NULL` phải tham chiếu bản ghi hợp
  lệ trong các bảng mới. Cần đối chiếu và chuyển dữ liệu liên quan trước khi thêm
  khóa ngoại; không tự ý xóa các ID đang lưu.

## Áp dụng migration

Chạy từ thư mục gốc repository:

```powershell
dotnet ef database update --project EWMS/EWMS.Api/EWMS.Api.csproj
```

Hoặc mở solution `EWMS/EWMS.sln` trong Visual Studio, đặt `EWMS.Api` làm Startup
Project và Default project trong Package Manager Console, rồi chạy:

```powershell
Update-Database -Project EWMS.Api -StartupProject EWMS.Api
```

Migration đã được tạo sẵn, không cần chạy `Add-Migration` lần nữa. Database mới
sẽ chạy `InitialCreate` rồi `CompleteSdsEntities`; database đã chạy
`InitialCreate` sẽ được bổ sung phần còn thiếu.

Có thể xuất script SQL để xem trước mà chưa cập nhật database:

```powershell
dotnet ef migrations script --idempotent --project EWMS/EWMS.Api/EWMS.Api.csproj --output "$env:TEMP/ewms-migrations.sql"
```

## Các quyết định về schema

Tất cả khóa ngoại dùng `DeleteBehavior.NoAction`. Các thao tác xóa dữ liệu có liên
quan phải được xử lý rõ ràng trong nghiệp vụ.

Giữ nguyên migration `InitialCreate` và ba trường đã có trên `GoodsReceipt`:
`ApprovedByUserId`, `ApprovalDate`, `RejectionReason`. `AuditLog.OldValue` và
`AuditLog.NewValue` dùng kiểu SQL `text` theo SDS. `User.Email` giữ cấu hình không
ràng buộc duy nhất hiện tại: mô tả trong SDS ghi “Unique”, nhưng cột UN không đánh dấu.

Phạm vi thay đổi là entity, cấu hình EF và migration; chưa triển khai các service
hay API nghiệp vụ. Chưa chạy `database update` trên SQL Server thực tế trong lần
triển khai này.

## Kiểm tra đã thực hiện

Build thành công với 0 warning/error; EF không còn model change chờ migration.
Đã đối chiếu 34 bảng, 217 cột theo SDS, khóa chính, khóa ngoại và unique index.
Đã sinh thành công script SQL tạo mới, nâng cấp và rollback; chưa thực thi các
script này trên SQL Server.

## Tham chiếu

- `EWMS/EWMS.Api/Entities/`: các entity.
- `EWMS/EWMS.Api/Data/EWMSDbContext.cs`: cấu hình bảng và quan hệ.
- `EWMS/EWMS.Api/Migrations/`: lịch sử migration và model snapshot.

## Bổ sung Goods Receipt — 04/11/2026

Migration `20261104063946_AddGoodsReceiptWorkflow` được tạo sau hai migration nền,
không sửa nội dung migration đã có. Schema hiện thêm một bảng scan tạm, tổng cộng
35 bảng entity. Đây là các bổ sung kỹ thuật cần cho sequence Goods Receipt mới:

- `GoodsReceiptSerial`: scan tạm gắn đúng receipt/item, unique
  `(GoodsReceiptId, SerialCode)`, composite FK bảo vệ quan hệ receipt/item.
- `GoodsReceiptItem.BinId` nullable để Draft được scan trước rồi chọn bin.
  Service vẫn bắt buộc bin hợp lệ lúc Submit/Approve.
- `GoodsReceipt.Version` kiểu SQL `rowversion` và `UpdatedAt`; frontend phải
  gửi lại version mới nhất khi sửa, scan, submit hoặc quyết định.
- `GoodsReceipt.RejectionReason` đổi từ `varchar(255)` sang `nvarchar(255)`
  để lưu tiếng Việt; dùng lại các field quyết định đã có.

Migration chưa được áp vào database ứng dụng của người dùng. Bộ integration test
đã áp migrations trên database SQL Server test riêng `EWMS_GoodsReceiptTests_<guid>`.
Không cần tạo lại migration/entity hoặc xóa database để chạy module.

Bản hiện tại dùng `StockReceivingService` ghi SQL trong transaction nhận hàng và Auth JWT thật. Khi chạy lệnh EF cho database local, đặt môi trường và database riêng trước, giữ seed tắt khi tạo schema:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__EWMSDatabase = 'Server=.;Database=EWMS_GoodsReceipt_Development;Integrated Security=true;TrustServerCertificate=true;Encrypt=false'
$env:GoodsReceipts__SeedDevelopmentData = 'false'
dotnet ef database update --project EWMS/EWMS.Api/EWMS.Api.csproj
```

Khi nâng cấp database đã có dữ liệu, `BinId` chỉ được nới nullability; reason
được mở sang Unicode; Version được SQL tạo. Không tái tạo bảng Receipt/Stock/Serial.

`Down` chặn trước khi xóa staging nếu còn `GoodsReceiptItem.BinId IS NULL`, tránh
gán BinId giả bằng 0. Rollback sẽ xóa staging/version và thu reason về varchar;
cần xuất dữ liệu scan/lý do Unicode muốn giữ trước khi quyết định rollback.

Hướng dẫn chạy demo, payload và tích hợp: [Goods Receipt Backend](goods-receipt-backend.md).

## Bổ sung phiên đăng nhập — 05/11/2026

Migration `20261105160638_AddAuthenticationSessions` thêm bảng `AuthSession` gồm ID phiên, UserId, thời điểm tạo/hết hạn/thu hồi và index `(UserId, ExpiresAt)`. Bảng này cho phép logout/đổi mật khẩu thu hồi JWT đang lưu ở cookie; không sửa các migration Receipt trước đó.

Đã áp migration mới vào `EWMS_GoodsReceipt_Development` để kiểm thử web và database test riêng. Chưa áp vào database làm việc của nhóm. Backend hiện qua 61/61 test SQL/HTTP với đăng nhập thật. Hướng dẫn khởi tạo tài khoản local, signing key và cấu hình HTTPS: [Web EWMS theo vai trò](role-based-web.md).

## Bổ sung bảng kê nhà cung cấp — bắt đầu 05/11/2026

Migration `20261105165917_AddInboundSupplierManifest` bổ sung `InboundRequest.HasSupplierManifest` (mặc định false), bảng `InboundExpectedSerial`, alternate key `(Id, InboundRequestId)` của item và composite FK `(InboundRequestItemId, InboundRequestId)`. Unique `(InboundRequestId, SerialCode)` chặn mã trùng trong cùng bảng kê. Không sửa nội dung migration trước đó; không ghi dữ liệu vào SerialNumber/Stock.

Cờ mặc định false giữ các yêu cầu đã duyệt trước migration như dữ liệu lịch sử chưa có bảng kê. Không lấy serial thực nhận để tự backfill thành serial dự kiến. Yêu cầu mới chỉ được duyệt sau khi Purchasing đã Confirmed và nhập đủ danh sách. `Down` xóa bảng kê/cờ nên làm mất dữ liệu khai báo; không dùng rollback để sửa dữ liệu nghiệp vụ.

`start-dev.cmd` tự build và áp migration vào database Development riêng, giữ mật khẩu và Receipt đã có; không cần `-ResetPasswords` để nâng cấp schema. Không áp migration vào database nhóm khi chưa chủ động chọn môi trường đó. Test hiện tại dùng database GUID riêng và đã qua **80/80 SQL/HTTP tests**; log `EWMS/EWMS.Api.Tests/obj/manifest-full-result.log`. Chi tiết: [Bảng kê và đối chiếu](supplier-serial-reconciliation.md).
