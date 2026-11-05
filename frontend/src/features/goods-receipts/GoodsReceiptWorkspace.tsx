import type { ReceiptSession } from "./goodsReceipt.types";
import GoodsReceiptListPage from "./GoodsReceiptListPage";
import CreateGoodsReceiptPage from "./CreateGoodsReceiptPage";
import GoodsReceiptDetailPage from "./GoodsReceiptDetailPage";
import "./goodsReceipt.css";
interface Props { route: string; session: ReceiptSession; busy: boolean; onBusyChange: (busy: boolean) => void; }
export default function GoodsReceiptWorkspace({ route, session, busy, onBusyChange }: Props) {
  const [path, query = ""] = route.split("?");
  const basePath = session.role === "Manager" ? "/manager/receipts" : "/staff/receipts";
  if (path === `${basePath}/new` && session.role === "Warehouse Staff") return <CreateGoodsReceiptPage busy={busy} onBusyChange={onBusyChange} basePath={basePath} />;
  const match = path.match(/^\/(staff|manager)\/receipts\/(\d+)$/);
  if (match && Number(match[2]) > 0) return <GoodsReceiptDetailPage key={match[2]} receiptId={Number(match[2])} session={session} busy={busy} onBusyChange={onBusyChange} basePath={basePath} />;
  if (path === basePath || path === "/manager/approvals") {
    const approvals = path === "/manager/approvals";
    const params = new URLSearchParams(query);
    if (approvals) params.set("status", "PendingApproval");
    return <GoodsReceiptListPage key={route} session={session} query={params.toString()} basePath={basePath} listPath={path} approvals={approvals} />;
  }
  return <section className="gr-panel gr-empty"><h1>Không tìm thấy trang</h1><p>Đường dẫn phiếu nhận hàng không hợp lệ.</p></section>;
}
