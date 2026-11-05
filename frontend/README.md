# EWMS Frontend

React 18 + TypeScript + Vite cho nghiệp vụ nhập kho. Người dùng đăng nhập bằng tài khoản thật; menu, URL và thao tác được chia theo vai trò. API kiểm tra lại quyền và kho được phân công trên từng request.

## Chạy local

Cần .NET 8 SDK, SQL Server local, `dotnet-ef` 8.x và Node.js/npm phù hợp với `package-lock.json`. Từ thư mục gốc repository:

```powershell
.\start-dev.cmd
```

File `.cmd` gọi PowerShell riêng để chạy `start-dev.ps1`, dùng được khi terminal VS Code chặn chạy trực tiếp `.ps1`. Không thay đổi Execution Policy của máy. CMD dùng `start-dev.cmd` khi đang ở thư mục gốc repository.

Script build Backend, cập nhật database riêng `EWMS_GoodsReceipt_Development`, khởi tạo dữ liệu local và chạy API/React. Lần đầu nhập mật khẩu 12–128 ký tự tại lời nhắc. Các lần sau nhấn Enter để giữ mật khẩu hiện tại.

Mở `http://127.0.0.1:5173/login`. Tài khoản local:

| Tên đăng nhập | Vai trò | Trang mặc định |
|---|---|---|
| `staff` | Nhân viên kho | `/staff/receipts` |
| `manager` | Quản lý kho | `/manager/approvals` |
| `purchasing` | Quản lý mua hàng | `/purchasing/inbound-requests` |

Mật khẩu là giá trị người chạy đã chọn; source và giao diện không có mật khẩu mặc định. Để đặt lại mật khẩu local: dừng ứng dụng rồi chạy `.\start-dev.cmd -ResetPasswords`. Việc này thu hồi các phiên của tài khoản local, giữ dữ liệu nhập hàng.

Dừng hai tiến trình do script tạo:

```powershell
.\stop-dev.cmd
```

Log nằm ở `.local/`. Script không sửa `appsettings.json` của nhóm. Nếu một cổng đã có ứng dụng khác dùng, dừng ứng dụng đó trước; script không tự dừng tiến trình lạ.

## Chạy React riêng

Khi Backend đã chạy tại `http://localhost:5085`, từ thư mục `frontend`:

```powershell
npm.cmd ci
Copy-Item .env.example .env.local
npm.cmd run dev -- --host 127.0.0.1 --port 5173
```

Trên Windows dùng `npm.cmd` nếu `npm.ps1` bị chặn. `.env.local` chỉ cần:

```dotenv
VITE_API_PROXY_TARGET=http://localhost:5085
```

Vite proxy `/api` sang Backend. Khởi động lại Vite sau khi đổi cấu hình. Cookie đăng nhập do Backend cấp; không có cấu hình chọn actor hoặc gửi user ID giả lập.

## Đọc source

| File/thư mục | Trách nhiệm |
|---|---|
| `src/App.tsx` | Tải phiên đăng nhập, bảo vệ route, tạo menu theo role và đăng xuất |
| `src/hooks/useAppRoute.ts` | URL thật, back/forward bằng History API |
| `src/api/http.ts` | Fetch chung, cookie, CSRF và xử lý hết phiên |
| `src/features/auth/` | Đăng nhập, hồ sơ và đổi mật khẩu |
| `src/features/inbound/` | Tạo, xem, cam kết nhà cung cấp, duyệt và tiến độ Inbound |
| `src/features/goods-receipts/` | Tạo Draft, scan Serial/IMEI, chọn bin, submit và Manager approve/reject |

Code dùng component thường, `useState`, `useEffect`, `useRef`, `fetch` và CSS; không thêm thư viện router hoặc state management.

## Kiểm tra

```powershell
npm.cmd run lint
npm.cmd run build
```

TypeScript/Vite build và ESLint qua. Backend đã qua 80/80 test SQL/HTTP: 61 ca Auth/Receipt cũ và 19 ca bảng kê/đối chiếu mới. Chi tiết quyền, phiên đăng nhập và giới hạn tích hợp: [Web theo role](../docs/role-based-web.md). Luồng Receipt và thứ tự đọc component: [Frontend Goods Receipt](../docs/goods-receipt-frontend.md).

## Bảng kê hàng nhà cung cấp

Purchasing phải nhập đủ serial cho từng SKU khi xác nhận cung cấp. SKU và số lượng lấy từ kế hoạch Manager; serial nhập mỗi mã một dòng. Manager chỉ duyệt Inbound mới sau khi bảng kê hợp lệ. Staff quét thiết bị thực tế, xem bảng so sánh dự kiến/thực nhận; mã ngoài bảng kê hoặc sai SKU bị chặn, không tăng số lượng. Chỉ Manager duyệt Receipt mới cộng tồn. Xem [hướng dẫn bảng kê và đối chiếu](../docs/supplier-serial-reconciliation.md).
