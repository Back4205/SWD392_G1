import type {
  BinAssignment, GoodsReceipt, ReceivableInbound, ReceiptBin,
  ReceiptList, ReceiptSerial, ReceiptSession,
} from "./goodsReceipt.types";

// Chỉ cho chọn actor giả lập khi chạy Vite Development và người dùng chủ động bật.
export const demoEnabled = import.meta.env.DEV && import.meta.env.VITE_RECEIVING_DEMO_ENABLED === "true";
export const demoStaffId = Number(import.meta.env.VITE_RECEIVING_STAFF_ID || 2);
export const demoManagerId = Number(import.meta.env.VITE_RECEIVING_MANAGER_ID || 1);

export function getDemoUserId(): number {
  return Number(localStorage.getItem("ewms.receiving.demoUserId")) || demoStaffId;
}

export function setDemoUserId(userId: number) {
  if (demoEnabled) localStorage.setItem("ewms.receiving.demoUserId", String(userId));
}

export class ReceiptApiError extends Error {
  status: number;
  code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

// Một hàm fetch chung; từng API bên dưới vẫn viết rõ URL và payload.
async function requestApi<T>(path: string, method = "GET", body?: object): Promise<T> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (body) headers["Content-Type"] = "application/json";
  if (demoEnabled) headers["X-Development-UserId"] = String(getDemoUserId());

  let response: Response;
  try {
    response = await fetch(`/api/goods-receipts${path}`, {
      method, headers, credentials: "include",
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ReceiptApiError(0, "NETWORK", "Không kết nối được máy chủ. Kiểm tra API rồi thử lại.");
  }

  const data = await response.json().catch(() => null);
  if (!response.ok) {
    if (!data && response.status >= 500) {
      throw new ReceiptApiError(response.status, "NETWORK", "Không kết nối được API. Kiểm tra máy chủ rồi thử lại.");
    }
    const validationErrors = data?.errors as Record<string, string[]> | undefined;
    const message = data?.message || (validationErrors ? Object.values(validationErrors).flat().join(" ") : data?.title);
    throw new ReceiptApiError(response.status, data?.code || "HTTP_ERROR", message || `Yêu cầu thất bại (${response.status}).`);
  }
  return data as T;
}

export function getReceiptSession() {
  return requestApi<ReceiptSession>("/session");
}
export function getGoodsReceipts(query: string) {
  const params = new URLSearchParams(query);
  // ReceivedDate lưu UTC. Chuyển ngày trên màn hình sang UTC trước khi lọc server.
  for (const field of ["fromDate", "toDate"]) {
    const value = params.get(field);
    if (value && /^\d{4}-\d{2}-\d{2}$/.test(value)) {
      const time = field === "fromDate" ? "T00:00:00" : "T23:59:59.999";
      const date = new Date(`${value}${time}`);
      if (!Number.isNaN(date.getTime())) params.set(field, date.toISOString());
    }
  }
  return requestApi<ReceiptList>(params.size ? `?${params}` : "");
}
export function getGoodsReceipt(id: number) {
  return requestApi<GoodsReceipt>(`/${id}`);
}
export function getReceivableInbounds() {
  return requestApi<ReceivableInbound[]>("/lookups/inbounds");
}
export function getReceiptBins(warehouseId: number) {
  return requestApi<ReceiptBin[]>(`/lookups/bins?warehouseId=${warehouseId}`);
}
export function createGoodsReceipt(inboundRequestId: number) {
  return requestApi<GoodsReceipt>("", "POST", { inboundRequestId });
}
export function saveGoodsReceipt(id: number, expectedVersion: string, items: BinAssignment[]) {
  return requestApi<GoodsReceipt>(`/${id}`, "PUT", { expectedVersion, items });
}
export function addReceiptSerial(id: number, itemId: number, expectedVersion: string, serialCode: string, sku: string) {
  return requestApi<ReceiptSerial>(`/${id}/items/${itemId}/serials`, "POST", { expectedVersion, serialCode, sku });
}
export function submitGoodsReceipt(id: number, expectedVersion: string) {
  return requestApi<GoodsReceipt>(`/${id}/submit`, "POST", { expectedVersion });
}
export function approveGoodsReceipt(id: number, expectedVersion: string) {
  return requestApi<GoodsReceipt>(`/${id}/approve`, "POST", { expectedVersion });
}
export function rejectGoodsReceipt(id: number, expectedVersion: string, reason: string) {
  return requestApi<GoodsReceipt>(`/${id}/reject`, "POST", { expectedVersion, reason });
}
