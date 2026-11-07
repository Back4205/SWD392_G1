import type { ReceiptSession } from "./goodsReceipt.types";
import { formatReceiptDate } from "./receipt.helpers";
import ReceiptStatusBadge from "./ReceiptStatusBadge";
import ReceiptDecisionDialog from "./ReceiptDecisionDialog";
import { navigateTo } from "../../hooks/useAppRoute";
import useGoodsReceiptDetail from "../../hooks/useGoodsReceiptDetail";

interface Props {
  receiptId: number;
  basePath: string;
  session: ReceiptSession;
  busy: boolean;
  onBusyChange: (busy: boolean) => void;
}

export default function GoodsReceiptDetailPage({ receiptId, session, busy, onBusyChange, basePath }: Props) {
  const {
    receipt, bins, binChoices, setBinChoices, selectedItemId, setSelectedItemId,
    serialCode, setSerialCode, loading, error, binError, notice, decision, setDecision,
    scanInput, editable, canDecide, quantity, placementComplete, binsChanged,
    reload, handleScan, handleSave, handleSubmit, handleDecision,
  } = useGoodsReceiptDetail(receiptId, session, onBusyChange);

  if (loading) return <div className="gr-panel gr-empty" role="status">Đang tải chi tiết phiếu…</div>;
  if (!receipt) return <section className="gr-panel gr-empty"><h1>Chưa tải được phiếu</h1><p role="alert">{error}</p><div className="gr-inline-actions"><button className="gr-button gr-button--secondary" onClick={reload}>Thử lại</button><button className="gr-button gr-button--quiet" onClick={() => navigateTo(basePath)}>Về danh sách</button></div></section>;

  return <>
    <button className="gr-back-link" disabled={busy} onClick={() => navigateTo(basePath)}>← Danh sách phiếu nhận</button>
    <header className="gr-page-heading"><div><p className="gr-eyebrow">CHI TIẾT PHIẾU NHẬN</p><div className="gr-title-line"><h1>GR-{String(receipt.id).padStart(4, "0")}</h1><ReceiptStatusBadge status={receipt.status} /></div>
      <p>Inbound #{receipt.inboundRequestId} <span className="gr-separator">/</span> Kho #{receipt.warehouseId} <span className="gr-separator">/</span> {formatReceiptDate(receipt.receivedDate)}</p></div>
      <div className="gr-quantity"><strong>{quantity}</strong><span>thiết bị đã quét</span></div>
    </header>

    <ol className="gr-steps" aria-label="Tiến trình nhận hàng">
      <li className="done"><span>1</span>Tạo phiếu</li><li className={placementComplete ? "done" : ""}><span>2</span>Quét & xếp bin</li>
      <li className={receipt.status !== "Draft" ? "done" : ""}><span>3</span>Gửi duyệt</li><li className={receipt.status === "Approved" || receipt.status === "Rejected" ? "done" : ""}><span>4</span>Manager quyết định</li>
    </ol>

    {error && <div className="gr-alert gr-alert--error" role="alert">{error}</div>}
    {binError && <div className="gr-alert gr-alert--info" role="status">Không tải được tên/vị trí bin: {binError} Đang hiển thị mã bin đã lưu. <button className="gr-text-button" disabled={busy} onClick={reload}>Thử tải lại</button></div>}
    {notice && <div className="gr-alert gr-alert--success" role="status">{notice}</div>}
    {receipt.status === "PendingApproval" && <div className="gr-alert gr-alert--info">Phiếu đang chờ Manager duyệt. Thông tin đã được khóa; hàng chưa được cộng vào tồn kho.</div>}
    {receipt.status === "Approved" && <div className="gr-alert gr-alert--success">Phiếu đã được duyệt {receipt.decisionDate && `lúc ${formatReceiptDate(receipt.decisionDate)}`}. Hàng đã được nhập kho.</div>}
    {receipt.status === "Draft" && !editable && <div className="gr-alert gr-alert--info">Chế độ chỉ xem. Chỉ nhân viên tạo phiếu mới được chỉnh sửa bản nháp.</div>}

    {(receipt.status === "Approved" || receipt.status === "Rejected") && <section className={`gr-panel gr-decision-record${receipt.status === "Rejected" ? " gr-decision-record--rejected" : ""}`}>
      <div className="gr-panel-heading"><div><p className="gr-eyebrow">QUYẾT ĐỊNH CỦA QUẢN LÝ</p><h2>{receipt.status === "Rejected" ? "Phiếu đã bị từ chối" : "Phiếu đã được phê duyệt"}</h2></div></div>
      <div className="gr-decision-metadata"><div><span>Người quyết định</span><strong>{receipt.decidedByName || (receipt.decidedByUserId ? `Tài khoản #${receipt.decidedByUserId}` : "Chưa lưu người quyết định")}</strong></div><div><span>Thời điểm quyết định</span><strong>{receipt.decisionDate ? formatReceiptDate(receipt.decisionDate) : "Chưa lưu thời điểm"}</strong></div></div>
      {(receipt.status === "Rejected" || receipt.approvalNote) && <div className="gr-decision-note"><h3>{receipt.status === "Rejected" ? "Lý do từ chối" : "Ghi chú đã lưu"}</h3><p>{receipt.status === "Rejected" ? receipt.rejectionReason || "Chưa có lý do từ chối được lưu." : receipt.approvalNote}</p></div>}
    </section>}


    {editable && <section className="gr-scan-panel" aria-labelledby="gr-scan-title">
      <div><p className="gr-eyebrow">NHẬN TỪNG THIẾT BỊ</p><h2 id="gr-scan-title">Quét Serial / IMEI</h2><p>Chọn đúng sản phẩm, quét mã và nhấn Enter. Mỗi mã hợp lệ được tính một thiết bị.</p></div>
      <form className="gr-scan-form" onSubmit={event => { event.preventDefault(); void handleScan(); }}>
        <label>Sản phẩm đang nhận<select value={selectedItemId} disabled={busy} onChange={event => setSelectedItemId(Number(event.target.value))}>
          {receipt.items.map(item => <option key={item.id} value={item.id}>{item.sku} · đã quét {item.receivedQty}/{item.remainingQty}</option>)}
        </select></label>
        <label>Serial / IMEI<input ref={scanInput} value={serialCode} disabled={busy} required maxLength={100} autoComplete="off" autoCapitalize="off" spellCheck={false}
          onChange={event => setSerialCode(event.target.value)} placeholder="Quét hoặc nhập mã thiết bị…" /></label>
        <button className="gr-button gr-button--primary" type="submit" disabled={busy || !serialCode}>{busy ? "Đang xử lý…" : "Thêm serial ↵"}</button>
      </form>
    </section>}

    <section className="gr-panel">
      <div className="gr-panel-heading"><div><h2>Sản phẩm & vị trí nhận</h2><p>“Đã nhận chính thức” chỉ tính các phiếu được duyệt của Inbound này.</p></div>{binsChanged && <span className="gr-unsaved">Có vị trí chưa lưu</span>}</div>
      <div className="gr-table-scroll"><table className="gr-table gr-items-table"><caption className="sr-only">Số lượng nhận và bin theo từng SKU</caption>
        <thead><tr><th>Sản phẩm / SKU</th><th>Dự kiến</th><th>Đã nhận chính thức</th><th>Còn thiếu</th><th>Phiếu này</th><th>Bin nhận hàng</th></tr></thead>
        <tbody>{receipt.items.map(item => <tr key={item.id}>
          <td className="gr-sku">{item.sku}</td><td>{item.expectedQty}</td><td>{item.approvedReceivedQty}</td><td>{item.remainingQty}</td><td><strong className="gr-count">{item.receivedQty}</strong></td>
          <td>{editable ? <select aria-label={`Bin cho ${item.sku}`} disabled={busy || Boolean(binError)} value={binChoices[item.id] ?? ""} onChange={event => setBinChoices(values => ({ ...values, [item.id]: event.target.value ? Number(event.target.value) : null }))}>
            <option value="">Chưa chọn bin</option>{binError && item.binId && <option value={item.binId}>Bin #{item.binId} (đã lưu)</option>}{bins.map(bin => <option key={bin.id} value={bin.id}>{bin.binCode} · còn {bin.availableWeightKg.toLocaleString("vi-VN")} kg</option>)}
          </select> : bins.find(bin => bin.id === item.binId)?.binCode || (item.binId ? `Bin #${item.binId}` : "Chưa chọn")}</td>
        </tr>)}</tbody>
      </table></div>
      {editable && <div className="gr-form-footer"><p>Bạn có thể lưu nháp khi chưa chọn đủ bin. Gửi duyệt yêu cầu dữ liệu đầy đủ.</p><div className="gr-inline-actions">
        <button className="gr-button gr-button--secondary" disabled={busy} onClick={() => void handleSave()}>Lưu nháp</button>
        <button className="gr-button gr-button--primary" disabled={busy || quantity === 0} onClick={() => void handleSubmit()}>{busy ? "Đang xử lý…" : "Gửi Manager duyệt →"}</button>
      </div></div>}
    </section>

    <section className="gr-panel gr-serial-panel"><div className="gr-panel-heading"><div><h2>Serial đã ghi nhận</h2><p>{quantity} mã được lưu cùng phiếu, kể cả khi tải lại trang.</p></div></div>
      {quantity === 0 ? <p className="gr-muted">Chưa có serial. Bắt đầu quét thiết bị ở phía trên.</p> : receipt.items.filter(item => item.serials.length > 0).map(item => <details key={item.id} className="gr-serial-group" open>
        <summary>{item.sku}<span>{item.serials.length} mã</span></summary><ul>{item.serials.map(serial => <li key={serial.id}>{serial.serialCode}</li>)}</ul>
      </details>)}
    </section>

    {canDecide && <section className="gr-review-panel"><div><p className="gr-eyebrow">MANAGER REVIEW</p><h2>Kiểm tra và quyết định</h2><p>Đối chiếu sản phẩm, số lượng, serial và bin trước khi duyệt phiếu.</p></div><div className="gr-inline-actions">
      <button className="gr-button gr-button--danger-outline" disabled={busy} onClick={() => setDecision("reject")}>Từ chối phiếu</button>
      <button className="gr-button gr-button--primary" disabled={busy} onClick={() => setDecision("approve")}>Duyệt nhập kho</button>
    </div></section>}
    {decision && <ReceiptDecisionDialog decision={decision} receipt={receipt} busy={busy} onCancel={() => setDecision(null)} onConfirm={handleDecision} />}
  </>;
}
