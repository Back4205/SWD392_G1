export interface AuthUser {
  userId: number;
  userName: string;
  fullName: string;
  role: string;
  warehouseIds: number[];
}
export function rolePrefix(role: string) {
  if (role === "Warehouse Staff") return "/staff";
  if (role === "Manager") return "/manager";
  if (role === "Purchasing Manager") return "/purchasing";
  return "/account";
}
export function homeForRole(role: string) {
  if (role === "Warehouse Staff") return "/staff/receipts";
  if (role === "Manager") return "/manager/approvals";
  if (role === "Purchasing Manager") return "/purchasing/inbound-requests";
  return "/account/profile";
}
export function roleLabel(role: string) {
  if (role === "Warehouse Staff") return "Nhân viên kho";
  if (role === "Manager") return "Quản lý kho";
  if (role === "Purchasing Manager") return "Quản lý mua hàng";
  return role;
}
