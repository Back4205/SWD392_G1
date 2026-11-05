import { ApiError } from "../../api/http";

export const inboundStatuses: Record<string, string> = {
  Pending: "Chờ xử lý", Approved: "Đã duyệt", PartiallyReceived: "Đã nhận một phần", Completed: "Đã nhận đủ", Rejected: "Từ chối",
};
export function inboundDate(value: string | null) {
  if (!value) return "Chưa cập nhật";
  const utc = /(?:Z|[+-]\d{2}:\d{2})$/.test(value) ? value : `${value}Z`;
  const date = new Date(utc);
  return Number.isNaN(date.getTime()) ? "—" : date.toLocaleDateString("vi-VN");
}
export function inboundError(error: unknown) {
  if (!(error instanceof Error)) return "Không hoàn tất được yêu cầu. Hãy thử lại.";
  const codes: Record<string, string> = {
    IN_MANIFEST_REQUIRED: "Cần nhà cung cấp xác nhận và bảng kê serial đầy đủ trước khi duyệt yêu cầu.",
    IN_MANIFEST_ITEMS: "Bảng kê phải có đủ và đúng các dòng sản phẩm của yêu cầu. Hãy tải lại thông tin.",
    IN_MANIFEST_COUNT: "Số lượng serial của từng SKU phải bằng số lượng dự kiến.",
    IN_MANIFEST_SKU: "SKU trong bảng kê không khớp sản phẩm đã đặt. Hãy tải lại yêu cầu.",
    IN_MANIFEST_DUPLICATE: "Có serial trùng trong bảng kê. Mỗi thiết bị chỉ được khai báo một lần.",
    IN_MANIFEST_SERIAL_IN_USE: "Có serial đã nhập kho hoặc đã được khai báo trong yêu cầu nhập khác đang hiệu lực.",
    IN_SUPPLIER_INACTIVE: "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động. Hãy chọn lại.",
    GR_SERIAL_FORMAT: "Có serial không đúng định dạng. Kiểm tra lại từng dòng mã thiết bị.",
  };
  if (error instanceof ApiError && codes[error.code]) return codes[error.code];
  const messages: Record<string, string> = {
    "Warehouse does not exist.": "Kho nhận hàng không tồn tại hoặc không còn được sử dụng.",
    "At least one product is required.": "Yêu cầu phải có ít nhất một sản phẩm.",
    "Duplicate ProductVariant is not allowed.": "Mỗi sản phẩm chỉ được xuất hiện một lần trong yêu cầu.",
    "Expected quantity must be greater than zero.": "Số lượng dự kiến phải lớn hơn 0.",
    "Only pending inbound requests can be updated.": "Yêu cầu không còn chờ xử lý. Hãy làm mới để xem trạng thái hiện tại.",
    "Only pending inbound requests can be reviewed.": "Yêu cầu đã được xử lý. Hãy làm mới để xem quyết định hiện tại.",
    "Expected delivery date is required when confirmed.": "Nhập ngày giao dự kiến khi nhà cung cấp xác nhận cung cấp.",
    "Expected delivery date cannot be in the past.": "Ngày giao dự kiến không được ở quá khứ.",
    "Rejection reason is required.": "Nhập lý do từ chối yêu cầu.",
  };
  return messages[error.message] || error.message;
}
