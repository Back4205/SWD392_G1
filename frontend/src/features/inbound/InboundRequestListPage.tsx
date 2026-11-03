
import { useEffect, useMemo, useState } from "react";
import {
    getInboundRequests,
    type InboundRequestListItem,
} from "./inbound.api";
import "./InboundRequestListPage.css";

type Props = {
    onCreateNew?: () => void;
    onViewDetail?: (requestId: number) => void;
};

export default function InboundRequestListPage({
    onCreateNew,
      onViewDetail,
}: Props) {
    const [requests, setRequests] = useState<InboundRequestListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [search, setSearch] = useState("");
    const [statusFilter, setStatusFilter] = useState("All");

    const loadRequests = async () => {
        setLoading(true);
        setError("");

        try {
            const data = await getInboundRequests();
            setRequests(data);
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : "Failed to load inbound requests."
            );
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void loadRequests();
    }, []);

    const filteredRequests = useMemo(() => {
        return requests.filter((request) => {
            const matchesSearch = String(request.id).includes(search.trim());

            const matchesStatus =
                statusFilter === "All" ||
                request.status.toLowerCase() === statusFilter.toLowerCase();

            return matchesSearch && matchesStatus;
        });
    }, [requests, search, statusFilter]);

    const formatDate = (date: string | null) => {
        if (!date) return "—";

        const parsed = new Date(date);
        if (Number.isNaN(parsed.getTime())) return "—";

        return parsed.toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "short",
            year: "numeric",
        });
    };

    const statusOptions = Array.from(
        new Set(requests.map((request) => request.status))
    );

    return (
        <main className= "inbound-list-page" >
        <div className="inbound-list-container" >
            <header className="inbound-list-header" >
                <div>
                <div className="inbound-list-breadcrumb" >
                    Warehouse Management / Inbound Requests
                        </div>
                        < h1 > Inbound Requests </h1>
                            < p > Manage and track your warehouse inbound requests.</p>
                                </div>

                                < button
    type = "button"
    className = "inbound-list-primary-btn"
    onClick = { onCreateNew }
    disabled = {!onCreateNew
}
          >
    + Create New Request
        </button>
        </header>

        < div className = "inbound-list-stats" >
            <div className="inbound-list-stat-card" >
                <span>Total Requests </span>
                    < strong > { requests.length } </strong>
                    </div>
                    < div className = "inbound-list-stat-card" >
                        <span>Pending Requests </span>
                            <strong>
{ requests.filter((r) => r.status === "Pending").length }
</strong>
    </div>
    < div className = "inbound-list-stat-card" >
        <span>Other Statuses </span>
            <strong>
{ requests.filter((r) => r.status !== "Pending").length }
</strong>
    </div>
    </div>

    < section className = "inbound-list-card" >
        <div className="inbound-list-card-header" >
            <div>
            <h2>All Inbound Requests </h2>
                < p > View and filter inbound request records.</p>
                    </div>
                    < button
type = "button"
className = "inbound-list-refresh-btn"
onClick = {() => void loadRequests()}
disabled = { loading }
    >
    Refresh
    </button>
    </div>

    < div className = "inbound-list-filters" >
        <input
              type="search"
placeholder = "Search by Request ID..."
value = { search }
onChange = {(e) => setSearch(e.target.value)}
            />

    < select
value = { statusFilter }
onChange = {(e) => setStatusFilter(e.target.value)}
title = "Filter by status"
    >
    <option value="All" > All Statuses </option>
{
    statusOptions.map((status) => (
        <option key= { status } value = { status } >
        { status }
        </option>
    ))
}
</select>
    </div>

{
    error && (
        <div className="inbound-list-error" role = "alert" >
        { error }
            </div>
          )
}

<div className="inbound-list-table-wrap" >
    <table className="inbound-list-table" >
        <thead>
        <tr>
        <th>Request ID </th>
            < th > Warehouse </th>
            < th > Requested By </th>
                < th > Supplier </th>
                < th > Created Date </th>
                    < th > Status </th>
                    </tr>
                    </thead>
                    <tbody>
{
    loading ? (
        <tr>
        <td colSpan= { 6} className = "inbound-list-empty" >
            Loading inbound requests...
    </td>
        </tr>
                ) : filteredRequests.length === 0 ? (
        <tr>
        <td colSpan= { 6} className = "inbound-list-empty" >
            No inbound requests found.
                    </td>
                </tr>
                ) : (
        filteredRequests.map((request) => (
            <tr key= { request.id } >
            <td className="inbound-list-request-id" >
        <button
    type="button"
    className = "inbound-list-detail-link"
    onClick = {() => onViewDetail?.(request.id)}
disabled = {!onViewDetail}
title = { "View details of request #" + request.id }
    >
    #{ request.id }
</button>
    </td>
        < td > Warehouse #{ request.warehouseId } </td>
        < td > User #{ request.requestedByUserId } </td>
        <td>
                        {
                request.supplierId
                    ? `Supplier #${request.supplierId}`
                    : "Not assigned"
            }
            </td>
            < td > { formatDate(request.createdDate)} </td>
                < td >
                <span
                          className={
    request.status.toLowerCase() === "pending"
        ? "inbound-list-status pending"
        : "inbound-list-status other"
}
                        >
{ request.status }
    </span>
    </td>
    </tr>
                  ))
                )}
</tbody>
    </table>
    </div>

    < div className = "inbound-list-footer" >
        Showing { filteredRequests.length } of { requests.length } requests
            </div>
            </section>
            </div>
            </main>
  );
}
