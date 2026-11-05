import { useEffect, useState } from "react";
import type { AuthUser } from "../auth/auth.types";
import { navigateTo } from "../../hooks/useAppRoute";
import { getInboundLookups, getInboundRequests, type InboundLookups, type InboundRequestListItem } from "./inbound.api";
import { inboundDate, inboundError, inboundStatuses } from "./inbound.helpers";

interface Props { session: AuthUser; basePath: string; }

export default function InboundRequestListPage({ session, basePath }: Props) {
  const [requests, setRequests] = useState<InboundRequestListItem[]>([]);
  const [lookups, setLookups] = useState<InboundLookups | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      setLoading(true); setError("");
      try {
        const [rows, options] = await Promise.all([getInboundRequests(), getInboundLookups()]);
        if (active) { setRequests(rows); setLookups(options); }
      } catch (err) { if (active) setError(inboundError(err)); }
      finally { if (active) setLoading(false); }
    }
    void load();
    return () => { active = false; };
  }, [reload]);

  const filtered = requests.filter(row => String(row.id).includes(search.trim()) && (!status || row.status === status));
  const manager = session.role === "Manager";
  const purchasing = session.role === "Purchasing Manager";

  return <div className="inbound-workspace">
    <header className="gr-page-heading"><div><p className="gr-eyebrow">KẾ HOẠCH NHẬP KHO</p><h1>Yêu cầu nhập kho</h1><p>{manager ? "Lập kế hoạch nhập hàng và kiểm tra yêu cầu trước khi phê duyệt." : purchasing ? "Cập nhật cam kết nhà cung cấp và theo dõi tiến độ giao nhận." : "Theo dõi các yêu cầu đã được duyệt trong kho bạn phụ trách."}</p></div>
      {manager && <button className="gr-button gr-button--primary" onClick={() => navigateTo(`${basePath}/new`)}>+ Tạo yêu cầu nhập</button>}
    </header>
    <div className="ib-summary-grid" aria-label="Thống kê yêu cầu">
      <div><span>Tổng yêu cầu</span><strong>{loading ? "—" : requests.length}</strong></div>
      <div><span>{manager || purchasing ? "Chờ xử lý" : "Có thể nhận hàng"}</span><strong>{loading ? "—" : requests.filter(row => manager || purchasing ? row.status === "Pending" : ["Approved", "PartiallyReceived"].includes(row.status)).length}</strong></div>
      <div><span>Đã nhận đủ</span><strong>{loading ? "—" : requests.filter(row => row.status === "Completed").length}</strong></div>
    </div>
    <section className="gr-panel">
      <div className="gr-panel-heading"><h2>Danh sách yêu cầu</h2><button className="gr-button gr-button--quiet" disabled={loading} onClick={() => setReload(value => value + 1)}>Làm mới</button></div>
      <div className="gr-filter-form">
        <label>Tìm mã yêu cầu<input type="search" placeholder="Ví dụ: 12" value={search} onChange={event => setSearch(event.target.value)} /></label>
        <label>Trạng thái<select value={status} onChange={event => setStatus(event.target.value)}><option value="">Tất cả trạng thái</option>{Object.entries(inboundStatuses).filter(([key]) => manager || purchasing || ["Approved", "PartiallyReceived", "Completed"].includes(key)).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
      </div>
      {error && <div className="gr-alert gr-alert--error" role="alert">{error} <button className="gr-text-button" onClick={() => setReload(value => value + 1)}>Thử lại</button></div>}
      {loading ? <div className="gr-empty" role="status">Đang tải yêu cầu nhập…</div> : !error && <>
        {filtered.length === 0 ? <div className="gr-empty"><h2>Chưa có yêu cầu phù hợp</h2><p>Thay đổi điều kiện tìm kiếm hoặc chờ yêu cầu được phân công vào kho của bạn.</p></div> : <div className="gr-table-scroll"><table className="gr-table"><caption className="sr-only">Yêu cầu nhập kho trong phạm vi được phân công</caption><thead><tr><th>Yêu cầu</th><th>Kho nhận</th><th>Nhà cung cấp</th><th>Ngày tạo</th><th>Trạng thái</th><th><span className="sr-only">Thao tác</span></th></tr></thead><tbody>{filtered.map(row => <tr key={row.id}>
          <td><button className="gr-receipt-link" onClick={() => navigateTo(`${basePath}/${row.id}`)}>IN-{String(row.id).padStart(4, "0")}</button></td>
          <td>{lookups?.warehouses.find(x => x.id === row.warehouseId)?.warehouseName || `Kho #${row.warehouseId}`}</td>
          <td>{row.supplierId ? lookups?.suppliers.find(x => x.id === row.supplierId)?.supplierName || `Nhà cung cấp #${row.supplierId}` : "Chờ phân công"}</td>
          <td>{inboundDate(row.createdDate)}</td><td><span className={`ib-status ib-status--${row.status}`}>{inboundStatuses[row.status] || row.status}</span></td>
          <td><button className="gr-text-button" onClick={() => navigateTo(`${basePath}/${row.id}`)}>Xem chi tiết →</button></td>
        </tr>)}</tbody></table></div>}
        <div className="ib-list-footer">Hiển thị {filtered.length} / {requests.length} yêu cầu trong phạm vi kho được gán</div>
      </>}
    </section>
  </div>;
}
