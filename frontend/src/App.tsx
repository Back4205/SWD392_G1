import { useState } from "react";
import InboundPages from "./features/inbound/InboundPages";
import GoodsReceiptWorkspace from "./features/goods-receipts/GoodsReceiptWorkspace";
import useHashRoute, { navigateTo } from "./hooks/useHashRoute";
import "./App.css";

export default function App() {
  const route = useHashRoute();
  const [busy, setBusy] = useState(false);
  const inboundOpen = route.startsWith("/inbound");

  return <div className="app-shell">
    <a className="app-skip-link" href="#main-content" onClick={event => {
      event.preventDefault();
      document.getElementById("main-content")?.focus();
    }}>Bỏ qua điều hướng</a>
    <aside className="app-sidebar" aria-label="Điều hướng hệ thống">
      <div className="app-brand"><span className="app-brand-mark" aria-hidden="true"><svg viewBox="0 0 32 32"><path d="M5 27V5m22 22V5M5 13h22M5 24h22M9 7h6v6H9zM18 18h6v6h-6zM9 18h5v6H9z" /></svg></span><div><strong>EWMS<span>.</span></strong><small>QUẢN LÝ KHO THIẾT BỊ</small></div></div>
      <p className="app-nav-heading">NGHIỆP VỤ NHẬP KHO</p>
      <nav>
        <button className={inboundOpen ? "app-nav-item active" : "app-nav-item"} aria-current={inboundOpen ? "page" : undefined} disabled={busy} onClick={() => navigateTo("/inbound")}>
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 4h14v16H5zM8 8h8M8 12h8M8 16h5" /></svg>Yêu cầu nhập kho
        </button>
        <button className={!inboundOpen ? "app-nav-item active" : "app-nav-item"} aria-current={!inboundOpen ? "page" : undefined} disabled={busy} onClick={() => navigateTo("/goods-receipts")}>
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m4 7 8-4 8 4v10l-8 4-8-4zM4 7l8 4 8-4M12 11v10m-5-6 2 2 3-3" /></svg>Phiếu nhận hàng
        </button>
      </nav>
      <div className="app-sidebar-footer"><span className="app-live-dot" /><div><strong>Không gian làm việc</strong><p>Nhận hàng · Serial · Duyệt phiếu</p></div></div>
    </aside>
    <div className="app-main">{inboundOpen ? <div id="main-content" tabIndex={-1}><InboundPages /></div> : <GoodsReceiptWorkspace route={route} busy={busy} onBusyChange={setBusy} />}</div>
  </div>;
}
