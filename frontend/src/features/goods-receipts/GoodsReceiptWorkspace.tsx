import { useEffect, useState } from "react";
import { demoEnabled, demoManagerId, demoStaffId, getDemoUserId, getReceiptSession, setDemoUserId } from "./goodsReceipt.api";
import type { ReceiptSession } from "./goodsReceipt.types";
import { receiptErrorMessage } from "./receipt.helpers";
import { navigateTo } from "../../hooks/useHashRoute";
import GoodsReceiptListPage from "./GoodsReceiptListPage";
import CreateGoodsReceiptPage from "./CreateGoodsReceiptPage";
import GoodsReceiptDetailPage from "./GoodsReceiptDetailPage";
import "./goodsReceipt.css";

interface Props { route: string; busy: boolean; onBusyChange: (busy: boolean) => void; }

export default function GoodsReceiptWorkspace({ route, busy, onBusyChange }: Props) {
  const [demoUser, setDemoUser] = useState(getDemoUserId);
  const [session, setSession] = useState<ReceiptSession | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    async function loadSession() {
      setLoading(true);
      setError("");
      setSession(null);
      try {
        const user = await getReceiptSession();
        if (active) setSession(user);
      } catch (err) {
        if (active) setError(receiptErrorMessage(err));
      } finally {
        if (active) setLoading(false);
      }
    }
    void loadSession();
    return () => { active = false; };
  }, [demoUser, reloadKey]);

  const [path, query = ""] = route.split("?");
  const receiptId = Number(path.split("/")[2]);

  // Chọn màn hình bằng if/return để dễ đọc hơn nhiều toán tử điều kiện lồng nhau.
  function renderPage() {
    if (loading) {
      return <section className="gr-panel gr-empty" role="status">Đang kiểm tra tài khoản và quyền kho…</section>;
    }
    if (error || !session) {
      return <section className="gr-panel gr-empty"><h1>Chưa mở được khu vực nhận hàng</h1><p role="alert">{error}</p><button className="gr-button gr-button--secondary" onClick={() => setReloadKey(value => value + 1)}>Thử kết nối lại</button></section>;
    }
    if (path === "/goods-receipts/new") {
      if (session.role === "Warehouse Staff") {
        return <CreateGoodsReceiptPage key={session.userId} busy={busy} onBusyChange={onBusyChange} />;
      }
      return <section className="gr-panel gr-empty"><h1>Chỉ nhân viên kho tạo phiếu</h1><p>Manager xem và quyết định các phiếu đã gửi duyệt.</p><button className="gr-button gr-button--secondary" onClick={() => navigateTo("/goods-receipts?status=PendingApproval")}>Xem phiếu chờ duyệt</button></section>;
    }
    if (path.startsWith("/goods-receipts/") && Number.isInteger(receiptId) && receiptId > 0) {
      return <GoodsReceiptDetailPage key={`${receiptId}-${session.userId}`} receiptId={receiptId} session={session} busy={busy} onBusyChange={onBusyChange} />;
    }
    if (path === "/goods-receipts" || path === "/goods-receipts/") {
      return <GoodsReceiptListPage key={`${query}-${session.userId}`} session={session} query={query} />;
    }
    return <section className="gr-panel gr-empty"><h1>Không tìm thấy trang</h1><button className="gr-button gr-button--secondary" onClick={() => navigateTo("/goods-receipts")}>Về danh sách phiếu</button></section>;
  }

  return <div className="gr-workspace">
    <div className="gr-workspace-topbar"><span>VẬN HÀNH KHO <span className="gr-topbar-divider">/</span> Nhận hàng</span>
      <div className="gr-user-area">
        {demoEnabled && <label className="gr-demo-selector"><span>Thực hành với</span><select aria-label="Tài khoản thực hành" disabled={busy} value={demoUser} onChange={event => {
          const id = Number(event.target.value);
          setDemoUserId(id);
          setDemoUser(id);
          // Manager vào list mặc định xem phiếu chờ duyệt; detail đang mở được giữ nguyên.
          if (path === "/goods-receipts" && id === demoManagerId) navigateTo("/goods-receipts?status=PendingApproval");
        }}><option value={demoStaffId}>Nhân viên nhận hàng</option><option value={demoManagerId}>Manager duyệt phiếu</option></select></label>}
        {session && <div className="gr-user-label"><span className="gr-avatar">{session.role === "Manager" ? "M" : "N"}</span><span><strong>{session.fullName}</strong><small>{session.role === "Manager" ? "Manager" : "Nhân viên kho"}</small></span></div>}
      </div>
    </div>
    <main id="main-content" tabIndex={-1} className="gr-content">
      {renderPage()}
    </main>
  </div>;
}
