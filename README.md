# EWMS — Flow nhập kho

Backend ASP.NET Core Web API 8 + EF Core/SQL Server, Frontend React 18 + TypeScript/Vite.

[Hướng dẫn demo theo class/sequence và State Diagram đã thống nhất](docs/inbound-demo-guide.md).

## Phạm vi

- Manager tạo yêu cầu nhập và chọn nhân viên thuộc kho.
- Purchasing cập nhật nhà cung cấp, ngày giao, SKU và số lượng xác nhận. NCC từ chối thì yêu cầu chuyển Canceled.
- Manager duyệt hoặc từ chối yêu cầu; từ chối phải có lý do.
- Staff lập phiếu nhận, quét Serial/IMEI, chọn bin, lưu nháp và gửi duyệt.
- Manager duyệt phiếu: đăng ký serial, tạo movement, cập nhật tồn kho và tiến độ trong cùng transaction. Từ chối không cộng tồn.
- Nhận nhiều đợt đến PartiallyReceived/Completed; kiểm tra SKU, serial trùng, remaining, sức chứa và dữ liệu cũ.

Flow theo swimlane/sequence RP1/RP2: Staff gửi phiếu chỉ chuyển PendingApproval; Manager duyệt mới cộng tồn. Purchasing không khai báo serial trước. Duyệt dùng xác nhận, từ chối bắt lý do. Tài liệu RP gốc không bị chỉnh sửa.

## Cấu trúc

```text
EWMS/
  EWMS.sln
  EWMS.Api/
    Controllers/        # API đăng nhập, Inbound và Goods Receipt
    Services/           # Validate và xử lý nghiệp vụ
    Interfaces/         # Hợp đồng service/repository
    Repositories/       # User, Inbound, GoodsReceipt, Scan, Serial, Stock và GenericRepository
    DTOs/               # Request/response, map thủ công
    Entities/           # Entity theo schema SDS
    Data/               # DbContext
    Migrations/         # Lịch sử cập nhật database
    GoodsReceipt.http   # Request nhận hàng để thử API
  EWMS.Api.Tests/       # Kiểm thử SQL/HTTP và transaction của flow
frontend/
  src/features/auth/             # Đăng nhập, hồ sơ, phân quyền
  src/features/inbound/          # Yêu cầu nhập, NCC, duyệt, tiến độ
  src/features/goods-receipts/   # Nhận hàng, serial, bin, duyệt phiếu
  src/api/                       # Fetch, cookie và CSRF
  src/hooks/                     # Điều hướng và lifecycle chi tiết Inbound/Goods Receipt
start-dev.cmd / start-dev.ps1     # Chạy API và React
stop-dev.cmd / stop-dev.ps1       # Dừng đúng các process đã khởi động
```

Entity/schema dùng chung vẫn được giữ vì DbContext, quan hệ và migration SDS phụ thuộc vào chúng. Chưa có màn hình hoặc API Outbound/Transfer/Inventory trong bản này. CRUD đầy đủ Product/Supplier/Warehouse và SMTP Email Service nằm ngoài phần Inbound đã triển khai; entity/lookup tương ứng đã có. `IStockService` và `IInboundService` được nối với service thật dùng chung DbContext, không dùng mock trong ứng dụng. Dữ liệu kiểm thử chỉ được tạo trong database GUID riêng của project test.

## Chạy trong terminal VS Code

Cần .NET 8 SDK, SQL Server, dotnet-ef 8.x và Node.js/npm tương thích package-lock. Tại thư mục gốc:

```powershell
.\start-dev.cmd
```

Mở http://127.0.0.1:5173/login. API chạy tại http://localhost:5085, Swagger tại http://localhost:5085/swagger.

Script dùng database hiện có `EWMS_GoodsReceipt_Development`, build và áp migration rồi chạy hai ứng dụng. Không tự tạo tài khoản, SKU, kho, tồn kho hoặc đơn mẫu; không hỏi/đặt lại mật khẩu. Dùng tài khoản đã có trong database, đúng role và được gán `UserWarehouse`. Nếu kết nối database khác, sửa biến connection trong `start-dev.ps1` hoặc chạy API riêng bằng cấu hình của bạn.

```powershell
.\stop-dev.cmd
```

Các script .cmd gọi PowerShell trong process riêng, không thay đổi Execution Policy của máy. `.local` chỉ chứa log/process metadata được sinh lại khi chạy; khóa JWT Development được giữ riêng ở `EWMS/EWMS.Api/.local`.

## Đọc code theo flow

| Sơ đồ | Backend | Frontend |
|---|---|---|
| SD03 — Tạo yêu cầu | InboundRequestService.CreateAsync | CreateInboundRequestPage |
| SD04 — Cam kết NCC | UpdateSupplierCommitmentAsync | SupplierCommitmentForm |
| SD05 — Duyệt yêu cầu | ReviewInboundRequestAsync | InboundReviewForm |
| UC17/UC18 — Nhận, nháp, gửi duyệt | GoodsReceiptService | CreateGoodsReceiptPage, GoodsReceiptDetailPage |
| UC50 — Serial/IMEI | SerialNumberService.AddAsync | Form quét trong GoodsReceiptDetailPage |
| UC56 — Duyệt phiếu | GoodsReceiptApprovalService | ReceiptDecisionDialog |
| Cộng tồn, tính tiến độ | StockReceivingService, InboundReceivingService | InboundReceivingProgress |

Đường xử lý chính: Controller → Service → Repository → EWMSDbContext. Service điều phối nghiệp vụ/transaction; entity thực hiện các operation như RecordApproval, UpdateQuantity và IncreaseOnHand. DI đăng ký scoped repository/service để dùng chung DbContext trong một HTTP request. Receipt dùng rowversion; web gửi revision khi sửa/duyệt Inbound để phát hiện dữ liệu cũ. Manager/Purchasing chỉ thấy tiến độ khi đã có Receipt thật. Không có migration/schema mới trong đợt đồng bộ SDS này.

## Kiểm tra

```powershell
dotnet test EWMS/EWMS.sln --no-restore
cd frontend
npm.cmd run lint
npm.cmd run build
```

Test cần SQL Server và tạo/xóa database `EWMS_GoodsReceiptTests_<guid>` riêng; không dùng database ứng dụng. Có thể đặt `EWMS_TEST_SQL_CONNECTION` nếu instance SQL khác. Khi chạy ngoài Development, cung cấp `Authentication:SigningKey` qua cấu hình môi trường.
