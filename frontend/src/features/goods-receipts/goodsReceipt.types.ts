// Các interface bên dưới khớp property DTO C#; không dùng dữ liệu giả cho phiếu.
export type ReceiptStatus = "Draft" | "PendingApproval" | "Approved" | "Rejected";

export interface ReceiptSession {
  userId: number;
  fullName: string;
  role: string;
  warehouseIds: number[];
}

export interface ReceiptSerial {
  id: number;
  itemId: number;
  serialCode: string;
  scannedAt: string;
  receivedQty: number;
  version: string;
}

export interface ReceiptItem {
  id: number;
  productVariantId: number;
  sku: string;
  binId: number | null;
  receivedQty: number;
  expectedQty: number;
  approvedReceivedQty: number;
  remainingQty: number;
  serials: ReceiptSerial[];
  expectedSerials: ExpectedReceiptSerial[];
}

export interface ExpectedReceiptSerial {
  serialCode: string;
  status: "AwaitingReceipt" | "ScannedThisReceipt" | "ScannedOtherReceipt" | "Received";
  receiptId: number | null;
  scannedSerialCode: string | null;
}

export interface GoodsReceipt {
  id: number;
  inboundRequestId: number;
  warehouseId: number;
  receivedByUserId: number;
  receivedDate: string;
  status: ReceiptStatus;
  version: string;
  decidedByUserId: number | null;
  decisionDate: string | null;
  rejectionReason: string | null;
  hasSupplierManifest: boolean;
  items: ReceiptItem[];
}

export interface ReceiptSummary {
  id: number;
  inboundRequestId: number;
  warehouseId: number;
  receivedByUserId: number;
  receivedDate: string;
  status: ReceiptStatus;
  version: string;
}

export interface ReceiptList {
  totalCount: number;
  page: number;
  pageSize: number;
  items: ReceiptSummary[];
}

export interface ReceivingLine {
  productVariantId: number;
  sku: string;
  expectedQty: number;
  receivedQty: number;
  remainingQty: number;
}

export interface ReceivableInbound {
  id: number;
  warehouseId: number;
  warehouseName: string;
  status: string;
  lines: ReceivingLine[];
}

export interface ReceiptBin {
  id: number;
  binCode: string;
  binName: string;
  rackCode: string;
  maxWeightKg: number;
  availableWeightKg: number;
}

export interface BinAssignment {
  itemId: number;
  binId: number | null;
}
