# Sửa tên tiếng Việt bị dấu hỏi

Chuỗi như `Qu?n lý kho` đã mất ký tự trong dữ liệu; đổi font không khôi phục được dấu. `FullName` trong snapshot cũ là varchar, dù model hiện đã dùng nvarchar.

Migration `PreserveVietnameseText` đồng bộ 15 cột tên/mô tả tiếng Việt sang nvarchar, giữ kiểu dữ liệu của username, role key, mật khẩu, SKU và serial. Khi SQL Server hoạt động, script start-dev hiện có sẽ áp migration này.

## Khôi phục các trường chữ trong dữ liệu demo

Sau khi migration thành công, tại thư mục repo bên trong chạy:

```cmd
scripts\repair-demo-text.cmd
```

Script đối chiếu 99 ánh xạ từ `seed_data.sql` và dữ liệu gốc đã có trong tiện ích sửa tên của dự án. Phạm vi gồm tài khoản, kho/địa chỉ, khu kho, bin, danh mục, sản phẩm/biến thể và nhà cung cấp. Chỉ giá trị khớp chính xác với dạng varchar mất dấu của dữ liệu gốc mới được cập nhật. Không chạy lại seed, tạo/xóa bản ghi hoặc đổi mật khẩu, role, UserWarehouse, tồn kho hay đơn hàng. Tên tùy chỉnh không khớp được giữ nguyên; danh mục có xung đột tên được bỏ qua thay vì xóa/gộp dữ liệu.

Trước khi sửa, script lưu bản sao toàn bộ các trường chữ và ID vào `.local/unicode-text-before-*.json`. Báo cáo sau sửa ở `.local/unicode-text-after-*.json`. Chạy lại khi dữ liệu đã đúng sẽ không cập nhật thêm. Lệnh cũ `scripts\repair-demo-user-names.cmd` chuyển tiếp sang script này.

Chỉ kiểm tra, không cập nhật:

```cmd
scripts\repair-demo-text.cmd -AuditOnly
```

Tải lại trang hoặc đăng nhập lại để lấy FullName mới. Trong lúc tên lưu trong database còn hỏng, giao diện dùng username thật thay cho tên có `?`/ký tự thay thế; đây là fallback hiển thị, không phải khôi phục dữ liệu.

Tên role được dịch tiếng Việt cho Admin, Manager, Purchasing Manager, Warehouse Staff và Sales Staff; role key phục vụ phân quyền vẫn giữ nguyên.

## Kết quả kiểm tra ngày 07/11/2026

- Đã áp dụng migration `PreserveVietnameseText` vào database development.
- Trước sửa: kiểm tra 104 giá trị, phát hiện 46 giá trị có dấu hiệu hỏng.
- Khôi phục 50 giá trị, gồm cả các chữ bị mất dấu nhưng không chứa `?` khi đối chiếu chính xác với bản gốc.
- Sau sửa: 0 giá trị nghi lỗi trong 15 trường được kiểm tra; chạy lại sửa 0 giá trị.
- Đã đọc lại trực tiếp `Kho Tổng Hà Nội` và tên đúng dấu của 5 tài khoản demo. Người dùng cần tải lại trang/đăng nhập lại để giao diện lấy dữ liệu mới.
