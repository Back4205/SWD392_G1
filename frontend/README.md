# EWMS Frontend

React 18 + TypeScript + Vite cho hệ thống quản lý kho thiết bị điện tử. Có hai khu vực:

- **Yêu cầu nhập kho:** các màn hình Inbound của nhóm.
- **Phiếu nhận hàng:** tạo Draft, scan Serial/IMEI, chọn bin, gửi duyệt và Manager approve/reject.

Giao diện gọi Backend .NET thật. Quy tắc quyền, số lượng, remaining và cập nhật tồn kho được kiểm tra ở server.

## Chạy nhanh

Cần Backend đã chạy ở `http://localhost:5085` với database demo riêng. Xem [các lệnh Backend đầy đủ](../docs/goods-receipt-frontend.md#1-chạy-bằng-hai-terminal).

Từ thư mục `frontend`, dùng PowerShell:

```powershell
npm.cmd ci

if (-not (Test-Path .env.local)) {
    Copy-Item .env.example .env.local
}

npm.cmd run dev -- --host 127.0.0.1 --port 5173
```

Mở `http://127.0.0.1:5173/`. Dùng `npm.cmd` trên Windows nếu `npm.ps1` bị chặn; không cần đổi Execution Policy.

`.env.local` dùng cấu hình mẫu:

```dotenv
VITE_API_PROXY_TARGET=http://localhost:5085
VITE_RECEIVING_DEMO_ENABLED=true
VITE_RECEIVING_STAFF_ID=2
VITE_RECEIVING_MANAGER_ID=1
```

Vite proxy `/api` sang Backend. Sau khi sửa `.env.local`, khởi động lại Vite. ID Staff/Manager cần khớp log seeder ở database đang dùng.

Backend phải có `ASPNETCORE_ENVIRONMENT=Development`, `GoodsReceipts__UseDevelopmentUserHeader=true` và dataset demo. Không bật chế độ giả lập cho production. Role thật vẫn do API session trả về và Backend kiểm tra.

## Đọc source

```text
src/
  App.tsx                         # Shell và điều hướng hai module
  hooks/useHashRoute.ts            # Theo dõi URL sau dấu #
  features/
    inbound/                      # UI Inbound đã có
    goods-receipts/
      goodsReceipt.types.ts       # Interface khớp DTO Backend
      goodsReceipt.api.ts         # Fetch + các API cụ thể
      GoodsReceiptWorkspace.tsx   # Session, actor demo, chọn trang
      GoodsReceiptListPage.tsx    # Danh sách, lọc, phân trang
      CreateGoodsReceiptPage.tsx  # Chọn Inbound và tạo nháp
      GoodsReceiptDetailPage.tsx  # Scan/bin/save/submit/review
      ReceiptDecisionDialog.tsx   # Xác nhận duyệt/từ chối
      receipt.helpers.ts         # Nhãn trạng thái, lỗi, ngày giờ
      goodsReceipt.css           # CSS riêng module
```

Code dùng component, `useState`, `useEffect`, `useRef`, `fetch` và CSS thường. Không thêm thư viện router, UI hoặc state management.

Các URL chính:

- `/#/goods-receipts`: danh sách phiếu.
- `/#/goods-receipts/new`: tạo nháp.
- `/#/goods-receipts/123`: chi tiết phiếu có ID 123.
- `/#/goods-receipts?status=PendingApproval`: phiếu chờ Manager.
- `/#/inbound`: module Inbound.

## Build và kiểm tra

```powershell
npm.cmd run build
```

Lệnh chạy TypeScript rồi tạo bản build trong `dist/`. Bộ chọn actor demo bị tắt trong bản build production bởi `import.meta.env.DEV`. Auth và Stock thật của nhóm vẫn cần được tích hợp trước khi triển khai toàn hệ thống.

## Hướng dẫn nghiệp vụ và demo

Đọc [React Goods Receipt — hướng dẫn đọc, chạy và demo](../docs/goods-receipt-frontend.md) để xem thứ tự đọc code, kịch bản nhận 8 rồi 2, reject, lỗi version và kết quả kiểm thử.

Chi tiết API/transaction/migration: [hướng dẫn Backend](../docs/goods-receipt-backend.md).
