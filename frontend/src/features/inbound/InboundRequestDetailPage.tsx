import InboundReceivingProgress from "./InboundReceivingProgress";
import { useEffect, useState } from "react";
import {
    getInboundRequestById,
    type InboundRequestDetail,
} from "./inbound.api";

import SupplierCommitmentForm from "./SupplierCommitmentForm";
import InboundReviewForm from "./InboundReviewForm";

import "./InboundRequestDetailPage.css";

interface Props {
    requestId: number;
    onBack: () => void;
}

export default function InboundRequestDetailPage({
    requestId,
    onBack,
}: Props) {
    const [request, setRequest] = useState<InboundRequestDetail | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [refreshKey, setRefreshKey] = useState(0);

    // Refresh after Supplier Commitment update
    const handleCommitmentUpdated = () => {
        setRefreshKey((prev) => prev + 1);
    };

    // Refresh after Manager Approve / Reject
    const handleRequestReviewed = () => {
        setRefreshKey((prev) => prev + 1);
    };

    useEffect(() => {
        let active = true;

        const loadDetail = async () => {
            setLoading(true);
            setError("");
            setRequest(null);

            try {
                const data = await getInboundRequestById(requestId);

                if (active) {
                    setRequest(data);
                }
            } catch (err) {
                if (active) {
                    setError(
                        err instanceof Error
                            ? err.message
                            : "Failed to load inbound request."
                    );
                }
            } finally {
                if (active) {
                    setLoading(false);
                }
            }
        };

        void loadDetail();

        return () => {
            active = false;
        };
    }, [requestId, refreshKey]);

    const formatDate = (value: string | null) => {
        if (!value) return "Not set";

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return "Invalid date";
        }

        return date.toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "short",
            year: "numeric",
        });
    };

    const totalQuantity =
        request?.items.reduce(
            (sum, item) => sum + item.expectedQty,
            0
        ) ?? 0;

    return (
        <main className= "inbound-detail-page" >
        <div className="inbound-detail-container" >
        {/* Header */ }
            < div className = "inbound-detail-topbar" >
                <div>
                <p className="inbound-detail-breadcrumb" >
                    Warehouse Management / Inbound Requests / Detail
                        </p>

                        <h1>
              Inbound Request{ " " }
    { request ? `#${request.id}` : `#${requestId}` }
    </h1>

        < p className = "inbound-detail-subtitle" >
            View inbound request information and product details.
            </p>
                </div>

                < button
    type = "button"
    className = "inbound-detail-back-btn"
    onClick = { onBack }
        >
            ← Back to List
        </button>
        </div>

    {/* Loading */ }
    {
        loading && (
            <div className="inbound-detail-message" >
                Loading inbound request...
        </div>
        )
    }

    {/* Error */ }
    {
        error && (
            <div className="inbound-detail-error" role = "alert" >
            { error }
                </div>
        )
    }

    {/* Detail Content */ }
    {
        !loading && request && (
            <>
            {/* Summary Cards */ }
            < div className = "inbound-detail-summary" >
                <div className="inbound-detail-summary-card" >
                    <span>Request ID </span>
                        < strong >#{ request.id } </strong>
                            </div>

                            < div className = "inbound-detail-summary-card" >
                                <span>Total Products </span>
                                    < strong > { request.items.length } </strong>
                                    </div>

                                    < div className = "inbound-detail-summary-card" >
                                        <span>Total Expected Quantity </span>
                                            < strong > { totalQuantity } </strong>
                                            </div>
                                            </div>

        {/* Section 01: Request Information */ }
        <section className="inbound-detail-card" >
            <div className="inbound-detail-card-heading" >
                <div className="inbound-detail-step" >01 </div>

                    < div >
                    <h2>Request Information </h2>
                        <p>
                    General information about this inbound request.
                  </p>
            </div>

            < span
        className = {`inbound-detail-status ${request.status.toLowerCase() === "pending"
                ? "pending"
                : "other"
            }`
    }
                >
    { request.status }
        </span>
        </div>

        < div className = "inbound-detail-info-grid" >
            <div>
            <span>Warehouse </span>
            < strong > Warehouse #{ request.warehouseId } </strong>
                </div>

                < div >
                <span>Requested By </span>
                    < strong > User #{ request.requestedByUserId } </strong>
                        </div>

                        < div >
                        <span>Supplier </span>
                        <strong>
    {
        request.supplierId != null
        ? `Supplier #${request.supplierId}`
        : "Not assigned"
    }
    </strong>
        </div>

        < div >
        <span>Approved By </span>
            <strong>
    {
        request.approvedByUserId != null
        ? `User #${request.approvedByUserId}`
        : "Not approved"
    }
    </strong>
        </div>

        < div >
        <span>Created Date </span>
            < strong > { formatDate(request.createdDate) } </strong>
            </div>

            < div >
            <span>Expected Delivery Date </span>
                <strong>
    { formatDate(request.expectedDeliveryDate) }
    </strong>
        </div>

        < div >
        <span>Supplier Commitment </span>
            <strong>
    { request.supplierCommitmentStatus ?? "Not set" }
    </strong>
        </div>

        < div >
        <span>Rejection Reason </span>
            <strong>
    { request.rejectionReason ?? "None" }
    </strong>
        </div>
        </div>
        </section>

    {/* Section 02: Product Details */ }
    <section className="inbound-detail-card" >
        <div className="inbound-detail-card-heading" >
            <div className="inbound-detail-step" >02 </div>

                < div >
                <h2>Product Details </h2>
                    <p>
                    Products included in this inbound request.
                  </p>
        </div>
        </div>

        < div className = "inbound-detail-table-wrap" >
            <table className="inbound-detail-table" >
                <thead>
                <tr>
                <th># </th>
                < th > Item ID </th>
                    < th > Product Variant ID </th>
                        < th > Expected Quantity </th>
                            </tr>
                            </thead>

                            <tbody>
    {
        request.items.length === 0 ? (
            <tr>
            <td
                          colSpan= { 4}
                          className = "inbound-detail-empty"
            >
            No products found.
                        </td>
                </tr>
                    ) : (
            request.items.map((item, index) => (
                <tr key= { item.id } >
                <td>
                { String(index + 1).padStart(2, "0")}
    </td>

        < td >#{ item.id } </td>

            < td className = "inbound-detail-product-id" >
                Product Variant #{ item.productVariantId }
    </td>

        < td > { item.expectedQty } </td>
        </tr>
                      ))
                    )
}
</tbody>
    </table>
    </div>
    </section>

{/* Section 03: Supplier Commitment */ }
{
    request.status === "Pending" && (
        <SupplierCommitmentForm
                key={ request.id }
    requestId = { request.id }
    initialSupplierId = { request.supplierId }
    initialStatus = { request.supplierCommitmentStatus }
    initialDeliveryDate = { request.expectedDeliveryDate }
    onUpdated = { handleCommitmentUpdated }
        />
            )
}
<InboundReceivingProgress requestId={ request.id } />

{/* Section 04: Manager Review */ }
{
    request.status === "Pending" && (
        <InboundReviewForm
                requestId={ request.id }
    onReviewed = { handleRequestReviewed }
        />
            )
}
</>
        )}
</div>
    </main>
  );
}
