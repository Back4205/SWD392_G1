import { useState } from "react";
import type { AuthUser } from "../auth/auth.types";
import { navigateTo } from "../../hooks/useAppRoute";
import useInboundRequestDetail from "../../hooks/useInboundRequestDetail";
import { inboundDate, inboundDecisionDate, inboundStatuses } from "./inbound.helpers";
import SupplierCommitmentForm from "./SupplierCommitmentForm";
import InboundReviewForm from "./InboundReviewForm";
import InboundReceivingProgress from "./InboundReceivingProgress";

interface Props { requestId: number; session: AuthUser; onBack: () => void; }

export default function InboundRequestDetailPage({ requestId, session, onBack }: Props) {
  const { request, lookups, loading, error, refresh, reload } = useInboundRequestDetail(requestId);
  const [notice, setNotice] = useState("");

  function updated(message: string) { setNotice(message); reload(); }
  const canReceive = session.role === "Warehouse Staff" && request && ["Approved", "PartiallyReceived"].includes(request.status);
  const canApprove = Boolean(request && ["Accepted", "Confirmed"].includes(request.supplierCommitmentStatus || "") && request.expectedDeliveryDate && request.items.length > 0 && request.items.every(item => item.expectedQty > 0));

  return <div className="inbound-workspace">
    <button className="gr-back-link" onClick={onBack}>← Danh sách yêu cầu nhập</button>
    <header className="gr-page-heading"><div><p className="gr-eyebrow">CHI TIẾT YÊU CẦU NHẬP</p><h1>IN-{String(requestId).padStart(4, "0")}</h1><p>Thông tin kế hoạch nhập hàng, nhà cung cấp và số lượng đã nhận chính thức.</p></div>
      <div className="gr-inline-actions"><button className="gr-button gr-button--secondary" disabled={loading} onClick={() => { setNotice(""); reload(); }}>Làm mới thông tin</button>{canReceive && <button className="gr-button gr-button--primary" onClick={() => navigateTo(`/staff/receipts/new?inboundRequestId=${requestId}`)}>Tạo phiếu nhận hàng</button>}</div>
    </header>
    {notice && <div className="gr-alert gr-alert--success" role="status">{notice}</div>}
    {error && <div className="gr-alert gr-alert--error" role="alert">{error} <button className="gr-text-button" onClick={() => reload()}>Thử lại</button></div>}
    {loading ? <section className="gr-panel gr-empty" role="status">Đang tải thông tin yêu cầu…</section> : !error && request && lookups && <>
      <section className="gr-panel ib-section">
        <div className="ib-section-heading"><h2>Thông tin yêu cầu</h2><span className={`ib-status ib-status--${request.status}`}>{inboundStatuses[request.status] || request.status}</span></div>
        <dl className="ib-info-grid">
          <div><dt>Kho nhận hàng</dt><dd>{lookups.warehouses.find(x => x.id === request.warehouseId)?.warehouseName || `Kho #${request.warehouseId}`}</dd></div>
          <div><dt>Ngày tạo</dt><dd>{inboundDate(request.createdDate)}</dd></div>
          <div><dt>Nhà cung cấp</dt><dd>{lookups.suppliers.find(x => x.id === request.supplierId)?.supplierName || (request.supplierId ? `Nhà cung cấp #${request.supplierId}` : "Chờ phân công")}</dd></div>
          <div><dt>Cam kết cung cấp</dt><dd>{["Accepted", "Confirmed"].includes(request.supplierCommitmentStatus || "") ? "Chấp nhận cung cấp" : request.supplierCommitmentStatus === "Rejected" ? "Nhà cung cấp từ chối" : "Chưa cập nhật"}</dd></div>
          <div><dt>Ngày giao dự kiến</dt><dd>{inboundDate(request.expectedDeliveryDate)}</dd></div>
          <div><dt>Người lập</dt><dd>Tài khoản #{request.requestedByUserId}</dd></div>
          <div><dt>Nhân viên kho được phân công</dt><dd>{request.assignedStaffName || (request.assignedStaffUserId ? `Tài khoản #${request.assignedStaffUserId}` : "Chưa lưu phân công")}</dd></div>
        </dl>
      </section>
      {["Approved", "PartiallyReceived", "Completed", "Rejected"].includes(request.status) && <section className={`gr-panel ib-section ib-decision-record${request.status === "Rejected" ? " ib-decision-record--rejected" : ""}`}>
        <div className="ib-section-heading"><div><p className="gr-eyebrow">QUYẾT ĐỊNH CỦA QUẢN LÝ</p><h2>{request.status === "Rejected" ? "Yêu cầu đã bị từ chối" : "Yêu cầu đã được phê duyệt"}</h2></div></div>
        <dl className="ib-info-grid"><div><dt>Người quyết định</dt><dd>{request.decidedByName || (request.approvedByUserId ? `Tài khoản #${request.approvedByUserId}` : "Chưa lưu người quyết định")}</dd></div><div><dt>Thời điểm quyết định</dt><dd>{inboundDecisionDate(request.decisionDate)}</dd></div></dl>
        {(request.status === "Rejected" || request.approvalNote) && <><h3>{request.status === "Rejected" ? "Lý do từ chối" : "Ghi chú đã lưu"}</h3><p className="ib-decision-note">{request.status === "Rejected" ? request.rejectionReason || "Chưa có lý do từ chối được lưu." : request.approvalNote}</p></>}
      </section>}
      {request.status === "Canceled" && <div className="gr-alert gr-alert--info">Yêu cầu đã hủy do nhà cung cấp từ chối cung cấp. Không thể duyệt yêu cầu hoặc tạo phiếu nhận hàng.</div>}
      <section className="gr-panel">
        <div className="gr-panel-heading"><h2>Danh sách sản phẩm</h2><span className="ib-muted">{request.items.length} dòng · {request.items.reduce((sum, item) => sum + item.expectedQty, 0)} thiết bị dự kiến</span></div>
        <div className="gr-table-scroll"><table className="gr-table"><caption className="sr-only">Sản phẩm và số lượng dự kiến</caption><thead><tr><th>SKU</th><th>Sản phẩm</th><th>Số lượng dự kiến</th></tr></thead><tbody>{request.items.map(item => {
          const product = lookups.productVariants.find(x => x.id === item.productVariantId);
          return <tr key={item.id}><td className="gr-sku">{item.sku || product?.sku || `#${item.productVariantId}`}</td><td>{item.productName || product?.productName || "Sản phẩm đã lưu trong yêu cầu"}</td><td>{item.expectedQty}</td></tr>;
        })}</tbody></table></div>
      </section>
      {session.role === "Purchasing Manager" && request.status === "Pending" && <SupplierCommitmentForm key={`${request.id}-${refresh}`} requestId={request.id} revision={request.revision} suppliers={lookups.suppliers} products={lookups.productVariants} items={request.items} initialSupplierId={request.supplierId} initialStatus={request.supplierCommitmentStatus} initialDeliveryDate={request.expectedDeliveryDate} onUpdated={() => updated("Đã cập nhật cam kết và các mặt hàng nhà cung cấp xác nhận.")} />}
      {session.role === "Manager" && request.status === "Pending" && <InboundReviewForm key={`review-${request.id}-${refresh}`} requestId={request.id} revision={request.revision} canApprove={canApprove} onReviewed={() => updated("Đã lưu quyết định đối với yêu cầu nhập.")} />}
      {request.hasGoodsReceipts && <InboundReceivingProgress key={`progress-${request.id}-${refresh}`} requestId={request.id} products={lookups.productVariants} />}
    </>}
  </div>;
}
