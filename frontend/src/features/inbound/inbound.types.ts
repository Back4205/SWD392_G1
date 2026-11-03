
export interface CreateInboundRequestItem {
    productVariantId: number;
    expectedQty: number;
}

export interface CreateInboundRequestRequest {
    warehouseId: number;
    items: CreateInboundRequestItem[];
}

export interface CreateInboundRequestResponse {
    message: string;
    id: number;
    status: string;
}
