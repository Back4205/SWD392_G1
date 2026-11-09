import { useState } from "react";
import { changePassword } from "./auth.api";
import { displayUserName, roleLabel, type AuthUser } from "./auth.types";
interface Props { user: AuthUser; onPasswordChanged: () => void; }
export default function ProfilePage({ user, onPasswordChanged }: Props) {
  const [current, setCurrent] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: React.FormEvent) {
    event.preventDefault(); if (busy) return;
    if (password !== confirm) { setError("Mật khẩu xác nhận chưa khớp."); return; }
    setBusy(true); setError("");
    try { await changePassword(current, password, confirm); onPasswordChanged(); }
    catch (err) { setError(err instanceof Error ? err.message : "Chưa đổi được mật khẩu."); }
    finally { setBusy(false); }
  }
  return <><header className="gr-page-heading"><div><p className="gr-eyebrow">TÀI KHOẢN</p><h1>Thông tin cá nhân</h1><p>Thông tin đăng nhập và quyền được quản trị viên phân công.</p></div></header>
    <div className="profile-grid"><section className="gr-panel profile-card"><h2>Hồ sơ làm việc</h2><dl><dt>Họ và tên</dt><dd>{displayUserName(user)}</dd><dt>Tên đăng nhập</dt><dd>{user.userName}</dd><dt>Vai trò</dt><dd>{roleLabel(user.role)}</dd><dt>Kho được phân công</dt><dd>{user.warehouseIds.length ? user.warehouseIds.map(id => `Kho #${id}`).join(", ") : "Chưa được phân công kho"}</dd></dl></section>
    <section className="gr-panel profile-card"><h2>Đổi mật khẩu</h2><p>Sau khi đổi mật khẩu, tất cả phiên đăng nhập sẽ kết thúc. Bạn cần đăng nhập lại.</p>
      {error && <div className="gr-alert gr-alert--error" role="alert">{error}</div>}
      <form className="auth-form" onSubmit={event => void submit(event)}>
        <label>Mật khẩu hiện tại<input type="password" autoComplete="current-password" required maxLength={128} value={current} onChange={e => setCurrent(e.target.value)} disabled={busy} /></label>
        <label>Mật khẩu mới<input type="password" autoComplete="new-password" required minLength={6} maxLength={128} value={password} onChange={e => setPassword(e.target.value)} disabled={busy} /><small>Từ 6 đến 128 ký tự, khác mật khẩu hiện tại.</small></label>
        <label>Nhập lại mật khẩu mới<input type="password" autoComplete="new-password" required minLength={6} maxLength={128} value={confirm} onChange={e => setConfirm(e.target.value)} disabled={busy} /></label>
        <button className="gr-button gr-button--primary" disabled={busy}>{busy ? "Đang lưu…" : "Cập nhật mật khẩu"}</button>
      </form>
    </section></div></>;
}
