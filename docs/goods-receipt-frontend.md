# React Goods Receipt — hướng dẫn đọc, chạy và demo

Giao diện Người 5 gồm danh sách phiếu, tạo phiếu từ Inbound còn thiếu, quét Serial/IMEI, chọn bin, lưu nháp, gửi duyệt và Manager duyệt/từ chối. React gọi Backend thật; số lượng, trạng thái và quyền thao tác đều lấy từ server.

Code giữ cách viết dễ đọc của module Inbound: component thường, `useState`, `useEffect`, `fetch`, interface TypeScript và CSS thường. Không cài thêm router, thư viện giao diện hay thư viện quản lý state.

## 1. Chạy bằng hai terminal

Cần .NET 8 SDK, SQL Server, `dotnet-ef` 8.x và Node.js/npm phù hợp với `frontend/package-lock.json`. Các lệnh dưới dùng PowerShell trên Windows. Dùng `npm.cmd` nếu máy chặn chạy `npm.ps1`; không cần thay đổi Execution Policy.

### Terminal 1: Backend

Chạy từ thư mục gốc repository. Dùng database demo riêng để không ảnh hưởng database làm việc của nhóm:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__EWMSDatabase = 'Server=.;Database=EWMS_GoodsReceipt_Development;Integrated Security=true;TrustServerCertificate=true;Encrypt=false'
$env:GoodsReceipts__UseDevelopmentUserHeader = 'true'
$env:GoodsReceipts__SeedDevelopmentData = 'false'

dotnet restore EWMS/EWMS.sln
dotnet build EWMS/EWMS.sln --no-restore
dotnet ef database update --project EWMS/EWMS.Api/EWMS.Api.csproj

$env:GoodsReceipts__SeedDevelopmentData = 'true'
dotnet run --project EWMS/EWMS.Api/EWMS.Api.csproj --no-launch-profile --urls http://localhost:5085
```

Seeder chỉ chấp nhận môi trường Development và đúng database `EWMS_GoodsReceipt_Development`. Nó log ID của Staff, Manager, Inbound và bin. Chạy lại seed không xóa hoặc reset dữ liệu đã demo.

### Terminal 2: React

Mở terminal khác tại thư mục gốc repository:

```powershell
cd frontend
npm.cmd ci

if (-not (Test-Path .env.local)) {
    Copy-Item .env.example .env.local
}

npm.cmd run dev -- --host 127.0.0.1 --port 5173
```

Mở `http://127.0.0.1:5173/`. Vite chuyển các request `/api` sang Backend qua proxy, nên không cần tự ghép URL Backend trong mỗi component.

`frontend/.env.example` hiện gồm:

```dotenv
VITE_API_PROXY_TARGET=http://localhost:5085
VITE_RECEIVING_DEMO_ENABLED=true
VITE_RECEIVING_STAFF_ID=2
VITE_RECEIVING_MANAGER_ID=1
```

`.env.local` được Git bỏ qua. Nếu sửa file này, dừng và chạy lại Vite. ID `2/1` đúng với database demo mới đã được tạo trên máy hiện tại; ở database khác, sửa theo log của seeder.

## 2. Đọc code từ đâu?

| File | Vai trò | Điều cần hiểu |
|---|---|---|
| [App.tsx](../frontend/src/App.tsx) | Khung ứng dụng, hai mục điều hướng | Mở Inbound hoặc Goods Receipt; khóa đổi trang khi đang gửi thao tác |
| [useHashRoute.ts](../frontend/src/hooks/useHashRoute.ts) | Theo dõi URL sau dấu `#` | Refresh/back vẫn giữ màn hình và bộ lọc, chưa cần cài router |
| [goodsReceipt.types.ts](../frontend/src/features/goods-receipts/goodsReceipt.types.ts) | Kiểu dữ liệu request/response | Property khớp DTO C#; interface không tạo dữ liệu giả |
| [goodsReceipt.api.ts](../frontend/src/features/goods-receipts/goodsReceipt.api.ts) | Gọi API bằng `fetch` | Mỗi hàm có URL và payload rõ ràng; xử lý lỗi chung ở một nơi |
| [GoodsReceiptWorkspace.tsx](../frontend/src/features/goods-receipts/GoodsReceiptWorkspace.tsx) | Tải session và chọn màn hình | Lấy role/kho từ Backend; bộ chọn actor chỉ dành cho demo |
| [GoodsReceiptListPage.tsx](../frontend/src/features/goods-receipts/GoodsReceiptListPage.tsx) | Danh sách, lọc, phân trang | `useEffect` tải dữ liệu; filter/page lưu trên URL |
| [CreateGoodsReceiptPage.tsx](../frontend/src/features/goods-receipts/CreateGoodsReceiptPage.tsx) | Chọn Inbound và tạo Draft | Chỉ thấy yêu cầu trong kho được gán và còn hàng chưa nhận |
| [GoodsReceiptDetailPage.tsx](../frontend/src/features/goods-receipts/GoodsReceiptDetailPage.tsx) | Scan, bin, lưu, gửi duyệt, xem quyết định | Handler gọi API, đợi thành công rồi cập nhật state |
| [ReceiptDecisionDialog.tsx](../frontend/src/features/goods-receipts/ReceiptDecisionDialog.tsx) | Xác nhận approve/reject | Dùng native `<dialog>`; reject kiểm tra lý do đã trim |
| [receipt.helpers.ts](../frontend/src/features/goods-receipts/receipt.helpers.ts) | Nhãn tiếng Việt, lỗi, ngày giờ | Phân biệt lỗi trùng serial với stale version |
| [goodsReceipt.css](../frontend/src/features/goods-receipts/goodsReceipt.css) | Giao diện module | Tiền tố `gr-` hạn chế đè CSS Inbound |

Các màn hình Inbound cũ được giữ trong `features/inbound`. Phần điều hướng Inbound trước đây ở `App.tsx` được chuyển sang [InboundPages.tsx](../frontend/src/features/inbound/InboundPages.tsx), chỉ đổi đường dẫn import và tên component.

### Các khái niệm React dùng trong module

- `useState`: nhớ dữ liệu đang hiển thị, ví dụ phiếu, ô serial, bin đang chọn hoặc thông báo lỗi.
- `useEffect`: tải dữ liệu khi mở trang, đổi phiếu hoặc đổi tài khoản. Không đặt thao tác approve/submit trong effect.
- Event handler như `handleScan`, `handleSave`: chạy khi người dùng bấm nút hoặc Enter.
- `useRef`: giữ input để đưa focus về ô scan, hoặc đánh dấu request đang chạy để chặn Enter liên tiếp.
- Props: component cha truyền session, trạng thái đang gửi và callback xuống component con.

Ví dụ luồng scan: submit form → `handleScan` → `addReceiptSerial` → Backend kiểm tra/lưu → tải lại phiếu → `setReceipt`. UI không tự cộng `ReceivedQty`.

## 3. URL và API

| Màn hình | URL local |
|---|---|
| Danh sách phiếu | `/#/goods-receipts` |
| Danh sách chờ duyệt | `/#/goods-receipts?status=PendingApproval` |
| Tạo phiếu | `/#/goods-receipts/new` |
| Chi tiết phiếu | `/#/goods-receipts/123` — thay 123 bằng ID thực |
| Module Inbound đã có | `/#/inbound` |

Tám API nghiệp vụ vẫn theo [bàn giao Backend](goods-receipt-backend.md). Giao diện dùng thêm ba API chỉ đọc:

| API | Dữ liệu phục vụ UI |
|---|---|
| `GET /api/goods-receipts/session` | ID, tên, role và các kho được phân công |
| `GET /api/goods-receipts/lookups/inbounds` | Inbound Approved/PartiallyReceived còn thiếu trong kho của Staff |
| `GET /api/goods-receipts/lookups/bins?warehouseId=...` | Bin đúng kho, tên rack và tải trọng còn trống |

Backend kiểm tra user/role/kho cho cả ba query. Ẩn nút ở React chỉ hỗ trợ thao tác; nó không thay thế kiểm tra quyền ở server.

## 4. Kịch bản demo khi bảo vệ

Database demo có thể đã có phiếu từ các lần kiểm thử. Không giả định danh sách trống hoặc tồn kho đang bằng 0. Chọn một Inbound còn thiếu trên màn hình tạo phiếu; nếu cần kịch bản đúng 10 máy, dùng yêu cầu mới có ExpectedQty = 10 qua module Inbound/API của nhóm. Không reset database để bắt đầu lại.

### Nhận 8 rồi nhận tiếp 2

1. Chọn **Nhân viên nhận hàng** ở bộ chọn thực hành.
2. Chọn **Tạo phiếu nhận**, chọn Inbound dự kiến 10 máy và còn thiếu 10. Bấm **Tạo phiếu nháp**.
3. Quét 8 mã khác nhau, ví dụ thêm tiền tố ngày/lần chạy để không trùng dữ liệu cũ. Backend trả số lượng 8; chưa cộng tồn.
4. Chọn bin cho dòng có hàng, bấm **Lưu nháp**. Tải lại trang để chứng minh scan/bin đã được lưu.
5. Bấm **Gửi Manager duyệt**. UI lưu bin thành công trước rồi submit bằng Version mới. Phiếu chuyển **Chờ duyệt**, không còn form chỉnh sửa.
6. Đổi sang **Manager duyệt phiếu**, mở/xem phiếu và bấm **Duyệt nhập kho → Xác nhận duyệt**.
7. Đối chiếu tiến độ Inbound: đã nhận 8, còn thiếu 2, trạng thái PartiallyReceived.
8. Đổi về Staff, tạo phiếu mới từ cùng Inbound, quét 2 mã mới, chọn bin và gửi duyệt.
9. Manager duyệt phiếu thứ hai. Tiến độ nhận đủ 10, còn thiếu 0, Inbound Completed; Inbound này không còn trong lookup tạo phiếu.

Chọn đúng SKU `GR-DEMO-SKU` và bin theo dataset. Trong database demo hiện tại, Staff ID 2, Manager ID 1, Bin A ID 1; UI dùng tên/combobox nên người dùng không cần nhập các ID này vào form phiếu.

### Nhánh từ chối và lỗi

1. Dùng Inbound khác còn thiếu, lập và gửi một phiếu hợp lệ.
2. Manager chọn **Từ chối phiếu**. Lý do rỗng hoặc chỉ khoảng trắng không được xác nhận.
3. Nhập lý do như “Hàng vỡ, thiếu phụ kiện”, xác nhận và tải lại trang. Lý do vẫn còn, phiếu Rejected, tiến độ nhận không tăng.
4. Thử serial chứa khoảng trắng hoặc quét lại mã đã có. UI báo lỗi và số lượng không tăng.
5. Để minh họa stale version, mở cùng phiếu nháp ở hai tab. Tab thứ nhất scan/lưu; tab thứ hai thao tác với dữ liệu cũ sẽ báo phiếu đã thay đổi và tải lại dữ liệu mới.

Không có thao tác xóa serial, mở lại phiếu Rejected hoặc cho Staff tăng Stock trực tiếp vì nguồn chưa có các nhánh đó.

## 5. Những hành vi quan trọng để giải thích

**Bin chưa lưu và scan:** lựa chọn bin nằm trong state riêng. Một lần scan thành công không tự xóa các lựa chọn bin này. Bước “Quét & xếp bin” chỉ được đánh dấu xong khi các dòng đã nhận có bin đã lưu.

**Version:** mỗi response write trả Version mới. Request tiếp theo dùng Version đó. Với `GR_STALE_VERSION` hoặc `GR_CONCURRENT_WRITE`, UI tải lại detail, báo người dùng xem lại và không tự gửi lại quyết định approve/reject.

**Submit:** `handleSubmit` gọi lưu draft trước, chờ response, rồi dùng Version vừa nhận để gửi duyệt. Nếu lưu bin lỗi thì dừng, không submit dữ liệu chưa lưu.

**Ngày giờ:** dữ liệu nhận lưu UTC. Bộ lọc giữ ngày người dùng chọn trên hash URL; API client đổi đầu/cuối ngày địa phương sang UTC trước khi gọi server. Ngày hiển thị dùng locale tiếng Việt.

**Màn hình lịch sử:** Approved/Rejected chỉ đọc. Nếu lookup bin tạm lỗi hoặc kho đã ngừng hoạt động, phiếu vẫn hiện được và dùng Bin ID đã lưu thay cho tên bin.

**Lỗi:** lỗi nghiệp vụ có `{ code, message }`, được đổi sang thông báo tiếng Việt. Lỗi DataAnnotations/bind là HTTP 400. Khi mất kết nối hoặc API chưa chạy, UI có thông báo và nút thử lại; chỉ báo thành công sau khi API thực sự trả thành công.

## 6. Chế độ demo và phần còn chờ tích hợp

Bộ chọn Staff/Manager chỉ xuất hiện khi `import.meta.env.DEV` và `VITE_RECEIVING_DEMO_ENABLED=true`. Client gửi ID qua `X-Development-UserId`; Backend chỉ chấp nhận header này trong Development và khi bật cấu hình riêng. Role/kho vẫn được lấy và kiểm tra trong database.

Bản build production tắt nhánh demo bằng điều kiện `DEV`. Nó cần Auth thật của Người 3 và Backend cần StockService thật của Người 2. `MockStockService` hiện ghi SQL development thật bằng DbContext chung để test transaction, nhưng chưa phải kết luận đã tích hợp module Stock của thành viên khác.

`VITE_API_PROXY_TARGET` dùng cho Vite dev server. Khi triển khai thật cần cấu hình ứng dụng/API và Auth phù hợp; lần làm này chỉ chuẩn bị code local, chưa deploy.

## 7. Kiểm chứng

```powershell
cd frontend
npm.cmd run build

# Chạy từ repository root trong terminal khác nếu cần kiểm tra Backend:
dotnet test EWMS/EWMS.sln --no-restore
```

- Build TypeScript + Vite đã thành công.
- Backend sau khi thêm lookup/session: **54/54 test qua** trên SQL Server và HTTP thật, không skip.
- Browser với API thật đã xác nhận: tạo phiếu, scan, invalid/duplicate không tăng count, giữ bin chưa lưu sau scan, stale reload, lưu/reload, submit khóa chỉnh sửa, Manager duyệt 8 rồi 2 và reject có lý do tiếng Việt.
- Đã xác nhận Manager không tạo phiếu, skip link không đổi route và bộ lọc ngày Bangkok đổi đúng sang UTC.
- Hoàn tất **44 kiểm tra trình duyệt** với API thật, gồm phím Enter/submit nhanh, offline → thử lại, fallback khi lookup bin lỗi và điều hướng Inbound cũ. Console cuối không có lỗi JavaScript.
- Desktop 1440px và mobile 375px đã được xem trực tiếp. Đã sửa lỗi nhãn hỗ trợ screen reader làm bảng tràn ngang; sau sửa, `documentElement.scrollWidth = clientWidth = 375`.
- SQL kiểm tra độc lập xác nhận luồng UI nhận đủ 10 máy: OnHandQty 10, ReservedQty 0 và đúng 10 movements; phiếu bị từ chối tạo 0 serial chính thức.
- Bản build production không chứa header `X-Development-UserId`. API/Vite/Chrome kiểm thử được dừng sau khi xác minh; dữ liệu demo được giữ để thực hành tiếp.

Báo cáo đầy đủ: [browser QA ngày 05/11/2026](../plans/20261104-goods-receipt/reports/browser-qa-20261105.md). Database demo có thêm Inbound 6/7/8 và phiếu 1–4; phiếu 4 còn Nháp, có 2 scan và bin đã lưu.

Không dùng response giả để tuyên bố tăng tồn thành công. Chi tiết transaction, migration và test SQL nằm trong [hướng dẫn Backend](goods-receipt-backend.md).
