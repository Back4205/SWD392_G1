---
date: 2026-11-03
tags: [database, ef-core, sds]
---

# Hoàn thiện entity theo SDS

## Bối cảnh

Dự án có 21/34 entity và migration `InitialCreate`. Cần bổ sung toàn bộ schema để
có thể migration các quy trình xuất kho, điều chuyển, kiểm kê và theo dõi serial.

## Thay đổi và quyết định

Bổ sung 13 entity, DbSet và mapping; nối hai khóa ngoại xuất kho của
`SerialNumber`. Giữ tên bảng số ít, namespace và kiểu Fluent API đang dùng.
Tất cả quan hệ dùng `NoAction` để giữ lịch sử và tránh nhiều đường cascade trên
SQL Server.

Migration tiếp nối giữ nguyên các cột cũ, gồm ba trường duyệt `GoodsReceipt`.
Tuân theo cột ràng buộc của SDS: số điện thoại user có unique index lọc `NULL`,
email giữ không duy nhất dù phần mô tả có ghi “Unique”. Các cột audit dùng `text`
theo thiết kế. Review đã đối chiếu đủ 34 bảng, trường, kiểu dữ liệu, nullable và
khóa ngoại; không phát hiện shadow property hoặc thao tác phá hủy trong `Up`.

## Bài học và bước tiếp theo

Giữ migration tiếp nối giúp database cũ nâng cấp mà không cần tạo lại. Trước khi
áp dụng, phải xử lý số điện thoại trùng và đối chiếu các ID xuất kho đang lưu trên
serial. Chưa áp dụng migration trên SQL Server thực tế; người dùng chạy lệnh
trong [hướng dẫn migration](../database-migrations.md) với kết nối local.
