import { useState } from "react";
import type { GoodsReceipt } from "./goodsReceipt.types";

interface Props { receipt: GoodsReceipt; }
const stateLabels: Record<string, string> = {
  AwaitingReceipt: "Chờ tiếp nhận",
  ScannedThisReceipt: "Khớp · Phiếu này",
  ScannedOtherReceipt: "Đang ở phiếu khác",
  Received: "Đã nhận chính thức",
};

export default function ReceiptSerialComparison({ receipt }: Props) {
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("");
  if (!receipt.hasSupplierManifest) return <section className="gr-panel gr-comparison-legacy"><h2>Đối chiếu với nhà cung cấp</h2><p>Yêu cầu nhập này đã được duyệt trước khi có bảng kê serial. Không có danh sách dự kiến để đối chiếu từng mã; hệ thống vẫn kiểm tra SKU, serial trùng và số lượng thực nhận.</p></section>;

  const rows = receipt.items.flatMap(item => item.expectedSerials.map(expected => {
    const local = item.serials.find(scan => scan.serialCode.toLowerCase() === expected.serialCode.toLowerCase());
    return { ...expected, sku: item.sku, actualSerial: local?.serialCode || expected.scannedSerialCode,
      rejectedHere: receipt.status === "Rejected" && Boolean(local) && expected.status === "AwaitingReceipt" };
  }));
  const query = search.trim().toLowerCase();
  const filtered = rows.filter(row => (!filter || row.status === filter)
    && (!query || row.sku.toLowerCase().includes(query) || row.serialCode.toLowerCase().includes(query)));
  const received = rows.filter(row => row.status === "Received").length;
  const waiting = rows.filter(row => row.status === "AwaitingReceipt").length;

  return <section className="gr-panel gr-comparison" aria-labelledby="gr-comparison-heading">
    <div className="gr-panel-heading"><div><p className="gr-eyebrow">BẢNG KÊ NHÀ CUNG CẤP ↔ HÀNG THỰC NHẬN</p><h2 id="gr-comparison-heading">Đối chiếu SKU & Serial</h2><p>Danh sách Purchasing đã xác nhận được khóa theo yêu cầu nhập. Mã ngoài danh sách hoặc sai SKU sẽ bị chặn khi quét.</p></div></div>
    <div className="gr-comparison-metrics"><div><span>Nhà cung cấp khai báo</span><strong>{rows.length}</strong></div><div><span>Quét ở phiếu này</span><strong>{receipt.items.reduce((sum, item) => sum + item.serials.length, 0)}</strong></div><div><span>Đã nhận chính thức</span><strong>{received}</strong></div><div><span>Chờ tiếp nhận</span><strong>{waiting}</strong></div></div>
    <div className="gr-comparison-filters"><label>Tìm SKU hoặc serial<input type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Nhập mã để đối chiếu…" /></label><label>Trạng thái đối chiếu<select value={filter} onChange={event => setFilter(event.target.value)}><option value="">Tất cả mã dự kiến</option>{Object.entries(stateLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label></div>
    <div className="gr-table-scroll"><table className="gr-table gr-comparison-table"><caption className="sr-only">So sánh serial nhà cung cấp khai báo và serial đã quét</caption><thead><tr><th>SKU</th><th>Serial nhà cung cấp</th><th>Serial thực tế đã quét</th><th>Kết quả đối chiếu</th><th>Phiếu ghi nhận</th></tr></thead><tbody>
      {filtered.map(row => <tr key={`${row.sku}-${row.serialCode}`} className={`gr-compare-row--${row.status}`}><td className="gr-sku">{row.sku}</td><td><code>{row.serialCode}</code></td><td>{row.actualSerial ? <code>{row.actualSerial}</code> : <span className="gr-muted">—</span>}</td><td><span className={`gr-compare-badge gr-compare-badge--${row.rejectedHere ? "Rejected" : row.status}`}>{row.rejectedHere ? "Không tính · Phiếu từ chối" : stateLabels[row.status]}</span></td><td>{row.receiptId ? `GR-${String(row.receiptId).padStart(4, "0")}` : "—"}</td></tr>)}
      {filtered.length === 0 && <tr><td colSpan={5}>Không có mã phù hợp với điều kiện tìm kiếm.</td></tr>}
    </tbody></table></div>
    <p className="gr-comparison-note">{receipt.status === "Rejected" ? "Phiếu này bị từ chối. Serial quét ở phiếu này không được tính đã nhận; có thể tiếp nhận lại bằng phiếu khác." : "Khớp serial mới xác nhận đúng thiết bị. Chỉ khi Manager duyệt phiếu, hàng mới được tính đã nhận chính thức và cộng tồn kho."}</p>
  </section>;
}
