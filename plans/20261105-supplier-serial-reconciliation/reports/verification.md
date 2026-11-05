# Kiểm chứng bảng kê nhà cung cấp và đối chiếu nhận hàng

## Kết quả

| Kiểm tra | Kết quả |
|---|---|
| API/project test build | Qua, 0 warning/error |
| SQL/HTTP tests | 80/80 qua, 0 skip: 61 ca cũ và 19 ca mới |
| React lint | Qua, 0 lỗi |
| React production build | Qua, 44 modules |
| Git diff whitespace | Qua |
| Review độc lập | Không còn vấn đề chặn; migration cũ và appsettings giữ nguyên |

Log test: `EWMS/EWMS.Api.Tests/obj/manifest-full-result.log`. Các test mới trong `SupplierManifestTests.cs` bao gồm atomic rollback, đủ dòng/SKU/count, format, mã trùng/đã dùng, quyền API, Manager approval gate, chặn mã sai không đổi count/Version, dữ liệu bị sửa ngoài luồng, trạng thái nhận nhiều đợt và từ chối/nhận lại.

## Trình duyệt, API và SQL thật

Chrome chạy profile QA riêng. Dùng database `EWMS_GoodsReceiptTests_<guid>` và tài khoản test riêng, API5086/React5174; không dùng mật khẩu của người dùng hoặc header giả lập actor. Sau kiểm tra đã dừng process QA và xóa đúng database theo metadata/GUID guard.

1. Manager tạo yêu cầu hai SKU, số lượng 2 và 1. Khi chưa có bảng kê, nút duyệt bị khóa; API gọi trực tiếp cũng từ chối duyệt.
2. Purchasing thấy hai ô serial đúng hai SKU từ kế hoạch. Ô rỗng bị required chặn; bộ đếm chỉ rõ số còn thiếu. Serial trùng giữa hai SKU bị form chặn; GET detail xác nhận bảng kê chưa được lưu.
3. Lưu `000MF-A1`, `000MF-A2` cho SKU thứ nhất và `000MF-B1` cho SKU thứ hai. API trả đủ ba mã, giữ nguyên số 0 đầu; màn hình hiển thị bảng kê đã khai báo.
4. Manager duyệt yêu cầu sau khi bảng kê đầy đủ. Staff tạo phiếu và thấy ba serial dự kiến, tách khỏi dữ liệu quét thực tế.
5. Staff quét `NOT-DECLARED`: báo không có trong bảng kê; số lượng vẫn 0 và Version không thay đổi. Quét `000MF-B1` khi đang chọn SKU thứ nhất: báo sai SKU, số lượng vẫn 0.
6. Quét `000mf-a1`: được nhận diện đúng theo quy tắc không phân biệt hoa/thường của database. Bảng kê giữ `000MF-A1`; `ScannedSerialCode` và cột thực tế giữ chính xác `000mf-a1`. UI hiện “Khớp · Phiếu này”. Chọn SKU thứ hai và quét `000MF-B1` thành công.
7. Tạo phiếu thứ hai trước khi duyệt đợt đầu: hai mã đã quét hiển thị “Đang ở phiếu khác”. Thử quét lại mã đang giữ bị chặn và số lượng phiếu thứ hai vẫn 0.
8. Gửi và duyệt phiếu đầu: hai mã chuyển “Đã nhận chính thức”, Inbound chuyển PartiallyReceived, một mã còn chờ tiếp nhận.
9. Ở đợt hai, Staff thấy hai mã đã nhận trước; không thể quét lại chúng. Quét mã còn thiếu `000MF-A2`, gửi và duyệt: cả ba mã Received, Inbound Completed. Tìm kiếm serial thu hẹp bảng đối chiếu đúng một dòng.
10. Purchasing xem lại: ba mã khai báo được giữ, không còn ô chỉnh sửa sau duyệt, tiến độ nhận hiển thị 100.0%.
11. Với một yêu cầu/bảng kê khác, quét và gửi một serial rồi Manager từ chối bằng lý do tiếng Việt. Bảng đối chiếu hiện “Không tính · Phiếu từ chối”; tiến độ chính thức vẫn 0. Staff tạo phiếu mới và quét lại chính serial đó được chấp nhận.
12. Reload trang vẫn tải đúng dữ liệu. Kiểm tra desktop1440×1000 và mobile390×844: bảng kê/đối chiếu không làm tràn ngang body; bảng rộng cuộn trong vùng bảng.

Ảnh QA nằm trong `.local/manifest-*.png`, được Git bỏ qua. Không công bố số lượng “browser tests” suy ra từ số assertion; bảng trên mô tả các bước đã thực hiện. Những lần script QA sai selector/đọc UI trước khi request hoàn tất đã được sửa bằng selector đúng và chờ trạng thái UI; không thay đổi nghiệp vụ để làm assertion qua.

## Sửa lỗi phát hiện trong kiểm tra

React báo trùng key giữa form commitment và progress cùng cấp trong chi tiết Inbound. Đã thêm tiền tố `progress-` cho key progress; reviewer xác nhận các key khác nhau và không đổi luồng dữ liệu.

## Database và bản local

Migration `20261105165917_AddInboundSupplierManifest` chỉ thêm cờ/bảng kê/constraint/index. Đã áp vào database Development riêng `EWMS_GoodsReceipt_Development`; không chỉnh migration cũ, không reset phiếu hoặc mật khẩu và không sửa `appsettings.json` của nhóm.

Bản local của người dùng đã được khởi động lại trên5173/5085. Sau dọn QA, web `/login` và API `/api/auth/csrf` đều trả HTTP200. Các yêu cầu đã duyệt trước migration giữ cờ false và thông báo lịch sử chưa có bảng kê; không backfill serial giả từ kết quả nhận hàng.

Nhánh `Bacsk/goods-receipt`; chưa stage/commit/push/deploy. Các file khóa JWT, fixture QA, log, `.local`, `.env.local` không nằm trong thay đổi chờ Git.
