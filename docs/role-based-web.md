# Web EWMS theo vai trò

Bản hiện tại dùng đăng nhập username/password và JWT theo hướng thiết kế Auth trong SDS. Người dùng không còn chọn actor trên giao diện. React tạo menu từ phiên do API trả về; Backend kiểm tra role, tài khoản Active và phạm vi kho trước khi cho thao tác.

Phạm vi là Goods Receipt, Serial/IMEI, Receipt Approval và các màn hình Inbound liên quan đã có trong repository. Bảng dưới mô tả quyền của module này theo guide Người 5; không đại diện cho toàn bộ quyền Admin/Sales hoặc các phân hệ EWMS chưa triển khai.

## Màn hình và quyền

| Vai trò | URL | Công việc |
|---|---|---|
| Chưa đăng nhập | `/login` | Đăng nhập bằng tên và mật khẩu |
| Warehouse Staff | `/staff/receipts` | Danh sách phiếu trong kho được phân công |
| Warehouse Staff | `/staff/receipts/new` | Chọn Inbound được duyệt, còn thiếu và tạo Draft |
| Warehouse Staff | `/staff/receipts/{id}` | Chủ nháp scan serial, chọn bin, lưu và gửi duyệt; phiếu đã gửi chỉ xem |
| Warehouse Staff | `/staff/inbound-requests` và `/{id}` | Xem kế hoạch được phép nhận, tiến độ và mở tạo phiếu |
| Manager | `/manager/approvals` | Danh sách cố định trạng thái PendingApproval |
| Manager | `/manager/receipts` và `/{id}` | Tra cứu, xem serial/bin và duyệt hoặc từ chối phiếu pending |
| Manager | `/manager/inbound-requests`, `/new`, `/{id}` | Tạo, xem và duyệt/từ chối yêu cầu nhập |
| Purchasing Manager | `/purchasing/inbound-requests` và `/{id}` | Cập nhật cam kết và đủ bảng kê SKU/serial khi còn Pending; theo dõi số đã nhận/còn thiếu |
| Đã đăng nhập | `/{staff hoặc manager hoặc purchasing}/profile` | Xem hồ sơ và đổi mật khẩu |

Mỗi vai trò chỉ có các mục menu tương ứng. Gõ URL của vai trò khác hiển thị 403; URL không tồn tại hiển thị 404. Ẩn nút ở React hỗ trợ người dùng thao tác đúng, còn API mới quyết định có được thực hiện hay không. Purchasing không duyệt Receipt; Manager không scan hoặc tạo phiếu nhận thay Staff.

Tất cả dữ liệu nghiệp vụ đều bị giới hạn bởi `UserWarehouse`. Staff chỉ sửa Draft do chính mình tạo. Inbound Pending/Rejected không được Staff dùng để nhận hàng. Form tạo Inbound chọn kho/sản phẩm/nhà cung cấp từ lookup API thật thay vì nhập ID.

## Bảng kê SKU/serial và đối chiếu

Purchasing nhập đủ serial cho từng SKU của kế hoạch khi nhà cung cấp xác nhận cung cấp. Manager chỉ duyệt Inbound mới sau khi bảng kê đầy đủ. Staff vẫn quét thiết bị thật; mã ngoài bảng kê hoặc sai SKU bị chặn và không làm tăng số lượng. Trang Receipt đặt mã dự kiến cạnh mã thực quét, chỉ rõ phiếu này/phiếu khác/đã nhận chính thức và hỗ trợ nhận nhiều đợt. Khai báo chưa cộng tồn.

Đây là phần mở rộng theo yêu cầu người dùng từ thông tin confirmed product details của UC12. Inbound đã duyệt trước migration được ghi rõ chưa có bảng kê; không backfill giả. Chi tiết payload, trạng thái và validation: [Bảng kê nhà cung cấp và đối chiếu](supplier-serial-reconciliation.md).

## Đăng nhập và vòng đời phiên

1. React lấy token bảo vệ tại `GET /api/auth/csrf`.
2. `POST /api/auth/login` gửi `{userName,password}` kèm header `X-CSRF-TOKEN`.
3. `AuthService` tìm user, kiểm tra Active và password hash bằng `PasswordHasher<User>`, tạo `AuthSession`; `TokenService` ký JWT có identity, role, permission và mã phiên.
4. Máy chủ đặt JWT trong cookie `.EWMS.Session` có HttpOnly/SameSite Strict. Web không lưu JWT trong localStorage. Response vẫn có token theo DTO Auth của SDS, nhưng React chỉ sử dụng thông tin user.
5. React tải `GET /api/auth/me` để mở đúng khu vực. Cookie được trình duyệt gửi theo request; các thao tác ghi có token CSRF.
6. JWT được kiểm tra chữ ký, thời hạn và phiên trong database. Logout, user bị khóa hoặc đổi role làm phiên cũ không còn hợp lệ.

`POST /api/auth/logout` thu hồi phiên hiện tại. `POST /api/auth/change-password` nhận mật khẩu hiện tại, mật khẩu mới và xác nhận; mật khẩu mới dài 12–128 ký tự, khác mật khẩu cũ. Thành công sẽ thu hồi mọi phiên của user và yêu cầu đăng nhập lại. React thông báo thay đổi phiên giữa các tab bằng một giá trị thời gian, không lưu token hay mật khẩu trong localStorage.

Phiên mặc định kéo dài 8 giờ. Đăng nhập sai trả lỗi chung để không tiết lộ tên tài khoản; endpoint đăng nhập giới hạn 10 request/phút theo IP. Request nghiệp vụ hết phiên trả 401 và web đưa về trang đăng nhập. Lỗi mạng có thông báo và nút thử lại.

## Chạy bản local

Từ repository root:

```powershell
.\start-dev.cmd
```

Launcher `.cmd` chạy `start-dev.ps1` trong một tiến trình PowerShell riêng với `-ExecutionPolicy Bypass`, không thay đổi policy của máy. Dùng cách này khi terminal VS Code báo `running scripts is disabled on this system`. Trong CMD có thể gõ `start-dev.cmd` từ thư mục gốc repository.

Nếu báo cổng 5085/5173 đang được sử dụng và web đã mở được, không cần chạy thêm một lần. Khi muốn khởi động lại phiên do launcher tạo, chạy `.\stop-dev.cmd` trước, rồi `.\start-dev.cmd`. Dùng thêm `-ResetPasswords` nếu cần tự chọn lại mật khẩu local.

Script dùng SQL Server instance `.` và database riêng `EWMS_GoodsReceipt_Development`, chạy build/migration trước khi mở API và React. Nó không sửa connection string trong `appsettings.json`. Seeder chỉ cho phép Development và đúng tên database trên; chạy lại giữ dữ liệu nghiệp vụ đã có.

Lần đầu, nhập mật khẩu khởi tạo 12–128 ký tự tại lời nhắc ẩn. Đăng nhập bằng `staff`, `manager` hoặc `purchasing` cùng mật khẩu đã chọn. Sau đó mỗi tài khoản có thể đổi mật khẩu riêng trong trang hồ sơ. Không có mật khẩu mẫu trong source hoặc trên màn hình đăng nhập.

```powershell
# Khi cần đặt lại mật khẩu cho các tài khoản local:
.\stop-dev.cmd
.\start-dev.cmd -ResetPasswords
```

Nếu đã khởi tạo và không reset, nhấn Enter ở lời nhắc mật khẩu để giữ giá trị cũ. Để dừng tiến trình API/React do script tạo, dùng `.\stop-dev.cmd`; script đối chiếu PID, tên và thời điểm bắt đầu để tránh dừng nhầm tiến trình khác. Log/PID nằm trong `.local/` được Git bỏ qua.

Mở `http://127.0.0.1:5173/login`. Backend chạy tại `http://localhost:5085`. Dữ liệu local có thể còn mã `GR-DEMO-*` từ dataset thử trước đây; đó là dữ liệu SQL mẫu, không phải cơ chế bỏ qua đăng nhập. Không giả định số lượng/tồn kho ban đầu bằng 0 sau khi đã chạy luồng nhận hàng.

Tên bootstrap dùng chữ không dấu vì `User.FullName` hiện là varchar theo schema của nhóm; nhãn vai trò và giao diện vẫn tiếng Việt. Không thay schema các phân hệ khác trong đợt này.

## Đọc code và DI

Luồng Receipt giữ cấu trúc dễ đọc: **Controller → Service → DbContext**. DTO map thủ công; không AutoMapper/MediatR/CQRS/Unit of Work.

```csharp
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddScoped<IStockService, StockReceivingService>();
builder.Services.AddScoped<IInboundService, InboundReceivingService>();
builder.Services.AddEwmsAuthentication(builder.Configuration, builder.Environment);
```

`AddScoped<Interface, Class>()` nghĩa là khi một request cần interface, DI tạo class tương ứng. Các service và DbContext của request cùng scope nên dùng chung transaction.

`StockReceivingService` kiểm tra tải trọng, cộng `OnHandQty` trong SQL và giữ `ReservedQty`. Đây là phần tồn kho phục vụ nhận hàng; không phải toàn bộ module kho của Người 2. `InboundReceivingService` tính lại tiến độ bằng receipt Approved. Cả hai dùng chung DbContext với service duyệt phiếu, không tự commit riêng.

Khi Approve, `using var transaction = await _context.Database.BeginTransactionAsync(...)` bao quanh đăng ký serial, movement, cộng tồn, lưu Approved và cập nhật Inbound. Lỗi một bước sẽ rollback mọi bước; không để hàng tăng tồn nhưng phiếu vẫn chưa duyệt. Tài liệu nghiệp vụ chi tiết nằm tại [Backend Goods Receipt](goods-receipt-backend.md).

Các file cần đọc thêm: `AuthenticationRegistration.cs`, `AuthController.cs`, `AuthService.cs`, `TokenService.cs`, `JwtSessionEvents.cs`, `frontend/src/App.tsx`, `frontend/src/api/http.ts` và `features/auth/`.

## Schema, cấu hình và phục vụ bản build

Migration `20261105160638_AddAuthenticationSessions` chỉ thêm `AuthSession` và index phục vụ kiểm tra phiên; giữ các migration Receipt trước đó. Đã áp migration vào database Development riêng để thử. Không chạy thay đổi vào database làm việc của nhóm trong đợt này.

Migration tiếp theo `20261105165917_AddInboundSupplierManifest` thêm cờ và bảng serial dự kiến cùng khóa/index. Script chạy local áp migration vào database Development riêng, giữ Receipt và mật khẩu hiện có. Xem [chi tiết migration](database-migrations.md).

Development tự tạo khóa JWT tại `.local/jwt-signing-key` bên dưới content root của API và dùng lại sau restart. Ngoài Development phải cung cấp `Authentication__SigningKey` ít nhất 32 byte từ cấu hình bí mật; không đưa khóa vào Git. Cookie ngoài Development có Secure, nên cần HTTPS. Không bật seeder Development trên môi trường dùng chung.

`npm.cmd run build` tạo `frontend/dist`. Khi chuẩn bị host chung ASP.NET, đặt nội dung bản build vào `EWMS/EWMS.Api/wwwroot` và bảo đảm nó được đưa vào bản publish. Program chỉ bật static files/SPA fallback khi có `wwwroot/index.html`; URL React được fallback để refresh/back hoạt động, `/api/...` không tồn tại vẫn trả JSON 404. Vite dev server đã hỗ trợ URL thật trong lúc phát triển.

Đây là khả năng phục vụ source đã chuẩn bị, chưa thực hiện publish/deploy hoặc kiểm chứng cấu hình hạ tầng production. Chưa triển khai các CRUD Auth/Admin, Stock, Outbound và phân hệ khác ngoài phạm vi trên.

## Kết quả kiểm chứng

- Backend build: 0 warning/error; **80/80 test SQL/HTTP qua**, không skip. Log local: `EWMS/EWMS.Api.Tests/obj/manifest-full-result.log`.
- Test dùng database `EWMS_GoodsReceiptTests_<guid>` riêng: 61 ca Auth/Receipt cũ và 19 ca bảng kê/đối chiếu, gồm mã sai không đổi số lượng/Version, nhận nhiều đợt, từ chối/nhận lại và quyền API.
- React TypeScript/Vite build thành công.
- React ESLint qua, không lỗi. Script start/restart/stop đã chạy thử thành công.
- Browser giai đoạn đăng nhập đã kiểm tra chu trình nhận hai đợt và phân quyền; báo cáo lịch sử ở `plans/20261105-role-based-web/reports/`. Browser bảng kê/đối chiếu lần này đang được ghi riêng ở kế hoạch bổ sung, không dùng báo cáo cũ thay thế.

Báo cáo 44 browser checks và 54 tests ở kế hoạch ngày 04/11 là kết quả lịch sử trước khi chuyển sang đăng nhập thật; không dùng chúng thay cho kiểm thử phân quyền hiện tại.

[Báo cáo giai đoạn đăng nhập](../plans/20261105-role-based-web/reports/verification-20261105.md) ghi kết quả trước khi có bảng kê. Kiểm chứng bảng kê được theo dõi ở [kế hoạch hiện tại](../plans/20261105-supplier-serial-reconciliation/plan.md).
