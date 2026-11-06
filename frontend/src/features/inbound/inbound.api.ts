import { apiRequest } from "../../api/http";
import type { CreateInboundRequestRequest, CreateInboundRequestResponse } from "./inbound.types";
const baseUrl = "/api/inbound-requests";

export interface InboundRequestListItem {
  id: number; warehouseId: number; requestedByUserId: number; supplierId: number | null;
  status: string; createdDate: string; expectedDeliveryDate: string | null;
}
export interface InboundRequestDetail extends InboundRequestListItem {
  approvedByUserId: number | null; supplierCommitmentStatus: string | null; rejectionReason: string | null;
  approvalNote: string | null; decidedByName: string | null; decisionDate: string | null;
  revision: string;
  assignedStaffUserId: number | null;
  assignedStaffName: string | null;
  hasGoodsReceipts: boolean;
  items: InboundRequestDetailItem[];
}
export interface InboundRequestDetailItem {
  id: number; productVariantId: number; expectedQty: number;
  sku: string; productName: string;
}
export interface InboundLookups {
  warehouses: { id: number; warehouseName: string }[];
  suppliers: { id: number; supplierName: string }[];
  productVariants: { id: number; sku: string; productName: string }[];
  warehouseStaff: { userId: number; fullName: string; warehouseId: number }[];
}
export interface UpdateSupplierCommitmentPayload {
  supplierId: number; commitmentStatus: "Accepted" | "Rejected"; expectedDeliveryDate: string | null;
  items: { itemId: number; productVariantId: number; expectedQty: number }[];
  expectedRevision?: string;
}
export type InboundReviewDecision = "Approved" | "Rejected";
export interface ReviewInboundRequestPayload {
  decision: InboundReviewDecision; rejectionReason: string | null; expectedRevision?: string;
}
export interface InboundReceivingProgress {
  inboundRequestId: number; totalExpectedQty: number; totalReceivedQty: number | null;
  remainingQty: number | null; progressPercentage: number | null; progressStatus: string;
  items: { productVariantId: number; expectedQty: number; receivedQty: number | null; remainingQty: number | null }[];
}

// Cookie đăng nhập và CSRF được xử lý tại hàm API dùng chung.
export function getInboundRequests() { return apiRequest<InboundRequestListItem[]>(baseUrl); }
export function getInboundRequestById(id: number) { return apiRequest<InboundRequestDetail>(`${baseUrl}/${id}`); }
export function getInboundLookups() { return apiRequest<InboundLookups>(`${baseUrl}/lookups`); }
export function createInboundRequest(data: CreateInboundRequestRequest) {
  return apiRequest<CreateInboundRequestResponse>(baseUrl, "POST", data);
}
export function updateSupplierCommitment(requestId: number, data: UpdateSupplierCommitmentPayload) {
  return apiRequest<{ message: string; id: number }>(`${baseUrl}/${requestId}/supplier-commitment`, "PUT", data);
}
export function reviewInboundRequest(requestId: number, data: ReviewInboundRequestPayload) {
  return apiRequest<{ message: string; id: number; status: string }>(`${baseUrl}/${requestId}/review`, "PUT", data);
}
export function getInboundReceivingProgress(requestId: number) {
  return apiRequest<InboundReceivingProgress>(`${baseUrl}/${requestId}/receiving-progress`);
}
