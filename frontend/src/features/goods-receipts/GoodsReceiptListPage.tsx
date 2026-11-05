import { useEffect, useState } from "react";
import { getGoodsReceipts } from "./goodsReceipt.api";
import type { ReceiptList, ReceiptSession, ReceiptStatus } from "./goodsReceipt.types";
import { formatReceiptDate, receiptErrorMessage, statusLabels } from "./receipt.helpers";
import ReceiptStatusBadge from "./ReceiptStatusBadge";
import { navigateTo } from "../../hooks/useHashRoute";

interface Props { session: ReceiptSession; query: string; }

export default function GoodsReceiptListPage({ session, query }: Props) {
  const params = new URLSearchParams(query);
  const [status, setStatus] = useState(params.get("status") || "");
  const [warehouse, setWarehouse] = useState(params.get("warehouseId") || "");
  const [fromDate, setFromDate] = useState(params.get("fromDate")?.slice(0, 10) || "");
  const [toDate, setToDate] = useState(params.get("toDate")?.slice(0, 10) || "");
  const [data, setData] = useState<ReceiptList | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    async function loadList() {
      setLoading(true);
      setError("");
      try {
        const result = await getGoodsReceipts(query);
        if (active) setData(result);
      } catch (err) {
        if (active) setError(receiptErrorMessage(err));
      } finally {
        if (active) setLoading(false);
      }
    }
    void loadList();
    return () => { active = false; };
  }, [query, reloadKey]);

  function applyFilters() {
    const next = new URLSearchParams();
    if (status) next.set("status", status);
    if (warehouse) next.set("warehouseId", warehouse);
    // URL giữ ngày người dùng chọn; API client sẽ đổi đầu/cuối ngày địa phương sang UTC.
    if (fromDate) next.set("fromDate", fromDate);
    if (toDate) next.set("toDate", toDate);
    next.set("page", "1");
    navigateTo(`/goods-receipts?${next}`);
  }

  function changePage(page: number) {
    const next = new URLSearchParams(query);
    next.set("page", String(page));
    navigateTo(`/goods-receipts?${next}`);
  }

  return <>
    <header className="gr-page-heading">
      <div><p className="gr-eyebrow">NHẬP KHO · GOODS RECEIPT</p><h1>Phiếu nhận hàng</h1>
        <p>Theo dõi hàng thực nhận, serial và quyết định duyệt của từng phiếu.</p></div>
      {session.role === "Warehouse Staff" && <button className="gr-button gr-button--primary" onClick={() => navigateTo("/goods-receipts/new")}>+ Tạo phiếu nhận</button>}
    </header>

    <div className="gr-workflow-note"><span className="gr-note-dot" /><p>Nhân viên nhận hàng và gửi duyệt. <strong>Manager duyệt mới cập nhật tồn kho.</strong></p></div>

    <section className="gr-panel">
      <div className="gr-panel-heading">
        <div className="gr-tabs" aria-label="Nhóm phiếu">
          <button className={!params.get("status") ? "active" : ""} onClick={() => navigateTo("/goods-receipts")}>Tất cả phiếu</button>
          <button className={params.get("status") === "PendingApproval" ? "active" : ""} onClick={() => navigateTo("/goods-receipts?status=PendingApproval")}>Chờ duyệt</button>
        </div>
        <button className="gr-button gr-button--quiet" disabled={loading} onClick={() => setReloadKey(value => value + 1)}>Làm mới</button>
      </div>

      <form className="gr-filter-form" onSubmit={event => { event.preventDefault(); applyFilters(); }}>
        <label>Trạng thái<select value={status} onChange={event => setStatus(event.target.value)}>
          <option value="">Tất cả trạng thái</option>
          {Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select></label>
        <label>Kho<select value={warehouse} onChange={event => setWarehouse(event.target.value)}>
          <option value="">Tất cả kho được gán</option>
          {session.warehouseIds.map(id => <option key={id} value={id}>Kho #{id}</option>)}
        </select></label>
        <label>Từ ngày<input type="date" value={fromDate} onChange={event => setFromDate(event.target.value)} /></label>
        <label>Đến ngày<input type="date" value={toDate} min={fromDate || undefined} onChange={event => setToDate(event.target.value)} /></label>
        <button className="gr-button gr-button--secondary" type="submit">Lọc phiếu</button>
      </form>

      {error && <div className="gr-alert gr-alert--error" role="alert">{error} <button className="gr-text-button" onClick={() => setReloadKey(value => value + 1)}>Thử lại</button></div>}

      {loading ? <div className="gr-empty" role="status">Đang tải danh sách phiếu…</div> : !error && data && <>
        {data.items.length === 0 ? <div className="gr-empty"><span className="gr-empty-symbol">GR</span><h2>Chưa có phiếu trong danh sách này</h2><p>Thay đổi bộ lọc hoặc tạo phiếu từ một yêu cầu nhập còn thiếu hàng.</p>
          {session.role === "Warehouse Staff" && <button className="gr-button gr-button--primary" onClick={() => navigateTo("/goods-receipts/new")}>Tạo phiếu đầu tiên</button>}</div>
          : <div className="gr-table-scroll"><table className="gr-table">
            <caption className="sr-only">Danh sách phiếu nhận hàng theo quyền kho</caption>
            <thead><tr><th>Phiếu nhận</th><th>Yêu cầu nhập</th><th>Kho</th><th>Ngày nhận</th><th>Trạng thái</th><th><span className="sr-only">Thao tác</span></th></tr></thead>
            <tbody>{data.items.map(receipt => <tr key={receipt.id}>
              <td><button className="gr-receipt-link" onClick={() => navigateTo(`/goods-receipts/${receipt.id}`)}>GR-{String(receipt.id).padStart(4, "0")}</button></td>
              <td>Inbound #{receipt.inboundRequestId}</td><td>Kho #{receipt.warehouseId}</td>
              <td className="gr-date">{formatReceiptDate(receipt.receivedDate)}</td>
              <td><ReceiptStatusBadge status={receipt.status as ReceiptStatus} /></td>
              <td><button className="gr-text-button" aria-label={`Xem phiếu ${receipt.id}`} onClick={() => navigateTo(`/goods-receipts/${receipt.id}`)}>Xem phiếu →</button></td>
            </tr>)}</tbody>
          </table></div>}
        <div className="gr-pagination"><span>{data.totalCount} phiếu · Trang {data.page}/{Math.max(1, Math.ceil(data.totalCount / data.pageSize))}</span>
          <div><button className="gr-button gr-button--quiet" disabled={data.page <= 1} onClick={() => changePage(data.page - 1)}>← Trước</button>
            <button className="gr-button gr-button--quiet" disabled={data.page * data.pageSize >= data.totalCount} onClick={() => changePage(data.page + 1)}>Sau →</button></div>
        </div>
      </>}
    </section>
  </>;
}
