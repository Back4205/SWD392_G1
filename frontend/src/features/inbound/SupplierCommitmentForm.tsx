import { useRef, useState, type FormEvent } from "react";
import { updateSupplierCommitment, type InboundRequestDetailItem, type InboundLookups } from "./inbound.api";
import { inboundError } from "./inbound.helpers";

interface Props {
  requestId: number; revision: string;
  suppliers: InboundLookups["suppliers"];
  products: InboundLookups["productVariants"];
  items: InboundRequestDetailItem[];
  initialSupplierId: number | null; initialStatus: string | null; initialDeliveryDate: string | null;
  onUpdated: () => void;
}

export default function SupplierCommitmentForm({ requestId, revision, suppliers, products, items, initialSupplierId, initialStatus, initialDeliveryDate, onUpdated }: Props) {
  const [supplierId, setSupplierId] = useState(initialSupplierId?.toString() || "");
  const [status, setStatus] = useState<"Accepted" | "Rejected">(initialStatus === "Rejected" ? "Rejected" : "Accepted");
  const [deliveryDate, setDeliveryDate] = useState(initialDeliveryDate?.slice(0, 10) || "");
  // SD04: Purchasing cập nhật sản phẩm và số lượng mà nhà cung cấp xác nhận.
  const [confirmedItems, setConfirmedItems] = useState(() => items.map(item => ({ itemId: item.id, productVariantId: item.productVariantId, expectedQty: item.expectedQty })));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const submitting = useRef(false);
  const today = new Date();
  const minDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;

  function updateItem(itemId: number, field: "productVariantId" | "expectedQty", value: number) {
    setConfirmedItems(current => current.map(item => item.itemId === itemId ? { ...item, [field]: value } : item));
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting.current) return;
    setError("");
    if (!suppliers.some(x => x.id === Number(supplierId))) { setError("Chọn nhà cung cấp đang hoạt động trong danh sách."); return; }
    if (status === "Accepted") {
      if (!deliveryDate || deliveryDate < minDate) { setError("Chọn ngày giao dự kiến từ hôm nay trở đi."); return; }
      if (!confirmedItems.length || confirmedItems.some(item => !products.some(product => product.id === item.productVariantId) || !Number.isInteger(item.expectedQty) || item.expectedQty < 1 || item.expectedQty > 2147483647)) {
        setError("Chọn SKU đang hoạt động và số lượng nguyên lớn hơn 0 cho từng dòng."); return;
      }
      if (new Set(confirmedItems.map(item => item.productVariantId)).size !== confirmedItems.length) { setError("Mỗi SKU chỉ được xuất hiện một lần trong yêu cầu."); return; }
    }
    submitting.current = true; setSaving(true);
    try {
      await updateSupplierCommitment(requestId, {
        supplierId: Number(supplierId), commitmentStatus: status,
        expectedDeliveryDate: status === "Accepted" ? `${deliveryDate}T00:00:00` : null,
        items: status === "Accepted" ? confirmedItems : [], expectedRevision: revision,
      });
      onUpdated();
    } catch (err) { setError(inboundError(err)); }
    finally { submitting.current = false; setSaving(false); }
  }

  return <section className="gr-panel ib-section">
    <div className="ib-section-heading"><div><p className="gr-eyebrow">BỘ PHẬN MUA HÀNG</p><h2>Cam kết cung cấp</h2><p className="ib-muted">Cập nhật phản hồi, ngày giao và các mặt hàng được nhà cung cấp xác nhận.</p></div></div>
    <form onSubmit={event => void save(event)}><fieldset disabled={saving} className="ib-fieldset">
      <div className="ib-form-grid">
        <label className="ib-field">Nhà cung cấp<select required value={supplierId} onChange={event => setSupplierId(event.target.value)}><option value="">Chọn nhà cung cấp…</option>{suppliers.map(supplier => <option key={supplier.id} value={supplier.id}>{supplier.supplierName}</option>)}</select></label>
        <label className="ib-field">Phản hồi<select value={status} onChange={event => setStatus(event.target.value as "Accepted" | "Rejected")}><option value="Accepted">Chấp nhận cung cấp</option><option value="Rejected">Từ chối cung cấp</option></select></label>
        {status === "Accepted" && <label className="ib-field">Ngày giao dự kiến<input required type="date" min={minDate} value={deliveryDate} onChange={event => setDeliveryDate(event.target.value)} /></label>}
      </div>
      {status === "Accepted" ? <>
        <h3>Mặt hàng nhà cung cấp xác nhận</h3><p className="ib-muted">Chọn SKU và số lượng xác nhận cho từng dòng của yêu cầu. Nhân viên kho ghi nhận serial khi nhận hàng thực tế.</p>
        <div className="gr-table-scroll"><table className="gr-table ib-edit-table"><caption className="sr-only">Sản phẩm và số lượng nhà cung cấp xác nhận</caption><thead><tr><th>Sản phẩm / SKU</th><th>Số lượng xác nhận</th></tr></thead><tbody>{confirmedItems.map((item, index) => <tr key={item.itemId}>
          <td><select required aria-label={`SKU xác nhận dòng ${index + 1}`} value={item.productVariantId} onChange={event => updateItem(item.itemId, "productVariantId", Number(event.target.value))}><option value="">Chọn sản phẩm…</option>{!products.some(product => product.id === item.productVariantId) && <option value={item.productVariantId} disabled>{items[index].sku} · Không còn hoạt động</option>}{products.map(product => <option key={product.id} value={product.id} disabled={confirmedItems.some(other => other.itemId !== item.itemId && other.productVariantId === product.id)}>{product.sku} · {product.productName}</option>)}</select></td>
          <td><input required aria-label={`Số lượng xác nhận dòng ${index + 1}`} type="number" min={1} max={2147483647} step={1} value={item.expectedQty || ""} onChange={event => updateItem(item.itemId, "expectedQty", Number(event.target.value))} /></td>
        </tr>)}</tbody></table></div>
      </> : <div className="gr-alert gr-alert--info">Khi lưu phản hồi từ chối, yêu cầu sẽ chuyển sang Đã hủy và không thể dùng để nhận hàng.</div>}
      {error && <div className="gr-alert gr-alert--error" role="alert">{error}</div>}
      {!suppliers.length && <p className="ib-muted">Chưa có nhà cung cấp đang hoạt động. Liên hệ bộ phận quản lý danh mục.</p>}
      <div className="ib-actions"><button className="gr-button gr-button--primary" type="submit" disabled={!suppliers.length}>{saving ? "Đang lưu…" : "Lưu cam kết"}</button></div>
    </fieldset></form>
  </section>;
}
