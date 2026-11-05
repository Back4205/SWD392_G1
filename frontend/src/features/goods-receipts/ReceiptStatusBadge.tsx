import type { ReceiptStatus } from "./goodsReceipt.types";
import { statusLabels } from "./receipt.helpers";

export default function ReceiptStatusBadge({ status }: { status: ReceiptStatus }) {
  return <span className={`gr-status gr-status--${status}`}>{statusLabels[status] || status}</span>;
}
