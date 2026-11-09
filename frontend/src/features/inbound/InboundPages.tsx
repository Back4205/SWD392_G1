import type { AuthUser } from "../auth/auth.types";
import { rolePrefix } from "../auth/auth.types";
import { navigateTo } from "../../hooks/useAppRoute";
import CreateInboundRequestPage from "./CreateInboundRequestPage";
import InboundRequestListPage from "./InboundRequestListPage";
import InboundRequestDetailPage from "./InboundRequestDetailPage";
import "../goods-receipts/goodsReceipt.css";
import "./inbound.css";

interface Props { route: string; session: AuthUser; }

export default function InboundPages({ route, session }: Props) {
  const basePath = `${rolePrefix(session.role)}/inbound-requests`;
  const path = route.split("?")[0].replace(/\/$/, "");
  if (path === basePath) return <InboundRequestListPage session={session} basePath={basePath} />;
  if (path === `${basePath}/new` && session.role === "Manager") {
    return <CreateInboundRequestPage onBack={() => navigateTo(basePath)} onCreated={id => navigateTo(`${basePath}/${id}`)} />;
  }
  const id = Number(path.slice(basePath.length + 1));
  if (path.startsWith(`${basePath}/`) && Number.isInteger(id) && id > 0) {
    return <InboundRequestDetailPage key={id} requestId={id} session={session} onBack={() => navigateTo(basePath)} />;
  }
  return <section className="gr-panel gr-empty"><h1>Không tìm thấy trang</h1><p>Đường dẫn này không thuộc khu vực yêu cầu nhập của bạn.</p><button className="gr-button gr-button--secondary" onClick={() => navigateTo(basePath)}>Về danh sách</button></section>;
}
