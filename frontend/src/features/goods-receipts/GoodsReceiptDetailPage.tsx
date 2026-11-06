import { useEffect, useRef, useState } from "react";
import {
  addReceiptSerial, approveGoodsReceipt, getGoodsReceipt, getReceiptBins,
  rejectGoodsReceipt, saveGoodsReceipt, submitGoodsReceipt,
} from "./goodsReceipt.api";
import type { GoodsReceipt, ReceiptBin, ReceiptSession } from "./goodsReceipt.types";
import { formatReceiptDate, isStaleReceipt, receiptErrorMessage } from "./receipt.helpers";
import ReceiptStatusBadge from "./ReceiptStatusBadge";
import ReceiptDecisionDialog from "./ReceiptDecisionDialog";
import { navigateTo } from "../../hooks/useAppRoute";

interface Props {
  receiptId: number;
  basePath: string;
  session: ReceiptSession;
  busy: boolean;
  onBusyChange: (busy: boolean) => void;
}

function getSavedBins(receipt: GoodsReceipt) {
  const values: Record<number, number | null> = {};
  for (const item of receipt.items) values[item.id] = item.binId;
  return values;
}

export default function GoodsReceiptDetailPage({ receiptId, session, busy, onBusyChange, basePath }: Props) {
  const [receipt, setReceipt] = useState<GoodsReceipt | null>(null);
  const [bins, setBins] = useState<ReceiptBin[]>([]);
  const [binChoices, setBinChoices] = useState<Record<number, number | null>>({});
  const [selectedItemId, setSelectedItemId] = useState(0);
  const [serialCode, setSerialCode] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [binError, setBinError] = useState("");
  const [notice, setNotice] = useState("");
  const [decision, setDecision] = useState<"approve" | "reject" | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const scanInput = useRef<HTMLInputElement>(null);
  const writeInProgress = useRef(false);

  useEffect(() => {
    let active = true;
    async function loadDetail() {
      setLoading(true);
      setError("");
      setBinError("");
      try {
        const result = await getGoodsReceipt(receiptId);
        if (active) {
          setReceipt(result);
          setBinChoices(getSavedBins(result));
          setSelectedItemId(result.items[0]?.id || 0);
        }
        // Phiếu lịch sử vẫn xem được khi không tải được danh sách bin (ví dụ kho ngừng hoạt động).
        try {
          const locations = await getReceiptBins(result.warehouseId);
          if (active) setBins(locations);
        } catch (lookupError) {
          if (active) setBinError(receiptErrorMessage(lookupError));
        }
      } catch (err) {
        if (active) setError(receiptErrorMessage(err));
      } finally {
        if (active) setLoading(false);
      }
    }
    void loadDetail();
    return () => { active = false; };
  }, [receiptId, session.userId, reloadKey]);

  const editable = receipt?.status === "Draft" && session.role === "Warehouse Staff" && receipt.receivedByUserId === session.userId;
  const canDecide = receipt?.status === "PendingApproval" && session.role === "Manager";
  const selectedItem = receipt?.items.find(item => item.id === selectedItemId);
  const quantity = receipt?.items.reduce((total, item) => total + item.receivedQty, 0) || 0;
  const placementComplete = quantity > 0 && receipt?.items.every(item => item.receivedQty === 0 || item.binId !== null);
  const binsChanged = receipt?.items.some(item => (binChoices[item.id] ?? null) !== item.binId) || false;

  function beginWrite() {
    // Ref chặn cả trường hợp máy scan gửi Enter liên tiếp trước khi React kịp render.
    if (writeInProgress.current) return false;
    writeInProgress.current = true;
    onBusyChange(true);
    setError("");
    setNotice("");
    return true;
  }

  function finishWrite() {
    writeInProgress.current = false;
    onBusyChange(false);
  }

  async function handleWriteError(err: unknown) {
    setError(receiptErrorMessage(err));
    if (isStaleReceipt(err)) {
      setDecision(null);
      try {
        const latest = await getGoodsReceipt(receiptId);
        setReceipt(latest);
        setBinChoices(getSavedBins(latest));
      } catch (reloadError) {
        setError(`Chưa tải lại được phiếu mới. ${receiptErrorMessage(reloadError)}`);
      }
    }
    // Duplicate cũng là 409, nhưng không phải stale: giữ dòng scan và lựa chọn bin.
  }

  async function handleScan() {
    if (!receipt || !selectedItem || !serialCode || !beginWrite()) return;
    try {
      await addReceiptSerial(receipt.id, selectedItem.id, receipt.version, serialCode, selectedItem.sku);
      const latest = await getGoodsReceipt(receipt.id);
      setReceipt(latest);
      // Giữ binChoices riêng để một lần scan không làm mất bin chưa lưu.
      setSerialCode("");
      setNotice(`Đã lưu serial. Phiếu hiện có ${latest.items.reduce((total, item) => total + item.receivedQty, 0)} thiết bị.`);
    } catch (err) {
      await handleWriteError(err);
    } finally {
      finishWrite();
      setTimeout(() => scanInput.current?.focus(), 0);
    }
  }

  async function handleSave() {
    if (!receipt || !beginWrite()) return;
    try {
      const items = receipt.items.map(item => ({ itemId: item.id, binId: binChoices[item.id] ?? null }));
      const latest = await saveGoodsReceipt(receipt.id, receipt.version, items);
      setReceipt(latest);
      setBinChoices(getSavedBins(latest));
      setNotice("Đã lưu phiếu nháp. Tồn kho chưa thay đổi.");
    } catch (err) {
      await handleWriteError(err);
    } finally {
      finishWrite();
    }
  }

  async function handleSubmit() {
    if (!receipt || !beginWrite()) return;
    try {
      // Bước 1: lưu bin thành công; bước 2: gửi duyệt bằng Version vừa được server trả về.
      const items = receipt.items.map(item => ({ itemId: item.id, binId: binChoices[item.id] ?? null }));
      const saved = await saveGoodsReceipt(receipt.id, receipt.version, items);
      setReceipt(saved);
      setBinChoices(getSavedBins(saved));
      const pending = await submitGoodsReceipt(saved.id, saved.version);
      setReceipt(pending);
      setNotice("Đã gửi Manager duyệt. Phiếu được khóa chỉnh sửa; tồn kho chưa thay đổi.");
    } catch (err) {
      await handleWriteError(err);
    } finally {
      finishWrite();
    }
  }

  async function handleDecision(note: string) {
    if (!receipt || !decision || !beginWrite()) return;
    try {
      const result = decision === "approve"
        ? await approveGoodsReceipt(receipt.id, receipt.version)
        : await rejectGoodsReceipt(receipt.id, receipt.version, note);
      setReceipt(result);
      setDecision(null);
      setNotice(result.status === "Approved" ? "Đã duyệt phiếu. Tồn kho, serial và tiến độ Inbound đã được cập nhật." : "Đã từ chối phiếu và lưu lý do. Tồn kho không thay đổi.");
    } catch (err) {
      await handleWriteError(err);
      setDecision(null); // Yêu cầu xem lại dữ liệu trước khi đưa ra quyết định tiếp.
    } finally {
      finishWrite();
    }
  }

  if (loading) return <div className="gr-panel gr-empty" role="status">Đang tải chi tiết phiếu…</div>;
  if (!receipt) return <section className="gr-panel gr-empty"><h1>Chưa tải được phiếu</h1><p role="alert">{error}</p><div className="gr-inline-actions"><button className="gr-button gr-button--secondary" onClick={() => setReloadKey(value => value + 1)}>Thử lại</button><button className="gr-button gr-button--quiet" onClick={() => navigateTo(basePath)}>Về danh sách</button></div></section>;

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
    {binError && <div className="gr-alert gr-alert--info" role="status">Không tải được tên/vị trí bin: {binError} Đang hiển thị mã bin đã lưu. <button className="gr-text-button" disabled={busy} onClick={() => setReloadKey(value => value + 1)}>Thử tải lại</button></div>}
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
