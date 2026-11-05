import { useEffect, useState } from "react";
import InboundPages from "./features/inbound/InboundPages";
import GoodsReceiptWorkspace from "./features/goods-receipts/GoodsReceiptWorkspace";
import LoginPage from "./features/auth/LoginPage";
import ProfilePage from "./features/auth/ProfilePage";
import { getCurrentUser, logout, notifyAuthChanged } from "./features/auth/auth.api";
import { homeForRole, roleLabel, rolePrefix, type AuthUser } from "./features/auth/auth.types";
import { ApiError, resetCsrfToken } from "./api/http";
import useAppRoute, { navigateTo } from "./hooks/useAppRoute";
import "./features/goods-receipts/goodsReceipt.css";
import "./App.css";

export default function App() {
  const route = useAppRoute();
  const path = route.split("?")[0];
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [connectionError, setConnectionError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    document.title = user ? `${roleLabel(user.role)} · EWMS` : "Đăng nhập · EWMS";
  }, [user]);

  useEffect(() => {
    // Mỗi lần mở web, lấy identity từ API; không khôi phục role từ localStorage.
    let active = true;
    async function loadSession() {
      setLoading(true); setConnectionError("");
      try {
        const account = await getCurrentUser();
        if (active) setUser(account);
      } catch (error) {
        if (!active) return;
        setUser(null);
        if (!(error instanceof ApiError && error.status === 401)) setConnectionError(error instanceof Error ? error.message : "Không kết nối được hệ thống.");
      } finally { if (active) setLoading(false); }
    }
    void loadSession();
    return () => { active = false; };
  }, [reload]);

  useEffect(() => {
    function expired() { setUser(null); setBusy(false); setNotice("Phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại."); }
    function changed(event: StorageEvent) {
      if (event.key === "ewms.auth.changed") { resetCsrfToken(); setReload(value => value + 1); }
    }
    window.addEventListener("ewms:session-expired", expired);
    window.addEventListener("storage", changed);
    return () => { window.removeEventListener("ewms:session-expired", expired); window.removeEventListener("storage", changed); };
  }, []);

  useEffect(() => {
    if (loading || connectionError) return;
    if (!user && path !== "/login") navigateTo(`/login?returnTo=${encodeURIComponent(route)}`, true);
    if (user && (path === "/" || path === "/login")) navigateTo(homeForRole(user.role), true);
  }, [user, loading, path, route, connectionError]);

  function loggedIn(account: AuthUser) {
    const requested = new URLSearchParams(window.location.search).get("returnTo");
    setUser(account); setNotice(""); notifyAuthChanged();
    const prefix = rolePrefix(account.role);
    navigateTo(requested?.startsWith(`${prefix}/`) ? requested : homeForRole(account.role), true);
  }
  function signedOut(message: string) {
    setUser(null); setBusy(false); setNotice(message); notifyAuthChanged(); navigateTo("/login", true);
  }
  async function signOut() {
    if (busy) return;
    setBusy(true);
    try { await logout(); signedOut("Bạn đã đăng xuất an toàn."); }
    catch (error) {
      if (error instanceof ApiError && error.status === 401) signedOut("Phiên đăng nhập đã kết thúc.");
      else setConnectionError(error instanceof Error ? error.message : "Chưa đăng xuất được.");
    } finally { setBusy(false); }
  }
  if (loading) return <main className="app-loading" role="status"><strong>EWMS.</strong><p>Đang mở không gian làm việc…</p></main>;
  if (connectionError) return <main className="app-loading"><h1>Chưa kết nối được hệ thống</h1><p role="alert">{connectionError}</p><button className="gr-button gr-button--primary" onClick={() => setReload(value => value + 1)}>Kết nối lại</button></main>;
  if (!user) return <LoginPage onLogin={loggedIn} notice={notice} />;

  const prefix = rolePrefix(user.role);
  const links: { path: string; label: string }[] = [];
  if (user.role === "Warehouse Staff") links.push({ path: "/staff/receipts", label: "Nhận hàng & Serial" }, { path: "/staff/inbound-requests", label: "Kế hoạch nhập kho" });
  if (user.role === "Manager") links.push({ path: "/manager/approvals", label: "Phiếu chờ phê duyệt" }, { path: "/manager/receipts", label: "Tra cứu phiếu nhận" }, { path: "/manager/inbound-requests", label: "Quản lý yêu cầu nhập" });
  if (user.role === "Purchasing Manager") links.push({ path: "/purchasing/inbound-requests", label: "Đơn nhập & Nhà cung cấp" });
  links.push({ path: `${prefix}/profile`, label: "Tài khoản của tôi" });

  function renderPage() {
    // Guard giao diện giúp người dùng thấy trang 403. API vẫn kiểm tra quyền riêng.
    if (!path.startsWith(`${prefix}/`)) return <section className="gr-panel gr-empty"><p className="gr-eyebrow">403 · KHÔNG CÓ QUYỀN TRUY CẬP</p><h1>Trang này thuộc vai trò khác</h1><p>Tài khoản {user!.userName} đang có quyền {roleLabel(user!.role)}.</p><button className="gr-button gr-button--primary" onClick={() => navigateTo(homeForRole(user!.role))}>Về công việc của tôi</button></section>;
    if (path === `${prefix}/profile`) return <ProfilePage user={user!} onPasswordChanged={() => signedOut("Đã đổi mật khẩu. Vui lòng đăng nhập bằng mật khẩu mới.")} />;
    if ((user!.role === "Manager" || user!.role === "Warehouse Staff") && (path.startsWith(`${prefix}/receipts`) || path === "/manager/approvals")) return <GoodsReceiptWorkspace route={route} session={user!} busy={busy} onBusyChange={setBusy} />;
    if (links.length > 1 && (path === `${prefix}/inbound-requests` || path.startsWith(`${prefix}/inbound-requests/`))) return <InboundPages route={route} session={user!} />;
    return <section className="gr-panel gr-empty"><p className="gr-eyebrow">404</p><h1>Không tìm thấy trang</h1><p>Kiểm tra đường dẫn hoặc chọn công việc trong menu.</p></section>;
  }

  return <div className="app-shell">
    <a className="app-skip-link" href="#main-content" onClick={event => { event.preventDefault(); document.getElementById("main-content")?.focus(); }}>Bỏ qua điều hướng</a>
    <aside className="app-sidebar" aria-label="Điều hướng hệ thống">
      <div className="app-brand"><span className="app-brand-mark" aria-hidden="true">E</span><div><strong>EWMS<span>.</span></strong><small>QUẢN LÝ KHO THIẾT BỊ</small></div></div>
      <p className="app-nav-heading">{roleLabel(user.role).toLocaleUpperCase("vi")}</p>
      <nav>{links.map(link => <a key={link.path} className={`app-nav-item${path === link.path || path.startsWith(`${link.path}/`) ? " active" : ""}`} href={link.path} aria-current={path === link.path ? "page" : undefined} onClick={event => {
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault(); if (!busy) navigateTo(link.path);
      }}>{link.label}</a>)}</nav>
      <div className="app-sidebar-footer"><span className="app-live-dot" /><div><strong>{user.fullName}</strong><p>{user.userName}</p><button className="gr-text-button" disabled={busy} onClick={() => void signOut()}>Đăng xuất</button></div></div>
    </aside>
    <div className="app-main"><header className="portal-topbar"><div><small>KHÔNG GIAN LÀM VIỆC</small><strong>{roleLabel(user.role)}</strong></div><div className="gr-user-label"><span className="gr-avatar">{user.fullName.slice(0, 1).toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.warehouseIds.length} kho được phân công</small></span><button className="gr-button gr-button--quiet portal-logout" disabled={busy} onClick={() => void signOut()}>Đăng xuất</button></div></header>
      <main id="main-content" tabIndex={-1} className="gr-content gr-workspace" key={user.userId}>{renderPage()}</main>
    </div>
  </div>;
}
