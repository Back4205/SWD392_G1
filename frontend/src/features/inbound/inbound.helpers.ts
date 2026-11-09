import { ApiError } from "../../api/http";

export const inboundStatuses: Record<string, string> = {
  Pending: "Chờ xử lý", Approved: "Đã duyệt", PartiallyReceived: "Đã nhận một phần", Completed: "Đã nhận đủ", Rejected: "Từ chối", Canceled: "Đã hủy",
};
export function inboundDate(value: string | null) {
  if (!value) return "Chưa cập nhật";
  const utc = /(?:Z|[+-]\d{2}:\d{2})$/.test(value) ? value : `${value}Z`;
  const date = new Date(utc);
  return Number.isNaN(date.getTime()) ? "—" : date.toLocaleDateString("vi-VN");
}
export function inboundDecisionDate(value: string | null) {
  if (!value) return "Chưa lưu thời điểm";
  const utc = /(?:Z|[+-]\d{2}:\d{2})$/.test(value) ? value : `${value}Z`;
  const date = new Date(utc);
  return Number.isNaN(date.getTime()) ? "—" : date.toLocaleString("vi-VN", { dateStyle: "short", timeStyle: "short" });
}
export function inboundError(error: unknown) {
  if (!(error instanceof Error)) return "Không hoàn tất được yêu cầu. Hãy thử lại.";
  const codes: Record<string, string> = {
    IN_REJECTION_REASON: "Nhập lý do từ chối từ 1 đến 255 ký tự, không chỉ chứa khoảng trắng.",
    IN_STALE_REVISION: "Yêu cầu đã thay đổi từ khi bạn mở màn hình. Nhấn Làm mới thông tin, kiểm tra lại rồi thao tác.",
    IN_SUPPLIER_INACTIVE: "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động. Hãy chọn lại.",
    IN_SUPPLIER_NOT_ACCEPTED: "Nhà cung cấp chưa chấp nhận cung cấp. Purchasing cần cập nhật cam kết trước khi duyệt yêu cầu.",
  };
  if (error instanceof ApiError && codes[error.code]) return codes[error.code];
  const messages: Record<string, string> = {
    "Warehouse does not exist.": "Kho nhận hàng không tồn tại hoặc không còn được sử dụng.",
    "Inbound request was not found.": "Không tìm thấy yêu cầu nhập kho này.",
    "Select an active warehouse.": "Kho nhận hàng không còn hoạt động. Hãy chọn lại kho trong danh sách.",
    "Select the assigned warehouse staff.": "Chọn nhân viên kho được phân công nhận hàng.",
    "Assigned staff must be active warehouse staff assigned to this warehouse.": "Nhân viên được chọn phải đang hoạt động và được phân công vào kho nhận hàng. Hãy làm mới thông tin để chọn lại.",
    "Manager must be active and assigned to this warehouse.": "Tài khoản quản lý phải đang hoạt động và được phân công vào kho này.",
    "Select an active product variant with a valid SKU and positive weight.": "Sản phẩm phải đang hoạt động, có mã SKU và trọng lượng lớn hơn 0. Hãy làm mới để chọn lại sản phẩm hoặc liên hệ bộ phận quản lý danh mục.",
    "At least one product is required.": "Yêu cầu phải có ít nhất một sản phẩm.",
    "Duplicate ProductVariant is not allowed.": "Mỗi sản phẩm chỉ được xuất hiện một lần trong yêu cầu.",
    "Every product variant must appear exactly once.": "Mỗi sản phẩm chỉ được xuất hiện một lần trong yêu cầu.",
    "Duplicate product variants are not allowed.": "Mỗi sản phẩm chỉ được xuất hiện một lần trong yêu cầu.",
    "Confirm every requested item exactly once.": "Cần xác nhận đầy đủ mỗi dòng hàng trong yêu cầu đúng một lần. Hãy làm mới thông tin để kiểm tra lại.",
    "Confirmed item does not belong to this request or is duplicated.": "Có dòng xác nhận bị trùng hoặc không thuộc yêu cầu này. Hãy làm mới thông tin rồi thử lại.",
    "Expected quantity must be greater than zero.": "Số lượng dự kiến phải lớn hơn 0.",
    "Only pending inbound requests can be updated.": "Yêu cầu không còn chờ xử lý. Hãy làm mới để xem trạng thái hiện tại.",
    "Only pending inbound requests can be reviewed.": "Yêu cầu đã được xử lý. Hãy làm mới để xem quyết định hiện tại.",
    "Expected delivery date is required when confirmed.": "Nhập ngày giao dự kiến khi nhà cung cấp xác nhận cung cấp.",
    "Expected delivery date cannot be in the past.": "Ngày giao dự kiến không được ở quá khứ.",
    "An expected delivery date today or later is required.": "Chọn ngày giao dự kiến từ hôm nay trở đi.",
    "Expected delivery date and requested items are required.": "Yêu cầu cần có ngày giao dự kiến và danh sách mặt hàng được xác nhận trước khi duyệt.",
    "Commitment status must be Accepted or Rejected.": "Chọn phản hồi chấp nhận hoặc từ chối cung cấp của nhà cung cấp.",
    "Decision must be Approved or Rejected.": "Chọn duyệt hoặc từ chối yêu cầu nhập kho.",
    "Invalid inbound request ID.": "Mã yêu cầu nhập kho không hợp lệ.",
    "Inbound receiving quantities are inconsistent.": "Số lượng nhận hàng chưa nhất quán. Hãy làm mới thông tin; nếu lỗi vẫn còn, liên hệ quản lý kho để kiểm tra.",
    "Received quantity exceeds expected quantity for a variant.": "Số lượng đã nhận vượt quá kế hoạch của một sản phẩm. Liên hệ quản lý kho để kiểm tra dữ liệu.",
    "Rejection reason is required.": "Nhập lý do từ chối yêu cầu.",
  };
  return messages[error.message] || error.message;
}
