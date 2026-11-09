---
date: 2026-11-07
topic: Dọn code không sử dụng trong luồng nhập kho
---

# Dọn code không sử dụng

## Bối cảnh

Luồng nhập kho đang hoạt động. Phạm vi lần này chỉ bỏ code thừa đã xác minh, giữ hành vi demo theo SDS.

## Thay đổi

- Bỏ năm phương thức Inbound Repository không có nơi gọi: `WarehouseExistsAsync`, `ProductVariantExistsAsync`, `UserExistsAsync`, `SupplierExistsAsync` và `UpdateAsync`, cùng khai báo interface tương ứng. Các kiểm tra Active/phân quyền đang dùng được giữ; thao tác `Update` dùng chung vẫn có trong GenericRepository.
- Bỏ các `using` dư đã được ImplicitUsings cung cấp.
- Bỏ wrapper frontend `getReceiptSession` không được sử dụng và type import liên quan; giữ endpoint backend `/api/goods-receipts/session`.
- Bỏ alias reexport `ReceiptApiError`; helper nhập trực tiếp cùng lớp `ApiError` từ HTTP client nên kiểm tra `instanceof` giữ nguyên.
- Bỏ ba CSS rule không có phần tử phù hợp: `.app-brand-mark svg`, `.app-nav-item svg`, `.app-nav-item:disabled`. Giao diện hiện dùng chữ trong span và liên kết anchor.

## Giới hạn và kiểm chứng

- Không đổi API HTTP, state, schema hoặc giao diện hiện có. Các phương thức entity/repository cần cho class/sequence SDS vẫn được giữ.
- Giữ nguyên các thay đổi receipt/font và file `seed_data.sql`, `FixVietnameseFontEncodingTest.cs` của người dùng.
- Frontend lint và build đã đạt sau cleanup; review độc lập không phát hiện hồi quy cụ thể trong diff cleanup.
- Backend build bằng output tạm riêng đạt 0 warning/error; giữ ứng dụng đang chạy. 29 test chọn lọc đạt, gồm 26 test Auth/entity, luồng Goods Receipt qua HTTP và hai nhánh duyệt/từ chối Inbound qua HTTP. File tiện ích sửa database development được loại khỏi lần chạy.

## Bước tiếp theo

Rehearsal luồng trên môi trường demo khi cần. Không mở rộng cleanup sang chức năng nghiệp vụ hoặc dữ liệu ứng dụng.
