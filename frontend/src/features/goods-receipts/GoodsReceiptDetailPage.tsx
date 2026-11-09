import { useState } from "react";
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
    reload, handleScan, handleScanForItem, handleSave, handleSubmit, handleDecision,
  } = useGoodsReceiptDetail(receiptId, session, onBusyChange);

  const [rowSerials, setRowSerials] = useState<Record<number, string>>({});

  if (loading) return <div className="gr-panel gr-empty" role="status">Đang tải chi tiết phiếu…</div>;
  if (!receipt) return <section className="gr-panel gr-empty"><h1>Chưa tải được phiếu</h1><p role="alert">{error}</p><div className="gr-inline-actions"><button className="gr-button gr-button--secondary" onClick={reload}>Thử lại</button><button className="gr-button gr-button--quiet" onClick={() => navigateTo(basePath)}>Về danh sách</button></div></section>;

  const selectedItem = receipt.items.find(item => item.id === selectedItemId) || receipt.items[0];

  return <>
    <button className="gr-back-link" disabled={busy} onClick={() => navigateTo(basePath)}>← Danh sách phiếu nhận</button>
    <header className="gr-page-heading">
      <div>
        <p className="gr-eyebrow">CHI TIẾT PHIẾU NHẬN HÀNG</p>
        <div className="gr-title-line">
          <h1>GR-{String(receipt.id).padStart(4, "0")}</h1>
          <ReceiptStatusBadge status={receipt.status} />
        </div>
        <p>Inbound #{receipt.inboundRequestId} <span className="gr-separator">/</span> Kho #{receipt.warehouseId} <span className="gr-separator">/</span> {formatReceiptDate(receipt.receivedDate)}</p>
      </div>
      <div className="gr-quantity">
        <strong>{quantity}</strong>
        <span>thiết bị đã quét</span>
      </div>
    </header>

    <ol className="gr-steps" aria-label="Tiến trình nhận hàng">
      <li className="done"><span>1</span>Tạo phiếu</li>
      <li className={placementComplete ? "done" : ""}><span>2</span>Quét & xếp bin</li>
      <li className={receipt.status !== "Draft" ? "done" : ""}><span>3</span>Gửi duyệt</li>
      <li className={receipt.status === "Approved" || receipt.status === "Rejected" ? "done" : ""}><span>4</span>Manager quyết định</li>
    </ol>

    {error && <div className="gr-alert gr-alert--error" role="alert">{error}</div>}
    {binError && <div className="gr-alert gr-alert--info" role="status">Không tải được tên/vị trí bin: {binError} Đang hiển thị mã bin đã lưu. <button className="gr-text-button" disabled={busy} onClick={reload}>Thử tải lại</button></div>}
    {notice && <div className="gr-alert gr-alert--success" role="status">{notice}</div>}
    {receipt.status === "PendingApproval" && <div className="gr-alert gr-alert--info">Phiếu đang chờ Manager duyệt. Thông tin đã được khóa; hàng chưa được cộng vào tồn kho.</div>}
    {receipt.status === "Approved" && <div className="gr-alert gr-alert--success">Phiếu đã được duyệt {receipt.decisionDate && `lúc ${formatReceiptDate(receipt.decisionDate)}`}. Hàng đã được nhập kho.</div>}
    {receipt.status === "Draft" && !editable && <div className="gr-alert gr-alert--info">Chế độ chỉ xem. Chỉ nhân viên tạo phiếu mới được chỉnh sửa bản nháp.</div>}

    {(receipt.status === "Approved" || receipt.status === "Rejected") && <section className={`gr-panel gr-decision-record${receipt.status === "Rejected" ? " gr-decision-record--rejected" : ""}`}>
      <div className="gr-panel-heading"><div><p className="gr-eyebrow">QUYẾT ĐỊNH CỦA QUẢN LÝ</p><h2>{receipt.status === "Rejected" ? "Phiếu đã bị từ chối" : "Phiếu đã được phê duyệt"}</h2></div></div>
      <div className="gr-decision-metadata">
        <div><span>Người quyết định</span><strong>{receipt.decidedByName || (receipt.decidedByUserId ? `Tài khoản #${receipt.decidedByUserId}` : "Chưa lưu người quyết định")}</strong></div>
        <div><span>Thời điểm quyết định</span><strong>{receipt.decisionDate ? formatReceiptDate(receipt.decisionDate) : "Chưa lưu thời điểm"}</strong></div>
      </div>
      {(receipt.status === "Rejected" || receipt.approvalNote) && <div className="gr-decision-note">
        <h3>{receipt.status === "Rejected" ? "Lý do từ chối" : "Ghi chú đã lưu"}</h3>
        <p>{receipt.status === "Rejected" ? receipt.rejectionReason || "Chưa có lý do từ chối được lưu." : receipt.approvalNote}</p>
      </div>}
    </section>}

    {editable && <section className="gr-scan-panel" aria-labelledby="gr-scan-title">
      <div>
        <p className="gr-eyebrow">QUÉT SERIAL / IMEI CHO TỪNG SẢN PHẨM</p>
        <h2 id="gr-scan-title">Nhận từng thiết bị theo sản phẩm</h2>
        <p>Chọn sản phẩm bên dưới để quét mã Serial/IMEI. Mỗi mã hợp lệ tương ứng với duy nhất 1 thiết bị vật lý của sản phẩm đó.</p>
      </div>

      <div className="gr-product-pills" role="tablist" aria-label="Danh sách sản phẩm cần nhận">
        {receipt.items.map(item => {
          const isDone = item.receivedQty >= item.remainingQty;
          const isSelected = item.id === selectedItemId;
          return (
            <button
              key={item.id}
              type="button"
              role="tab"
              aria-selected={isSelected}
              className={`gr-product-pill ${isSelected ? "gr-product-pill--active" : ""} ${isDone ? "gr-product-pill--done" : ""}`}
              onClick={() => {
                setSelectedItemId(item.id);
                setTimeout(() => scanInput.current?.focus(), 0);
              }}
            >
              <div className="gr-pill-title">{item.productName || item.sku}</div>
              <div className="gr-pill-meta">
                <code>{item.sku}</code>
                <span className="gr-pill-badge">{item.receivedQty} / {item.remainingQty} máy {isDone ? "✓" : ""}</span>
              </div>
            </button>
          );
        })}
      </div>

      {selectedItem && (
        <div className="gr-scan-target-box">
          <div className="gr-scan-target-info">
            <strong>Đang quét cho: {selectedItem.productName || selectedItem.sku}</strong>
            <code>({selectedItem.sku})</code>
            <span>· Tiến độ: {selectedItem.receivedQty}/{selectedItem.remainingQty} máy</span>
          </div>
          {selectedItem.receivedQty >= selectedItem.remainingQty ? (
            <span className="gr-badge-done">✓ Sản phẩm này đã quét đủ {selectedItem.remainingQty} thiết bị</span>
          ) : (
            <span className="gr-muted">Sẵn sàng nhận máy #{selectedItem.receivedQty + 1}</span>
          )}
        </div>
      )}

      <form className="gr-scan-form" onSubmit={event => { event.preventDefault(); void handleScan(); }}>
        <label>Sản phẩm đang chọn
          <select value={selectedItemId} disabled={busy} onChange={event => setSelectedItemId(Number(event.target.value))}>
            {receipt.items.map(item => (
              <option key={item.id} value={item.id}>
                {item.productName ? `${item.productName} (${item.sku})` : item.sku} · đã nhận {item.receivedQty}/{item.remainingQty} máy
              </option>
            ))}
          </select>
        </label>
        <label>Mã Serial / IMEI của thiết bị
          <input
            ref={scanInput}
            value={serialCode}
            disabled={busy || (selectedItem && selectedItem.receivedQty >= selectedItem.remainingQty)}
            required
            maxLength={100}
            autoComplete="off"
            autoCapitalize="off"
            spellCheck={false}
            onChange={event => setSerialCode(event.target.value)}
            placeholder={selectedItem && selectedItem.receivedQty >= selectedItem.remainingQty ? "Đã nhận đủ số lượng sản phẩm này" : `Quét hoặc nhập mã thiết bị #${(selectedItem?.receivedQty || 0) + 1}…`}
          />
        </label>
        <button
          className="gr-button gr-button--primary"
          type="submit"
          disabled={busy || !serialCode || (selectedItem && selectedItem.receivedQty >= selectedItem.remainingQty)}
        >
          {busy ? "Đang xử lý…" : "Thêm thiết bị ↵"}
        </button>
      </form>
    </section>}

    <section className="gr-panel">
      <div className="gr-panel-heading">
        <div>
          <h2>Danh sách sản phẩm & vị trí nhận kho</h2>
          <p>Nhập mã Serial/IMEI trực tiếp cho từng sản phẩm ở cột thao tác hoặc dùng khung quét nhanh bên trên.</p>
        </div>
        {binsChanged && <span className="gr-unsaved">Có vị trí chưa lưu</span>}
      </div>

      <div className="gr-table-scroll">
        <table className="gr-table gr-items-table">
          <caption className="sr-only">Số lượng nhận, bin và nhập serial theo từng sản phẩm</caption>
          <thead>
            <tr>
              <th>Sản phẩm & SKU</th>
              <th>Dự kiến</th>
              <th>Đã nhận trước</th>
              <th>Cần nhận</th>
              <th>Phiếu này</th>
              <th>Bin nhận hàng</th>
              {editable && <th>Quét / Nhập máy cho SP này</th>}
            </tr>
          </thead>
          <tbody>
            {receipt.items.map(item => {
              const isSelected = item.id === selectedItemId;
              const isDone = item.receivedQty >= item.remainingQty;
              return (
                <tr key={`item-${item.id}`} className={isSelected && editable ? "gr-item-row--active" : ""}>
                  <td className="gr-product-name-col">
                    <strong>{item.productName || item.sku}</strong>
                    <code>{item.sku}</code>
                  </td>
                  <td>{item.expectedQty}</td>
                  <td>{item.approvedReceivedQty}</td>
                  <td><strong>{item.remainingQty}</strong></td>
                  <td>
                    <strong className="gr-count">{item.receivedQty}</strong>
                  </td>
                  <td>
                    {editable ? (
                      <select
                        aria-label={`Bin cho ${item.sku}`}
                        disabled={busy || Boolean(binError)}
                        value={binChoices[item.id] ?? ""}
                        onChange={event => setBinChoices(values => ({ ...values, [item.id]: event.target.value ? Number(event.target.value) : null }))}
                      >
                        <option value="">Chưa chọn bin</option>
                        {binError && item.binId && <option value={item.binId}>Bin #{item.binId} (đã lưu)</option>}
                        {bins.map(bin => <option key={bin.id} value={bin.id}>{bin.binCode} · còn {bin.availableWeightKg.toLocaleString("vi-VN")} kg</option>)}
                      </select>
                    ) : (
                      bins.find(bin => bin.id === item.binId)?.binCode || (item.binId ? `Bin #${item.binId}` : "Chưa chọn")
                    )}
                  </td>
                  {editable && (
                    <td>
                      {isDone ? (
                        <span className="gr-badge-done">✓ Đã đủ {item.remainingQty} máy</span>
                      ) : (
                        <form
                          className="gr-row-scan-form"
                          onSubmit={event => {
                            event.preventDefault();
                            const val = (rowSerials[item.id] || "").trim();
                            if (val) {
                              void handleScanForItem(item.id, val);
                              setRowSerials(prev => ({ ...prev, [item.id]: "" }));
                            }
                          }}
                        >
                          <input
                            value={rowSerials[item.id] || ""}
                            disabled={busy}
                            maxLength={100}
                            placeholder={`Serial máy #${item.receivedQty + 1}…`}
                            onChange={event => setRowSerials(prev => ({ ...prev, [item.id]: event.target.value }))}
                          />
                          <button
                            type="submit"
                            className="gr-button gr-button--primary"
                            disabled={busy || !(rowSerials[item.id] || "").trim()}
                          >
                            + Nhập
                          </button>
                        </form>
                      )}
                    </td>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {editable && <div className="gr-form-footer">
        <p>Bạn có thể lưu nháp khi chưa chọn đủ bin. Gửi duyệt yêu cầu dữ liệu đầy đủ.</p>
        <div className="gr-inline-actions">
          <button className="gr-button gr-button--secondary" disabled={busy} onClick={() => void handleSave()}>Lưu nháp</button>
          <button className="gr-button gr-button--primary" disabled={busy || quantity === 0} onClick={() => void handleSubmit()}>{busy ? "Đang xử lý…" : "Gửi Manager duyệt →"}</button>
        </div>
      </div>}
    </section>

    <section className="gr-panel gr-serial-panel">
      <div className="gr-panel-heading">
        <div>
          <h2>Serial / IMEI đã ghi nhận theo từng sản phẩm</h2>
          <p>{quantity} thiết bị đã được gắn mã định danh cho phiếu này.</p>
        </div>
      </div>
      {quantity === 0 ? (
        <p className="gr-muted">Chưa có serial nào. Vui lòng chọn sản phẩm và quét từng thiết bị ở trên.</p>
      ) : (
        receipt.items.filter(item => item.serials.length > 0).map(item => (
          <details key={item.id} className="gr-serial-group" open>
            <summary>
              <strong>{item.productName || item.sku}</strong> (<code>{item.sku}</code>)
              <span>{item.serials.length} thiết bị đã ghi nhận</span>
            </summary>
            <ul>
              {item.serials.map((serial, idx) => (
                <li key={serial.id} className="gr-serial-chip">
                  <span className="gr-serial-chip-idx">Thiết bị #{idx + 1}:</span>
                  <strong>{serial.serialCode}</strong>
                </li>
              ))}
            </ul>
          </details>
        ))
      )}
    </section>

    {canDecide && <section className="gr-review-panel">
      <div>
        <p className="gr-eyebrow">MANAGER REVIEW</p>
        <h2>Kiểm tra và quyết định</h2>
        <p>Đối chiếu sản phẩm, số lượng, serial và bin trước khi duyệt phiếu.</p>
      </div>
      <div className="gr-inline-actions">
        <button className="gr-button gr-button--danger-outline" disabled={busy} onClick={() => setDecision("reject")}>Từ chối phiếu</button>
        <button className="gr-button gr-button--primary" disabled={busy} onClick={() => setDecision("approve")}>Duyệt nhập kho</button>
      </div>
    </section>}

    {decision && <ReceiptDecisionDialog decision={decision} receipt={receipt} busy={busy} onCancel={() => setDecision(null)} onConfirm={handleDecision} />}
  </>;
}
