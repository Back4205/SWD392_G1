# Bước tiếp theo: chạy và bàn giao demo Backend

Ngày: 2026-11-04. Trạng thái: thay thế bởi yêu cầu giao diện React. Người dùng đã chọn React ở câu hỏi phạm vi; chưa tạo/chạy script demo nào trong kế hoạch này.

Remote đã kiểm tra: chưa có nhánh Auth/Stock, chỉ có main, database-code-first, inbound-request và goods-receipt. Giữ phạm vi Backend đã được người dùng xác nhận; không tự triển khai module của thành viên khác.

## Kế hoạch đã rà soát

1. Thêm script khởi động dùng database demo riêng, cấu hình Development và seed hiện có.
2. Thêm script gọi API thật: tạo Inbound mới qua module của bạn cùng nhóm; Staff tạo/scan/chọn bin/submit; Manager duyệt 8 rồi 2; thử reject trên Inbound riêng.
3. Kiểm tra kết quả/progress và chặn script gọi nhầm máy chủ ngoài localhost. Script thêm dữ liệu demo mới, không reset/xóa dữ liệu cũ.
4. Chạy thực tế, ghi kết quả và bổ sung hướng dẫn.

Không thay đổi nghiệp vụ, entity hoặc migration. Không commit/push khi chưa được yêu cầu. Không lấy password/connection string thật từ appsettings.json.
