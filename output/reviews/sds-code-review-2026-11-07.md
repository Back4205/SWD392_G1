# Đối chiếu SDS với code nhập kho — 07/11/2026

## Phạm vi và kết luận

- Repo được review: `D:/Hoc Tap/Ki 7/SWD392/SWD392_G1/SWD392_G1`, branch `Bacsk/goods-receipt`, commit `11c376d`.
- Repo ngoài đang ở `feature/inbound-request`; frontend ngoài chỉ có module Inbound, không phải bản đầy đủ vừa clone.
- Nguồn: SDS PDF 112 trang do người dùng cung cấp, các ảnh swimlane/UC, code backend/frontend và bộ test hiện có.
- Đã trích xuất văn bản SDS; render và xem trực tiếp các trang 23, 24, 26, 31, 32, 37, 39, 40 cho chi tiết các diagram cần xác minh. Các sequence khác đối chiếu thêm với ảnh trong cuộc trò chuyện.
- Kết luận: luồng nghiệp vụ chính được triển khai, nhưng chưa khớp toàn bộ điều kiện, phương thức và các lifeline trong class/sequence/package diagram. Chưa thể kết luận demo thực tế đã đạt vì chưa chạy SQL integration tests hoặc thao tác trình duyệt.
- Đây là review, không sửa mã nguồn hoặc tài liệu SDS.

## Phát hiện cần xử lý trước khi demo theo sequence

### F1 — Cao khi phải trình bày code theo lifeline: thiếu các Repository thể hiện trong thiết kế

SDS vẽ UserRepository, GoodsReceiptRepository, GoodsReceiptScanRepository/SerialNumberRepository, StockRepository và IGenericRepository<T>. Code chỉ có InboundRequestRepository trong package Repositories; những Service còn lại truy cập EWMSDbContext trực tiếp.

Bằng chứng:

- `EWMS/EWMS.Api/Services/AuthService.cs:29,90,99`: truy vấn User và lưu mật khẩu trực tiếp.
- `EWMS/EWMS.Api/Services/GoodsReceiptService.cs:188,308`: tạo/tải phiếu bằng DbContext.
- `EWMS/EWMS.Api/Services/SerialNumberService.cs:46,48,67`: kiểm tra và lưu scan bằng DbContext.
- `EWMS/EWMS.Api/Services/GoodsReceiptApprovalService.cs:58,72`: tạo serial chính thức và SaveChanges trực tiếp.
- `EWMS/EWMS.Api/Services/InboundRequestService.cs:79,118,181`: xem chi tiết, cập nhật cam kết, duyệt/từ chối bỏ qua Repository; Create/GetAll vẫn dùng Repository.

Tác động: giao diện có thể hoạt động nhưng không thể chỉ ra đúng lời gọi Service → Repository theo các bước trên diagram. Không được mô tả code hiện tại là đã dùng đủ Repository như SDS.

Hướng xử lý: bổ sung Repository và nối lời gọi theo thiết kế, giữ cùng scoped DbContext/transaction; hoặc sửa diagram nếu nhóm quyết định dùng EF trực tiếp. Không cần thêm lớp rỗng chỉ để khớp tên.

### F2 — Vừa: điều kiện đổi mật khẩu 6 ký tự trong SD09 khác 12 ký tự trong code

- SDS trang 26: nhánh lỗi “New password has fewer than 6 characters”.
- `Services/AuthService.cs:88`: từ chối mật khẩu mới ngắn hơn 12.
- `DTOs/Auth/LoginDto.cs`: ChangePasswordDto có MinimumLength = 12.
- `frontend/src/features/auth/ProfilePage.tsx:25`: HTML minLength = 12.
- `frontend/src/api/http.ts`: thông báo chính sách 12–128 ký tự.

Tái hiện khi demo: mật khẩu mới 8 ký tự, khác mật khẩu cũ và xác nhận khớp sẽ bị chặn, dù vượt ngưỡng trong sequence.

Hướng xử lý: thống nhất policy trong SDS, DTO, Service, frontend và test. Không mặc định giảm policy nếu nhóm đã chọn 12 ký tự.

### F3 — Vừa: nhánh tài khoản bị vô hiệu hóa không có thông báo riêng như SD07

- SDS trang 24: đúng mật khẩu nhưng tài khoản deactivated → thông báo MSG02 riêng.
- `Services/AuthService.cs:30–32`: sai mật khẩu, không tồn tại và inactive cùng trả AUTH_INVALID_CREDENTIALS/401.
- `frontend/src/features/auth/LoginPage.tsx:17`: mọi 401 bị đổi thành “Tên đăng nhập hoặc mật khẩu chưa đúng”.

Không phải lỗi cho tài khoản inactive đăng nhập được: backend vẫn chặn. Đây là lệch nhánh kết quả quan sát được. Nếu giữ hành vi thông báo chung, sửa sequence; nếu giữ sequence, cần tách mã lỗi backend và ánh xạ frontend.

### F4 — Vừa khi giải thích class diagram: entity chỉ có properties, thiếu các operation được vẽ

- `Entities/InboundRequest.cs`: không có updateStatus, recordSupplierCommitment, recordApproval, recordRejection.
- `Entities/GoodsReceipt.cs`: không có confirm/updateStatus.
- `Entities/GoodsReceiptItem.cs`: không có updateQuantity.
- `Entities/Stock.cs`: không có increaseOnHand.
- `Entities/User.cs`: không có verifyPassword/setPasswordHash.

Logic tương ứng được thực hiện trong Service bằng gán thuộc tính và PasswordHasher. Đây là khác cách phân bổ trách nhiệm; không phải mọi thao tác đều thiếu về nghiệp vụ. Nếu giáo viên hỏi phương thức cụ thể trên entity, hiện chưa có phương thức để chỉ ra.

### F5 — Vừa khi trình bày package: frontend không đi qua Hooks theo chuỗi đã vẽ

- `frontend/src/features/inbound/InboundRequestDetailPage.tsx:20`: Page tự useEffect và gọi API.
- `frontend/src/features/goods-receipts/GoodsReceiptDetailPage.tsx:41,108`: Page tự tải dữ liệu, quản lý trạng thái và thực hiện write.
- API services nằm trong từng feature ở file `.api.ts`; API client dùng chung là `src/api/http.ts`.
- Custom hook hiện có là `src/hooks/useAppRoute.ts`, không có các data hooks trung gian như dependency chain Pages → Components → Hooks → API Services → API Client.

Feature folders tự thân hợp lý. Vấn đề là package diagram chưa phản ánh dependency thực tế. Backend Controller Inbound cũng trực tiếp dùng DbContext cho lookup/phân quyền; Common có dùng, Integrations/Email service chưa thấy trong phạm vi chức năng này.

## Bảng đối chiếu từng sequence

“Khớp nghiệp vụ” dưới đây là kết quả đọc code, không phải xác nhận đã chạy thành công trên database.

| SDS | Kết quả đối chiếu | Điểm cần biết khi demo |
|---|---|---|
| 3.1.2.1 Login / SD07 | Có kiểm tra user, password, active, tạo JWT, điều hướng theo role | F1, F3; dùng cookie HttpOnly và AuthSession, không lưu JWT trong localStorage |
| 3.1.2.2 Logout / SD08 | Có revoke session, xóa cookie, frontend về Login; có xử lý lỗi | Tất cả tab cùng browser profile dùng chung cookie; đăng nhập role khác sẽ đổi phiên dùng chung |
| 3.1.2.3 Change Password / SD09 | Có kiểm tra hiện tại, policy, confirm, khác mật khẩu cũ, băm và lưu | F1, F2, F4; thành công thu hồi mọi session và yêu cầu đăng nhập lại |
| 3.2.2.1 View Inbound List / SD01 | Có danh sách, empty state, lọc warehouse/role | GetAll qua Repository, phân quyền thêm ở Controller |
| 3.2.2.2 View Inbound Detail / SD02 | Có chi tiết, dòng sản phẩm, supplier ID, 404 | Service đọc trực tiếp DbContext; tên kho/supplier lấy thêm qua lookup |
| 3.2.2.3 Create Inbound / SD03 | Có cancel, validate, tạo Pending, lưu header/items transaction | Bản Revised trang 31 có assigned staff: đây là đúng thiết kế, không phải yêu cầu thêm |
| 3.2.2.4 Supplier Commitment / SD04 | Pending mới sửa; Accepted cập nhật SKU/qty/date; Rejected → Canceled | Logic đúng nhánh, nhưng không gọi saveCommitment/operation entity như vẽ |
| 3.2.2.5 Approve Inbound / SD05 | Có confirm/cancel, quyền Manager, tồn tại, trạng thái, lưu người duyệt và Approved | Gộp vào ReviewInboundRequestAsync thay vì approveInboundRequest riêng |
| 3.2.2.6 Reject Inbound / SD06 | Có lý do bắt buộc, quyền, trạng thái, lưu Rejected | Cùng endpoint PUT /review với Decision; không có rejectRequest riêng như tên diagram |
| 3.3.2.1 View Receipt List | Có list/empty, paging/filter, kho được phép | Qua Service rồi DbContext; diagram list vẽ Controller → Repository trực tiếp |
| 3.3.2.2 View Receipt Detail | Có detail, 404, 403 khác kho | Có thêm Service; dữ liệu DTO gồm item, scans, bin, progress, decision |
| 3.3.2.3 Create Receipt / UC17 | Approved/PartiallyReceived, còn remaining mới tạo Draft | Không cộng stock, bin ban đầu null; thiếu Repository lifeline |
| 3.3.2.4 Update/Submit / UC18 | Save draft; submit validate rồi PendingApproval; có rowversion | UI submit thực hiện PUT lưu bin rồi POST submit, không chỉ một request như hình |
| 3.3.2.5 Enter Serial / UC50 | Validate format, duplicate, persist scan, tính qty trên server | Lưu GoodsReceiptSerial tạm; SerialNumber chính thức chỉ tạo sau duyệt; thiếu scan Repository |
| 3.3.2.6 Approve/Reject Receipt / UC56 | Có GoodsReceiptApprovalService đúng sequence trang 40; approve/reject tách riêng | Register serial → stock → Approved → progress, cùng transaction; truy cập DbContext thay Repository |

## Các điểm thiết kế cần hiểu đúng, tránh sửa nhầm

1. SDS trang 40 đã có GoodsReceiptApprovalService riêng. Việc tách Service này KHÔNG tự thân là lệch sequence, dù class diagram trước đó chưa mô tả đầy đủ.
2. SDS trang 31 bản Revised có assigned staff. Các phản hồi trước khi xem PDF dựa vào ảnh nhỏ chưa đọc được chi tiết này; không coi đó là phát hiện lỗi.
3. Staff submit không tăng tồn là phù hợp swimlane và UC18/UC56. Mô tả confirmReceipt trong phần class description SDS trang 101/105 lại nói finalize/register/update stock, gây nhập nhằng. Nên thống nhất thuật ngữ “Submit for Approval” và “Manager Approve”.
4. Class diagram GoodsReceiptItem → StorageBin thể hiện bắt buộc có bin, nhưng code BinId nullable để cho phép tạo Draft/quét trước/chọn bin sau. Code kiểm tra bin bắt buộc khi submit. Nên biểu diễn rõ trạng thái Draft trong class diagram.
5. Class diagram SerialNumberService có registerSerial trong khi runtime tách lưu scan tạm ở SerialNumberService và đăng ký serial chính thức trong ApprovalService. UC50/UC56 hiện mô tả đúng ý này hơn class diagram.
6. StockUpdateService trong class diagram được triển khai thành StockReceivingService và InboundReceivingService. Tên lớp/tên hàm khác không tự động là lỗi nghiệp vụ, nhưng phải có mapping để giải thích.

## Giới hạn thao tác demo và gợi ý cải thiện

- SKU được frontend gửi từ dòng sản phẩm đã chọn (`GoodsReceiptDetailPage.tsx:111`), không có ô quét SKU độc lập. Có backend check sai SKU, nhưng muốn demo nhánh này phải dùng API; không thể tạo một payload SKU sai qua form hiện tại.
- Chưa có API/nút xóa hoặc sửa serial đã quét. Nhập nhầm một serial có format hợp lệ thì Draft chưa có cách sửa tại chỗ; tạo phiếu khác hoặc xử lý quy trình phù hợp. Đây là gợi ý vận hành, không coi là requirement bắt buộc vì sequence hiện tại không vẽ delete.
- AssignedStaffUserId được lưu và hiển thị nhưng quyền tạo phiếu nhận hiện kiểm tra membership kho, không kiểm tra đúng người được chỉ định. Nếu nhóm muốn chỉ Staff đã được Manager chọn mới được nhận, cần bổ sung rule và test; diagram hiện chưa nêu rõ độc quyền này.
- Phần lớn mã thông báo nghiệp vụ không giữ nguyên MSGxx trong SDS. Một số dùng code GR_/IN_/AUTH_, hoặc ModelState tự trả validation. Đúng ý nghĩa thông báo không đồng nghĩa khớp literal MSG trong sơ đồ.
- Cần có dữ liệu thật: Manager/Purchasing/Staff active, UserWarehouse cho cùng kho, supplier active, product active với SKU và WeightKg > 0, bin/rack/warehouse đủ tải trọng. Script start-dev không tự seed tài khoản/mặt hàng.

## Kịch bản rehearsal đề xuất

1. Mở đúng repo trong; chạy từ thư mục chứa start-dev.cmd của bản trong.
2. Manager đăng nhập, tạo request một SKU số lượng 3, chọn Staff thuộc kho. Kiểm tra Pending.
3. Purchasing đăng nhập, chọn supplier, Accepted, ngày hôm nay hoặc sau đó, xác nhận SKU/qty 3. Request vẫn Pending.
4. Manager xem lại chi tiết, mở dialog approve, thử cancel rồi approve. Kiểm tra Approved.
5. Staff tạo Goods Receipt. Kiểm tra Draft, số lượng 0, chưa có bin.
6. Thử serial có khoảng trắng → lỗi; scan SERIAL-001 → thành công; scan lại → duplicate, số lượng không tăng; scan SERIAL-002 → số lượng 2.
7. Chọn bin, lưu draft, gửi Manager. Kiểm tra PendingApproval; stock/serial chính thức chưa tăng.
8. Manager duyệt. Kiểm tra stock tăng 2, 2 serial chính thức IN_STOCK, movement, request PartiallyReceived, remaining 1.
9. Staff tạo phiếu đợt hai, scan SERIAL-003, chọn bin, submit, Manager duyệt. Kiểm tra Completed và remaining 0; không tạo thêm phiếu.
10. Dùng request/receipt riêng để demo supplier reject → Canceled, Manager reject Inbound → Rejected, Manager reject Receipt → không cộng stock.
11. Đổi mật khẩu dùng ít nhất 12 ký tự theo code hiện tại; kiểm tra bị yêu cầu đăng nhập lại.
12. Nếu dùng nhiều role đồng thời, dùng browser profile riêng; các tab cùng profile không giữ được các role độc lập với cookie hiện tại.

## Kiểm chứng đã thực hiện

- `dotnet build EWMS/EWMS.Api.Tests/EWMS.Api.Tests.csproj --no-restore --verbosity minimal`: PASS, 0 warnings, 0 errors; build cả API và project test.
- `npm.cmd run lint`: PASS.
- `npm.cmd run build`: PASS, TypeScript + Vite.
- Đã khôi phục NuGet và npm dependency. Chưa sửa source/lockfile; inner repo git status sạch sau kiểm tra.
- Bộ test hiện có bao phủ receive nhiều đợt, serial format/trùng, wrong SKU, phân quyền, capacity, stale version, rollback và auth/session. Việc đọc test KHÔNG chứng minh test pass.
- Chưa chạy integration test trên SQL: lần đầu thiếu assets; sau restore, yêu cầu chạy test SQL bằng quyền mở rộng bị automatic approval review từ chối thực thi do hết hạn mức xét duyệt. Đây là lỗi hệ thống xét duyệt, không phải kết quả test hoặc kết luận thao tác không an toàn.
- Không chạy browser end-to-end; chưa xác nhận dữ liệu demo trong database ứng dụng.

## Thứ tự xử lý

1. Thống nhất F2/F3 vì làm kết quả demo khác sequence trực tiếp.
2. Đồng bộ F1/F4/F5 trước khi trình bày cấu trúc code theo diagram.
3. Thống nhất các mô tả mâu thuẫn trong chính SDS về confirm/submit, bin nullable và vị trí đăng ký serial.
4. Chạy bộ test SQL và rehearsal trên môi trường demo trước khi xác nhận sẵn sàng.
