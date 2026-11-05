import { useState } from "react";
import { login } from "./auth.api";
import type { AuthUser } from "./auth.types";
import { ApiError } from "../../api/http";
interface Props { onLogin: (user: AuthUser) => void; notice: string; }
export default function LoginPage({ onLogin, notice }: Props) {
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: React.FormEvent) {
    event.preventDefault(); if (busy) return;
    setBusy(true); setError("");
    try { onLogin(await login(userName.trim(), password)); }
    catch (err) {
      if (err instanceof ApiError && err.status === 401) setError("Tên đăng nhập hoặc mật khẩu chưa đúng.");
      else if (err instanceof ApiError && err.status === 429) setError("Bạn đã thử đăng nhập nhiều lần. Vui lòng đợi một phút rồi thử lại.");
      else setError(err instanceof Error ? err.message : "Không đăng nhập được. Hãy thử lại.");
    } finally { setBusy(false); }
  }
  return <main className="auth-layout">
    <section className="auth-intro">
      <a className="auth-wordmark" href="/login" aria-label="EWMS">EWMS<span>.</span></a>
      <div><p className="auth-eyebrow">ELECTRONIC WAREHOUSE MANAGEMENT</p><h1>Mỗi thiết bị.<br />Một hành trình rõ ràng.</h1><p>Quản lý tiếp nhận hàng, truy vết serial và phê duyệt nhập kho trong cùng một hệ thống.</p>
        <div className="auth-flow" aria-label="Quy trình nhập kho"><span>01<br /><strong>Tiếp nhận</strong></span><span>02<br /><strong>Kiểm tra</strong></span><span>03<br /><strong>Phê duyệt</strong></span></div>
      </div><small>HỆ THỐNG QUẢN LÝ KHO THIẾT BỊ ĐIỆN TỬ</small>
    </section>
    <section className="auth-form-area"><div className="auth-form-card">
      <p className="gr-eyebrow">KHÔNG GIAN LÀM VIỆC EWMS</p><h2>Đăng nhập</h2><p>Sử dụng tài khoản được cấp để truy cập công việc của bạn.</p>
      {notice && <div className="gr-alert" role="status">{notice}</div>}
      {error && <div className="gr-alert gr-alert--error" role="alert">{error}</div>}
      <form className="auth-form" onSubmit={event => void submit(event)}>
        <label>Tên đăng nhập<input autoComplete="username" autoFocus required maxLength={50} value={userName} onChange={event => setUserName(event.target.value)} disabled={busy} /></label>
        <label>Mật khẩu<div className="auth-password"><input type={showPassword ? "text" : "password"} autoComplete="current-password" required maxLength={128} value={password} onChange={event => setPassword(event.target.value)} disabled={busy} /><button type="button" aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"} onClick={() => setShowPassword(!showPassword)}>{showPassword ? "Ẩn" : "Hiện"}</button></div></label>
        <button className="gr-button gr-button--primary" disabled={busy} type="submit">{busy ? "Đang đăng nhập…" : "Đăng nhập →"}</button>
      </form><p className="auth-help">Chưa có tài khoản hoặc quên mật khẩu? Liên hệ quản trị viên hệ thống để được hỗ trợ.</p>
      <div className="auth-protection">Quyền truy cập được xác định theo tài khoản và kho được phân công.</div>
    </div></section>
  </main>;
}
