import { useEffect, useState } from "react";
import { getInboundReceivingProgress, type InboundReceivingProgress as Progress } from "./inbound.api";
import { inboundError } from "./inbound.helpers";

interface Props { requestId: number; products: { id: number; sku: string; productName: string }[]; }

export default function InboundReceivingProgress({ requestId, products }: Props) {
  const [progress, setProgress] = useState<Progress | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    async function loadProgress() {
      setLoading(true); setError("");
      try { const data = await getInboundReceivingProgress(requestId); if (active) setProgress(data); }
      catch (err) { if (active) setError(inboundError(err)); }
      finally { if (active) setLoading(false); }
    }
    void loadProgress();
    return () => { active = false; };
  }, [requestId, reload]);

  const percent = Math.max(0, Math.min(100, progress?.progressPercentage || 0));
  return <section className="gr-panel ib-section">
    <div className="ib-section-heading"><div><h2>Tiến độ nhận hàng</h2><p className="ib-muted">Chỉ tính số lượng từ phiếu nhận đã được quản lý duyệt.</p></div><button className="gr-button gr-button--quiet" disabled={loading} onClick={() => setReload(value => value + 1)}>Làm mới tiến độ</button></div>
    {loading && <p role="status">Đang tải tiến độ nhận hàng…</p>}
    {error && <div className="gr-alert gr-alert--error" role="alert">{error} <button className="gr-text-button" onClick={() => setReload(value => value + 1)}>Thử lại</button></div>}
    {!loading && !error && progress && <>
      <div className="ib-progress-heading"><strong>{progress.totalReceivedQty ?? "—"} / {progress.totalExpectedQty} thiết bị</strong><span>{progress.progressPercentage === null ? "Chưa có số liệu" : `${percent.toFixed(1)}% hoàn thành`}</span></div>
      <progress className="ib-progress" max={100} value={percent} aria-label="Tỷ lệ nhận hàng" />
      <div className="gr-table-scroll"><table className="gr-table"><caption className="sr-only">Tiến độ nhận theo sản phẩm</caption><thead><tr><th>Sản phẩm / SKU</th><th>Dự kiến</th><th>Đã nhận</th><th>Còn thiếu</th></tr></thead><tbody>{progress.items.map(item => <tr key={item.productVariantId}><td className="gr-sku">{products.find(x => x.id === item.productVariantId)?.sku || `Sản phẩm #${item.productVariantId}`}</td><td>{item.expectedQty}</td><td>{item.receivedQty ?? "—"}</td><td><strong>{item.remainingQty ?? "—"}</strong></td></tr>)}</tbody></table></div>
    </>}
  </section>;
}
