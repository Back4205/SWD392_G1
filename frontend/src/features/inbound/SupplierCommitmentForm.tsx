import { useRef, useState, type FormEvent } from "react";
import { updateSupplierCommitment, type InboundRequestDetailItem } from "./inbound.api";
import { inboundError } from "./inbound.helpers";
import { manifestInputErrors, parseSerialLines } from "./supplierManifest.helpers";

interface Props {
  requestId: number;
  suppliers: { id: number; supplierName: string }[];
  items: InboundRequestDetailItem[];
  initialSupplierId: number | null; initialStatus: string | null; initialDeliveryDate: string | null;
  onUpdated: () => void;
}

export default function SupplierCommitmentForm({ requestId, suppliers, items, initialSupplierId, initialStatus, initialDeliveryDate, onUpdated }: Props) {
  const [supplierId, setSupplierId] = useState(initialSupplierId?.toString() || "");
  const [status, setStatus] = useState<"Confirmed" | "Rejected">(initialStatus === "Rejected" ? "Rejected" : "Confirmed");
  const [deliveryDate, setDeliveryDate] = useState(initialDeliveryDate?.slice(0, 10) || "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [showValidation, setShowValidation] = useState(false);
  const [serialTexts, setSerialTexts] = useState<Record<number, string>>(() => {
    const values: Record<number, string> = {};
    for (const item of items) values[item.id] = (item.expectedSerials || []).join("\n");
    return values;
  });
  const submitting = useRef(false);
  const today = new Date();
  const minDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
  const itemErrors = manifestInputErrors(items, serialTexts);
  const totalSerials = items.reduce((total, item) => total + parseSerialLines(serialTexts[item.id] || "").length, 0);
  const totalExpected = items.reduce((total, item) => total + item.expectedQty, 0);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting.current) return;
    setError("");
    if (!suppliers.some(x => x.id === Number(supplierId))) { setError("Chọn nhà cung cấp đang hoạt động trong danh sách."); return; }
    if (status === "Confirmed" && (!deliveryDate || deliveryDate < minDate)) { setError("Chọn ngày giao dự kiến từ hôm nay trở đi."); return; }
    if (status === "Confirmed") {
      setShowValidation(true);
      if (items.length === 0 || Object.values(itemErrors).some(errors => errors.length > 0)) {
        setError("Kiểm tra bảng kê bên dưới: mỗi SKU cần đủ serial hợp lệ và không trùng nhau."); return;
      }
    }
    submitting.current = true; setSaving(true);
    try {
      await updateSupplierCommitment(requestId, {
        supplierId: Number(supplierId), commitmentStatus: status,
        expectedDeliveryDate: status === "Confirmed" ? `${deliveryDate}T00:00:00` : null,
        // Rejected không gửi bảng kê cũ; server sẽ xóa khai báo của yêu cầu Pending.
        items: status === "Confirmed" ? items.map(item => ({ itemId: item.id, sku: item.sku, serialCodes: parseSerialLines(serialTexts[item.id] || "") })) : [],
      });
      onUpdated();
    } catch (err) { setError(inboundError(err)); }
    finally { submitting.current = false; setSaving(false); }
  }

  return <section className="gr-panel ib-section">
    <div className="ib-section-heading"><div><p className="gr-eyebrow">BỘ PHẬN MUA HÀNG</p><h2>Cam kết & bảng kê hàng giao</h2><p className="ib-muted">Nhập thông tin nhà cung cấp xác nhận để nhân viên kho đối chiếu từng thiết bị khi nhận.</p></div></div>
    <form onSubmit={event => void save(event)}><fieldset disabled={saving} className="ib-fieldset">
      <div className="ib-form-grid">
        <label className="ib-field">Nhà cung cấp<select required value={supplierId} onChange={event => setSupplierId(event.target.value)}><option value="">Chọn nhà cung cấp…</option>{suppliers.map(supplier => <option key={supplier.id} value={supplier.id}>{supplier.supplierName}</option>)}</select></label>
        <label className="ib-field">Phản hồi<select value={status} onChange={event => setStatus(event.target.value as "Confirmed" | "Rejected")}><option value="Confirmed">Xác nhận cung cấp</option><option value="Rejected">Từ chối cung cấp</option></select></label>
        {status === "Confirmed" && <label className="ib-field">Ngày giao dự kiến<input required type="date" min={minDate} value={deliveryDate} onChange={event => setDeliveryDate(event.target.value)} /></label>}
      </div>
      {status === "Confirmed" ? <div className="ib-manifest-editor">
        <div className="ib-section-heading"><div><h3>SKU và Serial / IMEI dự kiến</h3><p className="ib-muted">SKU và số lượng lấy từ kế hoạch đã lập. Nhập hoặc dán mỗi serial trên một dòng; cần đủ mã cho tất cả sản phẩm.</p></div><span className={`ib-manifest-count${totalSerials === totalExpected ? " complete" : ""}`}>{totalSerials} / {totalExpected} mã</span></div>
        <div className="gr-alert gr-alert--info">Đây là bảng kê do nhà cung cấp xác nhận, chưa phải hàng đã nhận. Staff vẫn phải quét thiết bị thực tế; tồn kho chỉ tăng sau khi Manager duyệt phiếu nhận.</div>
        {items.map(item => {
          const count = parseSerialLines(serialTexts[item.id] || "").length;
          const errors = itemErrors[item.id];
          const showErrors = showValidation || Boolean(serialTexts[item.id]?.trim());
          const errorId = `ib-serial-errors-${item.id}`;
          return <div className="ib-manifest-item" key={item.id}>
            <div className="ib-manifest-product"><div><label className="ib-field">SKU đã đặt<input readOnly value={item.sku} aria-label={`SKU dòng ${item.id}`} /></label><h4>{item.productName}</h4></div><div><span>Số lượng dự kiến</span><strong>{item.expectedQty} thiết bị</strong></div></div>
            <label className="ib-field ib-serial-input">Serial / IMEI cho {item.sku}<textarea rows={Math.min(8, Math.max(3, item.expectedQty))} required aria-describedby={errorId} aria-invalid={showErrors && errors.length > 0} value={serialTexts[item.id] || ""} autoComplete="off" autoCapitalize="off" spellCheck={false} placeholder="Mỗi serial một dòng, giữ nguyên số 0 ở đầu…" onChange={event => setSerialTexts(current => ({ ...current, [item.id]: event.target.value }))} /></label>
            <div className="ib-manifest-line-summary"><span className={count === item.expectedQty ? "ib-text-success" : "ib-text-warning"}>{count} / {item.expectedQty} serial{count < item.expectedQty ? ` · Còn thiếu ${item.expectedQty - count}` : count > item.expectedQty ? ` · Thừa ${count - item.expectedQty}` : " · Đã đủ số lượng"}</span><small>Dòng trống được bỏ qua. Không tự đổi chữ hoa/thường.</small></div>
            <div id={errorId}>{showErrors && errors.length > 0 && <ul className="ib-manifest-errors">{errors.map((message, index) => <li key={index}>{message}</li>)}</ul>}</div>
          </div>;
        })}
      </div> : <div className="gr-alert gr-alert--info">Nhà cung cấp từ chối cung cấp: không cần nhập serial. Khi lưu, bảng kê cũ của yêu cầu này sẽ được xóa.</div>}
      {error && <div className="gr-alert gr-alert--error" role="alert">{error}</div>}
      {!suppliers.length && <p className="ib-muted">Chưa có nhà cung cấp đang hoạt động. Liên hệ bộ phận quản lý danh mục.</p>}
      <div className="ib-actions"><button className="gr-button gr-button--primary" type="submit" disabled={!suppliers.length}>{saving ? "Đang lưu…" : "Lưu cam kết"}</button></div>
    </fieldset></form>
  </section>;
}
