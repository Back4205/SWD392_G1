# Kiểm chứng web phân quyền — 05/11/2026

Phạm vi là module Người 5 và các màn hình Inbound liên quan đã có trong source. Không nhận là toàn bộ các phân hệ EWMS đã hoàn tất hoặc đã deploy.

## Build và test tự động

| Kiểm tra | Kết quả |
|---|---|
| Backend .NET build | Qua, 0 warning/error |
| SQL/HTTP regression | 61/61 qua, 0 skip; database test riêng theo GUID |
| React TypeScript/Vite production build | Qua, 41 modules |
| React ESLint | Qua, 0 lỗi |
| `git diff --check` | Qua |

Log test local: `EWMS/EWMS.Api.Tests/obj/auth-full-result.log`. Không dùng kết quả 54 tests/44 browser checks trước đây thay cho lần kiểm tra đăng nhập này.

## Kiểm tra trên Chrome với API và SQL thật

Tài khoản được khởi tạo bằng mật khẩu ngẫu nhiên chỉ phục vụ QA; không lưu mật khẩu vào tài liệu/source. Browser nhập tài khoản và mật khẩu qua form đăng nhập. Không dùng header giả lập actor.

| Kịch bản | Kết quả quan sát |
|---|---|
| Staff đăng nhập | Vào `/staff/receipts`; menu nhận hàng, kế hoạch nhập và tài khoản; không có menu quản lý duyệt |
| Manager đăng nhập | Vào `/manager/approvals`; danh sách luôn lọc PendingApproval; có tra cứu Receipt và quản lý Inbound |
| Purchasing đăng nhập | Vào `/purchasing/inbound-requests`; có cam kết nhà cung cấp/tiến độ; không có nút tạo hoặc duyệt yêu cầu |
| Đường dẫn sai vai trò | Staff/Purchasing mở URL Manager nhận trang 403 |
| Gọi API ngoài quyền | Purchasing đọc API Receipt bị 403; Staff gọi duyệt không được thực hiện |
| Phiên đăng nhập | Identity lấy từ `/api/auth/me`; JWT không đọc được qua `document.cookie`; không có actor picker |
| Đăng xuất | Trở về login; `/api/auth/me` trả 401 |
| Phiên bị thu hồi | Request tiếp theo đưa người dùng về login |
| Đổi mật khẩu | Mật khẩu hiện tại sai có thông báo; đổi thành công kết thúc phiên; mật khẩu cũ bị từ chối, mật khẩu mới đăng nhập được |
| URL và tải lại | Reload `/manager/receipts/8` giữ đúng trang và nhận diện phiên qua API |
| Mobile | Kiểm tra ở 390×844: Inbound, Receipt, hồ sơ; không tràn ngang body, tên tài khoản và đăng xuất vẫn hiển thị |
| Desktop | Kiểm tra bố cục sidebar, menu và chi tiết Receipt ở 1440×1000 |

Luồng nghiệp vụ trên giao diện:

1. Manager tạo Inbound #9 bằng kho/sản phẩm từ lookup, số lượng 2.
2. Purchasing cập nhật nhà cung cấp và ngày giao; Manager duyệt Inbound.
3. Staff mở shortcut từ Inbound; yêu cầu được chọn sẵn, tạo Receipt #6.
4. Quét một serial: số lượng tăng thành 1; quét trùng: báo lỗi, số lượng vẫn 1.
5. Chọn bin và gửi duyệt: scanner đóng, phiếu xuất hiện trong queue của Manager.
6. Manager duyệt: Receipt Approved; Inbound PartiallyReceived; tiến độ chính thức 1 nhận/1 còn thiếu.
7. Nhận đợt thứ hai bằng Receipt #7 và duyệt: Inbound Completed; tiến độ chính thức 2 nhận/0 còn thiếu; Purchasing thấy 100.0%.
8. Tạo Receipt #8 từ Inbound #2, quét/chọn bin/gửi duyệt. Manager từ chối: lý do rỗng bị HTML validation chặn, lý do chỉ khoảng trắng bị kiểm tra trong form chặn. Lý do tiếng Việt được lưu; không còn nút duyệt; số lượng nhận chính thức của Inbound #2 vẫn bằng 0.

Hai lần assertion QA được sửa vì kỳ vọng kiểm thử sai: tiến độ hiển thị `100.0%` và form từ chối dùng `required` cùng validate khoảng trắng, không dựa vào nút disabled. Sau khi sửa assertion, các hành vi trên được kiểm chứng. Không thay đổi nghiệp vụ để làm assertion qua.

## Script và dữ liệu local

- Chạy thật `start-dev.ps1 -ResetPasswords`: build, migration, hash mật khẩu và mở API/React thành công.
- Chạy lại không reset, Enter ở prompt: giữ mật khẩu, dữ liệu nghiệp vụ và JWT signing key; phiên còn hiệu lực qua restart.
- `stop-dev.ps1` dừng đúng hai process được ghi nhận và giải phóng cổng 5085/5173. PID được đối chiếu cùng tên và UTC ticks để không nhầm process dùng lại PID.
- Hai biến process trong script khởi động được khởi tạo null để nhánh cleanup không lấy nhầm biến từ caller.
- Dataset local vẫn có mã `GR-DEMO-*` từ giai đoạn trước; đây là dữ liệu SQL mẫu. Các cơ chế đăng nhập/role hiện tại đều được thực thi thật.
- `User.FullName` giữ nguyên kiểu varchar theo schema hiện có. Tên tài khoản bootstrap dùng ASCII để không mất ký tự; nhãn role trên web vẫn tiếng Việt.
- Không reset/xóa lịch sử Receipt, serial, tồn kho hoặc Inbound. Dừng các server QA sau kiểm tra.

## Review và Git

Reviewer đã kiểm tra quyền API/kho, route guard, session/CSRF, giữ nguyên transaction nhận hàng và xử lý lỗi. Các vấn đề phát hiện được sửa: độ chính xác thời gian process trong stop script; username maxlength; cleanup process kế thừa từ caller; CSS ancestor sau khi tách layout role.

Nhánh `Bacsk/goods-receipt`. Chưa stage, commit hoặc push. `appsettings.json` của người dùng và migration Receipt cũ không đổi. Khóa JWT, log, PID, `.env.local`, `node_modules` và các file QA tạm được Git bỏ qua; script chứa credential QA đã được xóa sau chạy.
