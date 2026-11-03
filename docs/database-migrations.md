# Migration database EWMS

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
