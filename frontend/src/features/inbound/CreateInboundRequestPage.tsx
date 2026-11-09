import { useEffect, useRef, useState, type FormEvent } from "react";
import { createInboundRequest, getInboundLookups, type InboundLookups } from "./inbound.api";
import type { CreateInboundRequestItem } from "./inbound.types";
import { inboundError } from "./inbound.helpers";

interface Props { onBack: () => void; onCreated: (id: number) => void; }

export default function CreateInboundRequestPage({ onBack, onCreated }: Props) {
  const [lookups, setLookups] = useState<InboundLookups | null>(null);
  const [warehouseId, setWarehouseId] = useState("");
  const [assignedStaffUserId, setAssignedStaffUserId] = useState("");
  const [items, setItems] = useState<CreateInboundRequestItem[]>([{ productVariantId: 0, expectedQty: 1 }]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);
  const submitting = useRef(false);

  useEffect(() => {
    let active = true;
    async function loadLookups() {
      setLoading(true); setError("");
      try { const data = await getInboundLookups(); if (active) setLookups(data); }
      catch (err) { if (active) setError(inboundError(err)); }
      finally { if (active) setLoading(false); }
    }
    void loadLookups();
    return () => { active = false; };
  }, [reload]);

  function updateItem(index: number, field: keyof CreateInboundRequestItem, value: number) {
    setItems(current => current.map((item, i) => i === index ? { ...item, [field]: value } : item));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting.current) return;
    setError("");
    if (!lookups?.warehouses.some(x => x.id === Number(warehouseId))) {
      setError("Chọn kho nhận hàng trong danh sách được phân công."); return;
    }
    if (!lookups.warehouseStaff.some(x => x.userId === Number(assignedStaffUserId) && x.warehouseId === Number(warehouseId))) {
      setError("Chọn nhân viên kho đang hoạt động và được phân công vào kho nhận hàng."); return;
    }
    if (items.some(x => !lookups.productVariants.some(p => p.id === x.productVariantId) || !Number.isInteger(x.expectedQty) || x.expectedQty <= 0)) {
      setError("Chọn sản phẩm và nhập số lượng nguyên lớn hơn 0 cho từng dòng."); return;
    }
    if (new Set(items.map(x => x.productVariantId)).size !== items.length) {
      setError("Mỗi sản phẩm chỉ được xuất hiện một lần trong yêu cầu."); return;
    }
    submitting.current = true; setSaving(true);
    try {
      const result = await createInboundRequest({ warehouseId: Number(warehouseId), assignedStaffUserId: Number(assignedStaffUserId), items });
      onCreated(result.id);
    } catch (err) { setError(inboundError(err)); }
    finally { submitting.current = false; setSaving(false); }
  }

  return <div className="inbound-workspace">
    <button className="gr-back-link" disabled={saving} onClick={onBack}>← Danh sách yêu cầu nhập</button>
    <header className="gr-page-heading"><div><p className="gr-eyebrow">LẬP KẾ HOẠCH NHẬP HÀNG</p><h1>Tạo yêu cầu nhập kho</h1><p>Chọn kho nhận và các sản phẩm cần nhập. Bộ phận mua hàng sẽ cập nhật cam kết nhà cung cấp.</p></div></header>
    {error && <div className="gr-alert gr-alert--error" role="alert">{error} {!lookups && <button className="gr-text-button" onClick={() => setReload(value => value + 1)}>Thử lại</button>}</div>}
    {loading ? <section className="gr-panel gr-empty" role="status">Đang tải kho và danh mục sản phẩm…</section> : lookups && <form onSubmit={event => void handleSubmit(event)}>
      <fieldset disabled={saving} className="ib-fieldset">
        <section className="gr-panel ib-section">
          <div className="gr-section-label"><span>1</span><div><h2>Kho nhận hàng</h2><p>Chỉ hiển thị những kho bạn được phân công quản lý.</p></div></div>
          <label className="ib-field">Kho nhận<select required value={warehouseId} onChange={event => { setWarehouseId(event.target.value); setAssignedStaffUserId(""); }}>
            <option value="">Chọn kho…</option>{lookups.warehouses.map(warehouse => <option key={warehouse.id} value={warehouse.id}>{warehouse.warehouseName}</option>)}
          </select></label>
          <label className="ib-field">Nhân viên kho được phân công<select required disabled={!warehouseId} value={assignedStaffUserId} onChange={event => setAssignedStaffUserId(event.target.value)}>
            <option value="">Chọn nhân viên kho…</option>{lookups.warehouseStaff.filter(staff => staff.warehouseId === Number(warehouseId)).map(staff => <option key={staff.userId} value={staff.userId}>{staff.fullName}</option>)}
          </select></label>
          {warehouseId && !lookups.warehouseStaff.some(staff => staff.warehouseId === Number(warehouseId)) && <p className="ib-muted">Kho chưa có nhân viên đang hoạt động được phân công. Liên hệ quản trị viên để cập nhật phân công kho.</p>}
          {lookups.warehouses.length === 0 && <p className="ib-muted">Tài khoản chưa được phân công kho. Liên hệ quản trị viên để được cấp quyền.</p>}
        </section>
        <section className="gr-panel ib-section">
          <div className="ib-section-heading"><div className="gr-section-label"><span>2</span><div><h2>Sản phẩm cần nhập</h2><p>Mỗi dòng gồm một sản phẩm và số lượng dự kiến.</p></div></div><button className="gr-button gr-button--secondary" type="button" disabled={items.length >= lookups.productVariants.length} onClick={() => setItems(current => [...current, { productVariantId: 0, expectedQty: 1 }])}>+ Thêm sản phẩm</button></div>
          <div className="gr-table-scroll"><table className="gr-table ib-edit-table"><caption className="sr-only">Danh sách sản phẩm cần nhập</caption><thead><tr><th>Sản phẩm / SKU</th><th>Số lượng dự kiến</th><th><span className="sr-only">Thao tác</span></th></tr></thead><tbody>{items.map((item, index) => <tr key={index}>
            <td><select required aria-label={`Sản phẩm dòng ${index + 1}`} value={item.productVariantId || ""} onChange={event => updateItem(index, "productVariantId", Number(event.target.value))}><option value="">Chọn sản phẩm…</option>{lookups.productVariants.map(product => <option key={product.id} value={product.id} disabled={items.some((x, i) => i !== index && x.productVariantId === product.id)}>{product.sku} · {product.productName}</option>)}</select></td>
            <td><input required aria-label={`Số lượng dòng ${index + 1}`} type="number" min={1} max={2147483647} step={1} value={item.expectedQty || ""} onChange={event => updateItem(index, "expectedQty", Number(event.target.value))} /></td>
            <td><button type="button" className="gr-text-button" disabled={items.length === 1} onClick={() => setItems(current => current.filter((_, i) => i !== index))}>Xóa dòng</button></td>
          </tr>)}</tbody></table></div>
          {lookups.productVariants.length === 0 && <p className="ib-muted">Chưa có sản phẩm đang hoạt động để lập yêu cầu.</p>}
          <div className="ib-totals"><span>{items.length} dòng sản phẩm</span><strong>Tổng số lượng: {items.reduce((sum, item) => sum + item.expectedQty, 0)}</strong></div>
        </section>
        <div className="gr-form-footer"><p>Yêu cầu mới sẽ ở trạng thái <strong>Chờ xử lý</strong>.</p><button type="submit" className="gr-button gr-button--primary" disabled={!lookups.warehouses.length || !lookups.productVariants.length}>{saving ? "Đang tạo yêu cầu…" : "Tạo yêu cầu nhập"}</button></div>
      </fieldset>
    </form>}
  </div>;
}
