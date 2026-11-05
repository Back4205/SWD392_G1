import { useState } from "react";
import type { InboundRequestDetail } from "./inbound.api";

interface Props { request: InboundRequestDetail; }

export default function SupplierManifestPanel({ request }: Props) {
  const [search, setSearch] = useState("");
  const query = search.trim().toUpperCase();
  const total = request.items.reduce((sum, item) => sum + (item.expectedSerials?.length || 0), 0);
  const pending = request.status === "Pending";

  return <section className="gr-panel ib-section" aria-labelledby="ib-manifest-title">
    <div className="ib-section-heading"><div><p className="gr-eyebrow">THÔNG TIN NHÀ CUNG CẤP XÁC NHẬN</p><h2 id="ib-manifest-title">Bảng kê SKU & Serial / IMEI</h2><p className="ib-muted">Danh sách thiết bị dự kiến dùng để đối chiếu với hàng thực nhận.</p></div>{request.hasSupplierManifest && <span className="ib-manifest-count complete">{total} serial đã khai báo</span>}</div>
    {!request.hasSupplierManifest ? <div className="gr-alert gr-alert--info">
      {pending ? "Purchasing cần xác nhận nhà cung cấp và nhập đủ serial cho từng SKU trước khi Manager duyệt yêu cầu." : "Yêu cầu này chưa có bảng kê serial của nhà cung cấp. Với yêu cầu đã duyệt từ trước, hệ thống không tự suy diễn serial dự kiến từ hàng thực nhận."}
    </div> : <>
      <div className="ib-manifest-readonly-note">{pending ? "Purchasing có thể cập nhật khi yêu cầu còn chờ xử lý." : "Bảng kê đã khóa sau khi yêu cầu được xử lý."} Serial trong bảng kê chưa đồng nghĩa với thiết bị đã nhập kho.</div>
      <label className="ib-field ib-manifest-search">Tìm SKU hoặc serial<input type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Nhập mã cần đối chiếu…" /></label>
      <div className="ib-manifest-groups">{request.items.map(item => {
        const serials = (item.expectedSerials || []).filter(serial => !query || item.sku.toUpperCase().includes(query) || serial.toUpperCase().includes(query));
        if (query && serials.length === 0) return null;
        return <details key={item.id} className="ib-manifest-group" open={query ? true : undefined}>
          <summary><span><strong>{item.sku}</strong><small>{item.productName}</small></span><span>{serials.length} / {item.expectedQty} mã</span></summary>
          {serials.length === 0 ? <p className="ib-muted">Chưa có serial được khai báo cho SKU này.</p> : <ul className="ib-manifest-serials">{serials.map(serial => <li key={serial}>{serial}</li>)}</ul>}
        </details>;
      })}</div>
      {query && !request.items.some(item => item.sku.toUpperCase().includes(query) || (item.expectedSerials || []).some(serial => serial.toUpperCase().includes(query))) && <p className="ib-muted" role="status">Không tìm thấy SKU hoặc serial phù hợp.</p>}
    </>}
  </section>;
}
