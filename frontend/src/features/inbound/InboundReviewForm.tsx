import { useRef, useState, type FormEvent } from "react";
import { reviewInboundRequest, type InboundReviewDecision } from "./inbound.api";
import { inboundError } from "./inbound.helpers";

interface Props { requestId: number; canApprove: boolean; onReviewed: () => void; }

export default function InboundReviewForm({ requestId, canApprove, onReviewed }: Props) {
  const [decision, setDecision] = useState<InboundReviewDecision | null>(null);
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const submitting = useRef(false);

  async function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!decision || submitting.current) return;
    if (decision === "Approved" && !canApprove) { setError("Purchasing cần xác nhận đủ bảng kê SKU/serial trước khi duyệt."); return; }
    if (decision === "Rejected" && !reason.trim()) { setError("Nhập lý do từ chối yêu cầu."); return; }
    submitting.current = true; setSaving(true); setError("");
    try {
      await reviewInboundRequest(requestId, { decision, rejectionReason: decision === "Rejected" ? reason.trim() : null });
      onReviewed();
    } catch (err) { setError(inboundError(err)); }
    finally { submitting.current = false; setSaving(false); }
  }

  return <section className="gr-panel ib-section">
    <div><p className="gr-eyebrow">QUẢN LÝ KHO</p><h2>Phê duyệt yêu cầu nhập</h2><p className="ib-muted">Kiểm tra sản phẩm, số lượng và thông tin cung cấp trước khi quyết định.</p></div>
    {error && <div className="gr-alert gr-alert--error" role="alert">{error}</div>}
    {!canApprove && <div className="gr-alert gr-alert--info">Chưa thể duyệt: Purchasing cần xác nhận nhà cung cấp và khai báo đủ serial cho tất cả SKU. Bạn vẫn có thể từ chối yêu cầu với lý do.</div>}
    {!decision ? <div className="ib-actions">
      <button type="button" className="gr-button gr-button--primary" disabled={!canApprove} onClick={() => { setDecision("Approved"); setError(""); }}>Duyệt yêu cầu</button>
      <button type="button" className="gr-button ib-button-danger" onClick={() => { setDecision("Rejected"); setError(""); }}>Từ chối</button>
    </div> : <form className="ib-confirm-panel" onSubmit={event => void confirm(event)}>
      <h3>{decision === "Approved" ? "Xác nhận duyệt yêu cầu" : "Xác nhận từ chối yêu cầu"}</h3>
      <p>{decision === "Approved" ? "Sau khi duyệt, nhân viên kho có thể lập phiếu nhận hàng từ yêu cầu này." : "Yêu cầu bị từ chối sẽ không được sử dụng để nhận hàng."}</p>
      {decision === "Rejected" && <label className="ib-field">Lý do từ chối<textarea autoFocus required maxLength={255} rows={3} value={reason} disabled={saving} onChange={event => setReason(event.target.value)} placeholder="Mô tả lý do để người lập yêu cầu có thể đối chiếu…" /></label>}
      <div className="ib-actions"><button type="submit" className={`gr-button ${decision === "Rejected" ? "ib-button-danger" : "gr-button--primary"}`} disabled={saving || (decision === "Rejected" && !reason.trim())}>{saving ? "Đang lưu quyết định…" : "Xác nhận"}</button><button type="button" className="gr-button gr-button--secondary" disabled={saving} onClick={() => { setDecision(null); setError(""); }}>Quay lại</button></div>
    </form>}
  </section>;
}
