
import type {
    CreateInboundRequestRequest,
    CreateInboundRequestResponse,
} from "./inbound.types";

const API_BASE_URL = "";

// Task 1: Create Inbound Request
export async function createInboundRequest(
    data: CreateInboundRequestRequest
): Promise<CreateInboundRequestResponse> {
    const response = await fetch(
        `${API_BASE_URL}/api/inbound-requests`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(data),
        }
    );

    if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(
            errorData?.message ??
            errorData?.title ??
            "Failed to create inbound request."
        );
    }

    return response.json() as Promise<CreateInboundRequestResponse>;
}

// Task 2: Get Inbound Request List
export async function getInboundRequests(): Promise<
    InboundRequestListItem[]
> {
    const response = await fetch(
        `${API_BASE_URL}/api/inbound-requests`,
        {
            method: "GET",
            headers: {
                Accept: "application/json",
            },
        }
    );

    if (!response.ok) {
        throw new Error(
            `Failed to load inbound requests. Status: ${response.status}`
        );
    }

    return response.json() as Promise<InboundRequestListItem[]>;
}

export interface InboundRequestListItem {
    id: number;
    warehouseId: number;
    requestedByUserId: number;
    supplierId: number | null;
    status: string;
    createdDate: string;
    expectedDeliveryDate: string | null;
}

export interface InboundRequestDetailItem {
    id: number;
    productVariantId: number;
    expectedQty: number;
}

export interface InboundRequestDetail {
    id: number;
    warehouseId: number;
    supplierId: number | null;
    requestedByUserId: number;
    approvedByUserId: number | null;
    supplierCommitmentStatus: string | null;
    expectedDeliveryDate: string | null;
    rejectionReason: string | null;
    status: string;
    createdDate: string;
    items: InboundRequestDetailItem[];
}

// Task 3: Get Inbound Request Detail
export async function getInboundRequestById(
    id: number
): Promise<InboundRequestDetail> {
    const response = await fetch(
        `${API_BASE_URL}/api/inbound-requests/${id}`,
        {
            method: "GET",
            headers: {
                Accept: "application/json",
            },
        }
    );

    if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(
            errorData?.message ??
            `Failed to load inbound request #${id}.`
        );
    }

    return response.json() as Promise<InboundRequestDetail>;
}

export interface UpdateSupplierCommitmentPayload {
    supplierId: number;
    commitmentStatus: "Confirmed" | "Rejected";
    expectedDeliveryDate: string | null;
}

export interface UpdateSupplierCommitmentResponse {
    message: string;
    id: number;
}

// Task 4: Update Supplier Commitment
export async function updateSupplierCommitment(
    requestId: number,
    payload: UpdateSupplierCommitmentPayload
): Promise<UpdateSupplierCommitmentResponse> {
    const response = await fetch(
        `${API_BASE_URL}/api/inbound-requests/${requestId}/supplier-commitment`,
        {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                Accept: "application/json",
            },
            body: JSON.stringify(payload),
        }
    );

    if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(
            errorData?.message ??
            `Failed to update supplier commitment. HTTP ${response.status}`
        );
    }

    return response.json() as Promise<UpdateSupplierCommitmentResponse>;
}

// Task 5: Manager Approve / Reject Inbound Request

export type InboundReviewDecision = "Approved" | "Rejected";

export interface ReviewInboundRequestPayload {
    decision: InboundReviewDecision;
    rejectionReason: string | null;
}

export interface ReviewInboundRequestResponse {
    message: string;
    id: number;
    status: string;
}

export async function reviewInboundRequest(
    requestId: number,
    payload: ReviewInboundRequestPayload
): Promise<ReviewInboundRequestResponse> {
    const response = await fetch(
        `${API_BASE_URL}/api/inbound-requests/${requestId}/review`,
        {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                Accept: "application/json",
            },
            body: JSON.stringify(payload),
        }
    );

    if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(
            errorData?.message ??
            `Failed to review inbound request. HTTP ${response.status}`
        );
    }

    return (await response.json()) as ReviewInboundRequestResponse;
}

// Task 6: Receiving Progress

export interface InboundReceivingProgressItem {
    productVariantId: number;
    expectedQty: number;
    receivedQty: number | null;
    remainingQty: number | null;
}

export interface InboundReceivingProgress {
    inboundRequestId: number;
    totalExpectedQty: number;
    totalReceivedQty: number | null;
    remainingQty: number | null;
    progressPercentage: number | null;
    progressStatus: string;
    items: InboundReceivingProgressItem[];
}

export async function getInboundReceivingProgress(
    requestId: number
): Promise<InboundReceivingProgress> {
    const response = await fetch(
        `${API_BASE_URL}/api/inbound-requests/${requestId}/receiving-progress`,
        {
            method: "GET",
            headers: {
                Accept: "application/json",
            },
        }
    );

    if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        throw new Error(
            errorData?.message ??
            `Failed to load receiving progress. HTTP ${response.status}`
        );
    }

    return (await response.json()) as InboundReceivingProgress;
}