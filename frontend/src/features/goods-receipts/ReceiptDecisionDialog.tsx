import { useEffect, useRef, useState } from "react";
import type { GoodsReceipt } from "./goodsReceipt.types";

interface Props {
  decision: "approve" | "reject";
  receipt: GoodsReceipt;
  busy: boolean;
  onCancel: () => void;
  onConfirm: (note: string) => Promise<void>;
}

export default function ReceiptDecisionDialog({ decision, receipt, busy, onCancel, onConfirm }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [note, setNote] = useState("");
  const [validation, setValidation] = useState("");
  const approving = decision === "approve";
  const quantity = receipt.items.reduce((total, item) => total + item.receivedQty, 0);

  // Native dialog tự giữ focus trong hộp thoại, hỗ trợ Escape và trả focus khi đóng.
  useEffect(() => {
    const dialog = dialogRef.current;
    dialog?.showModal();
    return () => { dialog?.close(); };
  }, []);

  return <dialog ref={dialogRef} className="gr-dialog" aria-labelledby="gr-decision-title"
    onCancel={event => { event.preventDefault(); if (!busy) onCancel(); }}>
    <form onSubmit={event => {
      event.preventDefault();
      if (!approving && (note.trim().length === 0 || note.trim().length > 255)) {
        setValidation("Nhập lý do từ chối từ 1 đến 255 ký tự.");
        return;
      }
      setValidation("");
      void onConfirm(note.trim());
    }}>
      <p className="gr-eyebrow">QUYẾT ĐỊNH CỦA MANAGER</p>
      <h2 id="gr-decision-title">{approving ? "Duyệt phiếu nhận hàng?" : "Từ chối phiếu nhận hàng?"}</h2>
      <p>Phiếu <strong>GR-{String(receipt.id).padStart(4, "0")}</strong> · {quantity} thiết bị · Kho #{receipt.warehouseId}</p>
      {approving && <div className="gr-alert gr-alert--info">Sau khi duyệt, hệ thống đăng ký serial, cộng tồn vào các bin đã chọn và cập nhật tiến độ Inbound.</div>}
      {!approving && <label>Lý do từ chối<textarea autoFocus required maxLength={255} rows={4} value={note} disabled={busy}
        onChange={event => setNote(event.target.value)} placeholder="Mô tả vấn đề cần nhân viên kiểm tra…" />
        <span className="gr-field-hint">{note.length}/255 ký tự · Bắt buộc nhập và được lưu cùng phiếu.</span></label>}
      {validation && <p className="gr-inline-error" role="alert">{validation}</p>}
      <div className="gr-dialog-actions"><button className="gr-button gr-button--quiet" type="button" disabled={busy} onClick={onCancel}>Quay lại kiểm tra</button>
        <button className={`gr-button ${approving ? "gr-button--primary" : "gr-button--danger"}`} type="submit" disabled={busy || (!approving && (!note.trim() || note.trim().length > 255))}>
          {busy ? "Đang xử lý…" : approving ? "Xác nhận duyệt" : "Xác nhận từ chối"}
        </button></div>
    </form>
  </dialog>;
}
