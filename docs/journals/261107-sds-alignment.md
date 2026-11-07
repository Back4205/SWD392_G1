---
date: 2026-11-07
topic: Đồng bộ triển khai Inbound với SDS
---

# Đồng bộ SDS và code nhập kho

## Bối cảnh

Nhóm demo theo class/sequence diagram. Review phát hiện Service truy cập dữ liệu trực tiếp, thiếu operation entity, policy mật khẩu khác SD09 và State Diagram mới gộp Inbound Request với Goods Receipt.

## Thay đổi

- Bổ sung và sử dụng User/GoodsReceipt/Scan/Serial/Stock Repository, cùng hợp đồng repository dùng chung và DI.
- Đưa các thao tác ghi nhận cam kết, quyết định, số lượng, trạng thái và tăng tồn vào entity; Service điều phối theo sequence.
- Tách lifecycle màn hình chi tiết frontend vào custom hooks, giữ scan/rowversion/bin chưa lưu và đường gọi API hiện có.
- Phân biệt nhánh tài khoản inactive với sai mật khẩu; đồng bộ mật khẩu mới 6–128 ký tự với SD09.
- Viết [hướng dẫn demo](../inbound-demo-guide.md) với mapping API/lớp, hai State Diagram và rehearsal nhận hai đợt.

## Quyết định

- Theo lựa chọn người dùng: giữ nghiệp vụ sequence cũ; điều chỉnh State Diagram cho thống nhất.
- Inbound: Pending → Approved → PartiallyReceived/Completed; NCC từ chối là Canceled, Manager từ chối là Rejected.
- Receipt: Draft → PendingApproval → Approved/Rejected; không tự Reject khi scan sai, không mở lại phiếu đã Reject.
- Manager duyệt mới tạo serial chính thức, movement và tăng tồn; mọi thao tác này chia sẻ transaction/DbContext.
- Không đổi schema hoặc dữ liệu ứng dụng; không triển khai ngoài phạm vi Inbound như full CRUD danh mục hoặc SMTP.

## Kiểm chứng và bước tiếp theo

AuthSequenceTests riêng và frontend lint/build đã chạy đạt trong quá trình sửa. Kết quả toàn bộ test sau thay đổi cuối cùng do phiên điều phối ghi nhận, không suy ra từ kết quả trước đó. Trước demo cần rehearsal trên dữ liệu ứng dụng và sử dụng profile trình duyệt riêng cho từng vai trò nếu muốn đăng nhập đồng thời.
