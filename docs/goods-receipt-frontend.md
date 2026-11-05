# React Goods Receipt — hướng dẫn đọc và sử dụng

Giao diện Người 5 gọi Backend thật để tạo Draft, scan Serial/IMEI, chọn bin, lưu nháp, gửi duyệt và Manager approve/reject. Bản hiện tại đã chuyển sang đăng nhập và URL theo vai trò. Hướng dẫn chạy trước đây bằng bộ chọn actor đã được thay thế bởi [Web EWMS theo vai trò](role-based-web.md).

## Chạy ứng dụng

Từ repository root, chạy `.\start-dev.cmd`, chọn mật khẩu khởi tạo 12–128 ký tự khi chạy lần đầu rồi mở `http://127.0.0.1:5173/login`. Đăng nhập tài khoản local `staff`, `manager` hoặc `purchasing` bằng mật khẩu người chạy đã chọn. Nhấn Enter ở lần chạy sau để giữ mật khẩu cũ; dùng `-ResetPasswords` nếu cần đặt lại. Dừng bằng `.\stop-dev.cmd`.

Script dùng database local riêng `EWMS_GoodsReceipt_Development`; giữ dữ liệu nghiệp vụ khi chạy lại. Chi tiết yêu cầu máy, cấu hình, đăng nhập, CSRF và production nằm trong [hướng dẫn web](role-based-web.md). Khi chỉ chạy React riêng, xem [frontend README](../frontend/README.md).

## Đọc source theo thứ tự

| File | Trách nhiệm |
|---|---|
| [App.tsx](../frontend/src/App.tsx) | Lấy user từ API, chuyển trang đăng nhập, menu theo role, 403/404, đăng xuất |
| [useAppRoute.ts](../frontend/src/hooks/useAppRoute.ts) | History API cho URL thật và back/forward |
| [http.ts](../frontend/src/api/http.ts) | Cookie, token CSRF, fetch chung và lỗi 401 |
| [features/auth](../frontend/src/features/auth) | Đăng nhập, thông tin tài khoản và đổi mật khẩu |
| [goodsReceipt.types.ts](../frontend/src/features/goods-receipts/goodsReceipt.types.ts) | Interface khớp DTO C# |
| [goodsReceipt.api.ts](../frontend/src/features/goods-receipts/goodsReceipt.api.ts) | URL/payload cụ thể của từng API Receipt |
| [GoodsReceiptWorkspace.tsx](../frontend/src/features/goods-receipts/GoodsReceiptWorkspace.tsx) | Chọn list/create/detail theo URL và session từ App |
| [GoodsReceiptListPage.tsx](../frontend/src/features/goods-receipts/GoodsReceiptListPage.tsx) | Filter, phân trang và hàng dữ liệu API |
| [CreateGoodsReceiptPage.tsx](../frontend/src/features/goods-receipts/CreateGoodsReceiptPage.tsx) | Chọn Inbound được phép nhận rồi tạo Draft |
| [GoodsReceiptDetailPage.tsx](../frontend/src/features/goods-receipts/GoodsReceiptDetailPage.tsx) | Handler scan/bin/save/submit, xem serial và quyết định |
| [ReceiptSerialComparison.tsx](../frontend/src/features/goods-receipts/ReceiptSerialComparison.tsx) | Đối chiếu serial nhà cung cấp với mã quét, có trạng thái theo các đợt nhận |
| [ReceiptDecisionDialog.tsx](../frontend/src/features/goods-receipts/ReceiptDecisionDialog.tsx) | Native dialog xác nhận approve/reject |
| [receipt.helpers.ts](../frontend/src/features/goods-receipts/receipt.helpers.ts) | Nhãn, thông báo lỗi và ngày giờ tiếng Việt |

Code dùng component thường, `useState`, `useEffect`, `useRef`, `fetch` và CSS. `useState` nhớ dữ liệu màn hình; `useEffect` tải dữ liệu khi mở/đổi trang; handler thực hiện thao tác khi người dùng bấm nút; `useRef` giữ focus ô scan và chặn request gửi trùng. Không đưa approve/submit vào effect.

Ví dụ scan: submit form → `handleScan` → `addReceiptSerial` → API kiểm tra và lưu → tải lại DTO → `setReceipt`. UI không tự tăng `ReceivedQty` trước khi server xác nhận.

## Các trang Receipt

| Vai trò | Trang |
|---|---|
| Staff | `/staff/receipts`, `/staff/receipts/new`, `/staff/receipts/{id}` |
| Manager | `/manager/approvals`, `/manager/receipts`, `/manager/receipts/{id}` |

Staff tạo phiếu và sửa nháp do mình tạo, thuộc kho được gán. Manager xem hàng chờ duyệt và lịch sử, kiểm tra serial/bin rồi quyết định. Purchasing xem tiến độ qua Inbound, không có quyền duyệt Receipt. Gõ URL của role khác sẽ bị chặn; API kiểm tra quyền riêng trên từng request.

Ba query Receipt phục vụ form:

| API | Dữ liệu |
|---|---|
| `GET /api/goods-receipts/session` | Session tương thích của module; App hiện dùng `/api/auth/me` |
| `GET /api/goods-receipts/lookups/inbounds` | Inbound Approved/PartiallyReceived còn thiếu trong kho Staff |
| `GET /api/goods-receipts/lookups/bins?warehouseId=...` | Bin đúng kho và tải trọng còn trống |

Inbound dùng thêm `/api/inbound-requests/lookups` cho các select kho/sản phẩm/nhà cung cấp. Tám API nghiệp vụ Receipt và DTO Version giữ như [hướng dẫn Backend](goods-receipt-backend.md).

## Thử trọn luồng với các tài khoản

1. Đăng nhập `manager`, mở **Quản lý yêu cầu nhập**, tạo yêu cầu với kho và số lượng dự kiến 10.
2. Đăng xuất, đăng nhập `purchasing`, mở yêu cầu vừa tạo, chọn nhà cung cấp, xác nhận/ngày giao và khai báo đủ 10 serial cho từng SKU theo số lượng kế hoạch. Mỗi serial một dòng; SKU/số lượng hiển thị chỉ đọc.
3. Đăng nhập `manager`, xem và duyệt yêu cầu nhập.
4. Đăng nhập `staff`, tạo phiếu từ yêu cầu đó, quét 8 serial có trong bảng kê và đúng SKU, rồi chọn bin. Mã sai bị chặn, không tăng số lượng. Bảng đối chiếu hiển thị dự kiến cạnh mã thực tế.
5. **Lưu nháp**, tải lại trang để kiểm tra scan/bin vẫn còn. **Gửi Manager duyệt** sẽ lưu bin trước rồi submit bằng Version mới.
6. Đăng nhập `manager`, mở phiếu tại **Phiếu chờ phê duyệt**, chọn **Duyệt nhập kho → Xác nhận duyệt**. Inbound đã nhận 8, còn 2, trạng thái PartiallyReceived.
7. Staff tạo phiếu thứ hai từ cùng Inbound, nhận 2 mã còn lại trong bảng kê và gửi duyệt. Manager duyệt lần hai; Inbound Completed, còn 0 và không còn trong lookup nhận hàng.
8. Purchasing xem lại tiến độ đã nhận đủ từ trang chi tiết Inbound.

Mật khẩu có thể khác nhau sau khi từng tài khoản đổi trong hồ sơ. Mỗi lần đổi role để thử là đăng xuất/đăng nhập thật, không đổi ID trong trình duyệt. Không giả định dataset đang rỗng; khai báo mã serial mới chưa được sử dụng cho từng yêu cầu mới.

Để thử nhánh từ chối, gửi một phiếu khác rồi Manager chọn **Từ chối phiếu**, nhập lý do tiếng Việt và xác nhận. Lý do rỗng/khoảng trắng bị chặn; Rejected không cộng tồn hay tiến độ. Không có thao tác xóa scan, mở lại hoặc gửi lại phiếu Rejected vì nguồn nghiệp vụ chưa có các nhánh đó.

## Hành vi cần giải thích khi review

- **Bin đang chọn:** giữ trong state riêng. Scan thành công không làm mất bin chưa lưu; bước xếp bin chỉ hoàn tất khi đã lưu đủ vị trí cho các dòng có hàng.
- **Version:** mỗi write thành công trả Version mới. Nếu stale/concurrent, tải lại detail và kiểm tra dữ liệu, không tự gửi lại approve/reject.
- **Submit:** lưu Draft thành công trước, sau đó submit với Version vừa nhận. Lỗi lưu bin thì dừng, không gửi duyệt.
- **Pending/Approved/Rejected:** không cho sửa. Chỉ Manager approve mới tạo serial chính thức, movement và tồn kho.
- **Ngày giờ:** URL giữ ngày người dùng chọn; API client chuyển đầu/cuối ngày địa phương sang UTC để lọc. Hiển thị locale tiếng Việt.
- **Hết phiên:** chuyển về đăng nhập. **Mất mạng:** báo lỗi và cho thử lại. Thành công chỉ hiển thị khi API thực sự thành công.
- **Lịch sử:** nếu lookup bin lỗi, chi tiết vẫn hiển thị mã bin đã lưu và có nút tải lại tên/vị trí.

## Kiểm chứng hiện tại và lịch sử

```powershell
# Tại frontend:
npm.cmd run build

# Tại repository root:
dotnet test EWMS/EWMS.sln --no-restore
```

React build đã qua. Backend hiện có **61/61 test SQL/HTTP qua** với đăng nhập thật, CSRF, quyền kho/role và các kiểm thử Receipt trước đó. Browser giai đoạn hiện tại đã chạy luồng ba tài khoản từ tạo Inbound tới nhận một phần và nhận đủ; báo cáo cuối đặt tại `plans/20261105-role-based-web/reports/`.

[Báo cáo browser cũ](../plans/20261104-goods-receipt/reports/browser-qa-20261105.md) ghi 44 kiểm tra của giai đoạn giao diện trước. Báo cáo này là lịch sử, không còn là hướng dẫn đăng nhập hoặc nguồn cấu hình hiện tại.

Chưa deploy. Phần cộng tồn nhận hàng dùng SQL thật trong `StockReceivingService` và transaction chung; không tuyên bố đã hoàn thiện các phân hệ Stock/Auth Admin khác của nhóm.

## Bảng kê và kiểm chứng hiện tại

[Bảng kê nhà cung cấp và đối chiếu](supplier-serial-reconciliation.md) giải thích các trạng thái chờ nhận/quét phiếu này/đang ở phiếu khác/đã nhận chính thức, nhánh từ chối và dữ liệu lịch sử chưa có bảng kê. `SupplierCommitmentForm` xử lý serial theo từng SKU; `SupplierManifestPanel` hiển thị dữ liệu đã lưu; `ReceiptSerialComparison` đặt mã dự kiến cạnh `ScannedSerialCode`, không tự tạo scan từ danh sách nhà cung cấp.

API hiện qua 80/80 test SQL/HTTP (61 cũ + 19 mới), React lint/build qua với 44 modules. Browser của tính năng này được ghi riêng trong [kế hoạch bảng kê](../plans/20261105-supplier-serial-reconciliation/plan.md), không dùng báo cáo demo cũ thay thế.
