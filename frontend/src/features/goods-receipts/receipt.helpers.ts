import { ApiError } from "../../api/http";
import type { ReceiptStatus } from "./goodsReceipt.types";

export const statusLabels: Record<ReceiptStatus, string> = {
  Draft: "Nháp", PendingApproval: "Chờ duyệt", Approved: "Đã duyệt", Rejected: "Từ chối",
};

const messages: Record<string, string> = {
  MSG56: "Bạn không được phân công vào kho này.",
  MSG91: "Yêu cầu nhập chưa được duyệt hoặc không còn được nhận hàng.",
  MSG92: "Yêu cầu nhập đã nhận đủ hàng. Hãy chọn yêu cầu khác.",
  MSG93: "Cần quét ít nhất một thiết bị trước khi gửi duyệt.",
  MSG94: "Hãy chọn bin cho tất cả dòng đang nhận hàng.",
  MSG95: "Số lượng đang nhận vượt quá số còn thiếu của sản phẩm.",
  MSG96: "Vị trí không đủ tải trọng. Hãy chọn bin khác hoặc kiểm tra sức chứa.",
  MSG157: "Số lượng và số serial đã lưu chưa khớp. Hãy tải lại phiếu.",
  MSG158: "Serial này đã được quét hoặc đã có trong kho. Số lượng không thay đổi.",
  GR_SERIAL_FORMAT: "Serial không đúng định dạng. Kiểm tra mã trên thiết bị rồi quét lại.",
  GR_SKU: "Sản phẩm không khớp hoặc không còn hoạt động. Kiểm tra lại dòng đang quét.",
  GR_BIN_WAREHOUSE: "Bin phải thuộc đúng kho của phiếu nhận hàng.",
  GR_ROLE: "Tài khoản hiện tại không được thực hiện thao tác này.",
  GR_OWNER: "Chỉ nhân viên tạo phiếu mới được sửa nháp này.",
  GR_USER_INACTIVE: "Tài khoản không tồn tại hoặc đã ngừng hoạt động.",
  GR_UNAUTHENTICATED: "Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.",
  GR_NOT_FOUND: "Không tìm thấy phiếu nhận hàng này.",
  GR_STATE: "Trạng thái phiếu đã thay đổi. Hãy tải lại để xem dữ liệu mới.",
  GR_STALE_VERSION: "Phiếu vừa được thay đổi. Đã tải lại dữ liệu; hãy kiểm tra trước khi thao tác tiếp.",
  GR_CONCURRENT_WRITE: "Có thao tác đồng thời. Đã tải lại phiếu; hãy kiểm tra dữ liệu mới.",
  GR_REJECTION_REASON: "Nhập lý do từ chối từ 1 đến 255 ký tự.",
  GR_INTERNAL: "Máy chủ chưa hoàn tất thao tác. Hãy tải lại phiếu để kiểm tra trước khi thử tiếp.",
};

export function receiptErrorMessage(error: unknown) {
  if (error instanceof ApiError) return messages[error.code] || error.message;
  return error instanceof Error ? error.message : "Có lỗi xảy ra. Hãy thử lại.";
}

export function isStaleReceipt(error: unknown) {
  return error instanceof ApiError && ["GR_STALE_VERSION", "GR_CONCURRENT_WRITE", "GR_STATE"].includes(error.code);
}

export function formatReceiptDate(value: string) {
  // DateTime UTC của API không luôn có hậu tố Z; thêm Z để không lệch giờ hiển thị.
  const utcValue = /(?:Z|[+-]\d{2}:\d{2})$/.test(value) ? value : `${value}Z`;
  return new Date(utcValue).toLocaleString("vi-VN", { dateStyle: "short", timeStyle: "short" });
}
