import { useEffect, useState } from "react";
import type { AuthUser } from "../auth/auth.types";
import { navigateTo } from "../../hooks/useAppRoute";
import { getInboundLookups, getInboundRequestById, type InboundLookups, type InboundRequestDetail } from "./inbound.api";
import { inboundDate, inboundError, inboundStatuses } from "./inbound.helpers";
import SupplierCommitmentForm from "./SupplierCommitmentForm";
import InboundReviewForm from "./InboundReviewForm";
import InboundReceivingProgress from "./InboundReceivingProgress";
import SupplierManifestPanel from "./SupplierManifestPanel";

interface Props { requestId: number; session: AuthUser; onBack: () => void; }

export default function InboundRequestDetailPage({ requestId, session, onBack }: Props) {
  const [request, setRequest] = useState<InboundRequestDetail | null>(null);
  const [lookups, setLookups] = useState<InboundLookups | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [refresh, setRefresh] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      setLoading(true); setError("");
      try {
        const [data, options] = await Promise.all([getInboundRequestById(requestId), getInboundLookups()]);
        if (active) { setRequest(data); setLookups(options); }
      } catch (err) { if (active) setError(inboundError(err)); }
      finally { if (active) setLoading(false); }
    }
    void load();
    return () => { active = false; };
  }, [requestId, refresh]);

  function updated(message: string) { setNotice(message); setRefresh(value => value + 1); }
  const canReceive = session.role === "Warehouse Staff" && request && ["Approved", "PartiallyReceived"].includes(request.status);
  const canApprove = Boolean(request?.supplierCommitmentStatus === "Confirmed" && request.hasSupplierManifest && request.items.length > 0 && request.items.every(item => item.expectedSerials?.length === item.expectedQty));

  return <div className="inbound-workspace">
    <button className="gr-back-link" onClick={onBack}>← Danh sách yêu cầu nhập</button>
    <header className="gr-page-heading"><div><p className="gr-eyebrow">CHI TIẾT YÊU CẦU NHẬP</p><h1>IN-{String(requestId).padStart(4, "0")}</h1><p>Thông tin kế hoạch nhập hàng, nhà cung cấp và số lượng đã nhận chính thức.</p></div>
      {canReceive && <button className="gr-button gr-button--primary" onClick={() => navigateTo(`/staff/receipts/new?inboundRequestId=${requestId}`)}>Tạo phiếu nhận hàng</button>}
    </header>
    {notice && <div className="gr-alert gr-alert--success" role="status">{notice}</div>}
    {error && <div className="gr-alert gr-alert--error" role="alert">{error} <button className="gr-text-button" onClick={() => setRefresh(value => value + 1)}>Thử lại</button></div>}
    {loading ? <section className="gr-panel gr-empty" role="status">Đang tải thông tin yêu cầu…</section> : !error && request && lookups && <>
      <section className="gr-panel ib-section">
        <div className="ib-section-heading"><h2>Thông tin yêu cầu</h2><span className={`ib-status ib-status--${request.status}`}>{inboundStatuses[request.status] || request.status}</span></div>
        <dl className="ib-info-grid">
          <div><dt>Kho nhận hàng</dt><dd>{lookups.warehouses.find(x => x.id === request.warehouseId)?.warehouseName || `Kho #${request.warehouseId}`}</dd></div>
          <div><dt>Ngày tạo</dt><dd>{inboundDate(request.createdDate)}</dd></div>
          <div><dt>Nhà cung cấp</dt><dd>{lookups.suppliers.find(x => x.id === request.supplierId)?.supplierName || (request.supplierId ? `Nhà cung cấp #${request.supplierId}` : "Chờ phân công")}</dd></div>
          <div><dt>Cam kết cung cấp</dt><dd>{request.supplierCommitmentStatus === "Confirmed" ? "Đã xác nhận" : request.supplierCommitmentStatus === "Rejected" ? "Nhà cung cấp từ chối" : "Chưa cập nhật"}</dd></div>
          <div><dt>Ngày giao dự kiến</dt><dd>{inboundDate(request.expectedDeliveryDate)}</dd></div>
          <div><dt>Người lập</dt><dd>Tài khoản #{request.requestedByUserId}</dd></div>
          {request.approvedByUserId && <div><dt>Người quyết định</dt><dd>Tài khoản #{request.approvedByUserId}</dd></div>}
        </dl>
        {request.rejectionReason && <div className="gr-alert gr-alert--error"><strong>Lý do từ chối:</strong> {request.rejectionReason}</div>}
      </section>
      <section className="gr-panel">
        <div className="gr-panel-heading"><h2>Danh sách sản phẩm</h2><span className="ib-muted">{request.items.length} dòng · {request.items.reduce((sum, item) => sum + item.expectedQty, 0)} thiết bị dự kiến</span></div>
        <div className="gr-table-scroll"><table className="gr-table"><caption className="sr-only">Sản phẩm và số lượng dự kiến</caption><thead><tr><th>SKU</th><th>Sản phẩm</th><th>Số lượng dự kiến</th></tr></thead><tbody>{request.items.map(item => {
          const product = lookups.productVariants.find(x => x.id === item.productVariantId);
          return <tr key={item.id}><td className="gr-sku">{item.sku || product?.sku || `#${item.productVariantId}`}</td><td>{item.productName || product?.productName || "Sản phẩm đã lưu trong yêu cầu"}</td><td>{item.expectedQty}</td></tr>;
        })}</tbody></table></div>
      </section>
      <SupplierManifestPanel key={`manifest-${request.id}-${refresh}`} request={request} />
      {session.role === "Purchasing Manager" && request.status === "Pending" && <SupplierCommitmentForm key={`${request.id}-${refresh}`} requestId={request.id} suppliers={lookups.suppliers} items={request.items} initialSupplierId={request.supplierId} initialStatus={request.supplierCommitmentStatus} initialDeliveryDate={request.expectedDeliveryDate} onUpdated={() => updated("Đã cập nhật cam kết và bảng kê SKU/serial của nhà cung cấp.")} />}
      {session.role === "Manager" && request.status === "Pending" && <InboundReviewForm requestId={request.id} canApprove={canApprove} onReviewed={() => updated("Đã lưu quyết định đối với yêu cầu nhập.")} />}
      <InboundReceivingProgress key={`progress-${request.id}-${refresh}`} requestId={request.id} products={lookups.productVariants} />
    </>}
  </div>;
}
