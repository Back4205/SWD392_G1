import type {
  BinAssignment, GoodsReceipt, ReceivableInbound, ReceiptBin,
  ReceiptList, ReceiptSerial, ReceiptSession,
} from "./goodsReceipt.types";

import { apiRequest } from "../../api/http";
export { ApiError as ReceiptApiError } from "../../api/http";
function requestApi<T>(path: string, method = "GET", body?: object) {
  return apiRequest<T>(`/api/goods-receipts${path}`, method, body);
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
