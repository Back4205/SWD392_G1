import { useEffect, useState } from "react";
import { createGoodsReceipt, getReceivableInbounds } from "./goodsReceipt.api";
import type { ReceivableInbound } from "./goodsReceipt.types";
import { receiptErrorMessage } from "./receipt.helpers";
import { navigateTo } from "../../hooks/useHashRoute";

interface Props { busy: boolean; onBusyChange: (busy: boolean) => void; }

export default function CreateGoodsReceiptPage({ busy, onBusyChange }: Props) {
  const [inbounds, setInbounds] = useState<ReceivableInbound[]>([]);
  const [selectedId, setSelectedId] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    async function loadInbounds() {
      setLoading(true);
      setError("");
      try {
        const result = await getReceivableInbounds();
        if (active) setInbounds(result);
      } catch (err) {
        if (active) setError(receiptErrorMessage(err));
      } finally {
        if (active) setLoading(false);
      }
    }
    void loadInbounds();
    return () => { active = false; };
  }, [reloadKey]);

  const selected = inbounds.find(inbound => inbound.id === selectedId);

  async function handleCreate() {
    if (!selected || busy) return;
    onBusyChange(true);
    setError("");
    try {
      const receipt = await createGoodsReceipt(selected.id);
      navigateTo(`/goods-receipts/${receipt.id}`);
    } catch (err) {
      setError(receiptErrorMessage(err));
    } finally {
      onBusyChange(false);
    }
  }

  return <>
    <button className="gr-back-link" disabled={busy} onClick={() => navigateTo("/goods-receipts")}>← Danh sách phiếu nhận</button>
    <header className="gr-page-heading"><div><p className="gr-eyebrow">BẮT ĐẦU NHẬN HÀNG</p><h1>Tạo phiếu nhận</h1><p>Chọn yêu cầu nhập đã được duyệt và còn hàng chưa nhận.</p></div></header>

    {error && <div className="gr-alert gr-alert--error" role="alert">{error} <button className="gr-text-button" onClick={() => setReloadKey(value => value + 1)}>Tải lại danh sách</button></div>}
    <section className="gr-panel gr-create-panel">
      <div className="gr-section-label"><span>1</span><div><h2>Chọn yêu cầu nhập</h2><p>Chỉ hiển thị yêu cầu trong các kho bạn được phân công.</p></div></div>
      {loading ? <p role="status">Đang tải yêu cầu có thể nhận…</p> : !error && inbounds.length === 0 ? <div className="gr-empty"><h2>Chưa có yêu cầu để nhận hàng</h2><p>Yêu cầu cần được duyệt và còn ít nhất một sản phẩm chưa nhận đủ.</p><button className="gr-button gr-button--secondary" onClick={() => navigateTo("/inbound")}>Xem yêu cầu nhập</button></div> : <>
        <label className="gr-inbound-select">Yêu cầu nhập<select value={selectedId || ""} disabled={busy} onChange={event => setSelectedId(Number(event.target.value))}>
          <option value="" disabled>Chọn một yêu cầu nhập…</option>
          {inbounds.map(inbound => <option key={inbound.id} value={inbound.id}>Inbound #{inbound.id} · {inbound.warehouseName} · {inbound.status === "PartiallyReceived" ? "Đã nhận một phần" : "Đã duyệt"}</option>)}
        </select></label>

        {selected && <div className="gr-inbound-preview">
          <div className="gr-preview-title"><h3>Inbound #{selected.id}</h3><span>{selected.warehouseName} · Kho #{selected.warehouseId}</span></div>
          <div className="gr-table-scroll"><table className="gr-table"><caption className="sr-only">Tiến độ theo sản phẩm của yêu cầu nhập đã chọn</caption>
            <thead><tr><th>Sản phẩm / SKU</th><th>Dự kiến</th><th>Đã nhận chính thức</th><th>Còn thiếu</th></tr></thead>
            <tbody>{selected.lines.map(line => <tr key={line.productVariantId}><td className="gr-sku">{line.sku}</td><td>{line.expectedQty}</td><td>{line.receivedQty}</td><td><strong>{line.remainingQty}</strong></td></tr>)}</tbody>
          </table></div>
        </div>}
        <div className="gr-form-footer"><p>Phiếu mới ở trạng thái <strong>Nháp</strong>. Sau đó bạn quét serial và chọn bin.</p><button className="gr-button gr-button--primary" disabled={!selected || busy} onClick={() => void handleCreate()}>{busy ? "Đang tạo…" : "Tạo phiếu nháp →"}</button></div>
      </>}
    </section>
  </>;
}
