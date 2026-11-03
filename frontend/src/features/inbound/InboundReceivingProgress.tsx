import "./InboundReceivingProgress.css";
import { useEffect, useState } from "react";
import {
    getInboundReceivingProgress,
    type InboundReceivingProgress as ReceivingProgressData,
} from "./inbound.api";

interface Props {
    requestId: number;
}

export default function InboundReceivingProgress({
    requestId,
}: Props) {
    const [progress, setProgress] =
        useState<ReceivingProgressData | null>(null);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let active = true;

        async function loadProgress() {
            setLoading(true);
            setError(null);
            setProgress(null);

            try {
                const data = await getInboundReceivingProgress(requestId);

                if (active) {
                    setProgress(data);
                }
            } catch (err) {
                if (active) {
                    setError(
                        err instanceof Error
                            ? err.message
                            : "Failed to load receiving progress."
                    );
                }
            } finally {
                if (active) {
                    setLoading(false);
                }
            }
        }

        void loadProgress();

        return () => {
            active = false;
        };
    }, [requestId]);

    return (
        <section className= "inbound-progress" >
        <div className="inbound-progress__header" >
            <div>
            <span className="inbound-progress__eyebrow" >
            05 / RECEIVING PROGRESS
        </span>
        < h2 > Receiving Progress </h2>
            < p > Track expected and received quantities.</p>
                </div>
                </div>

    {
        loading && (
            <p className="inbound-progress__message" >
                Loading receiving progress...
        </p>
      )
    }

    {
        error && (
            <p className="inbound-progress__error" role = "alert" >
            { error }
                </p>
      )
    }

    {
        !loading && !error && progress && (
            <>
            <div className="inbound-progress__stats" >
                <div>
                <span>Expected Quantity </span>
                    < strong > { progress.totalExpectedQty } </strong>
                    </div>

                    < div >
                    <span>Received Quantity </span>
                        <strong>
        { progress.totalReceivedQty ?? "N/A" }
        </strong>
            </div>

            < div >
            <span>Remaining Quantity </span>
                <strong>
        { progress.remainingQty ?? "N/A" }
        </strong>
            </div>

            < div >
            <span>Completion </span>
            <strong>
        {
            progress.progressPercentage === null
            ? "N/A"
            : `${progress.progressPercentage.toFixed(1)}%`
        }
        </strong>
            </div>
            </div>

        {
            progress.progressStatus === "NotAvailable" && (
                <div className="inbound-progress__notice" >
                    Receiving data is not available yet.
              Goods Receipt integration is pending.
            </div>
          )
        }

        <div className="inbound-progress__table-wrapper" >
            <table className="inbound-progress__table" >
                <thead>
                <tr>
                <th>Product Variant </th>
                    < th > Expected </th>
                    < th > Received </th>
                    < th > Remaining </th>
                    </tr>
                    </thead>

                    <tbody>
        {
            progress.items.map((item) => (
                <tr key= { item.productVariantId } >
                <td>Variant #{ item.productVariantId } </td>
                < td > { item.expectedQty } </td>
                < td > { item.receivedQty ?? "N/A" } </td>
                < td > { item.remainingQty ?? "N/A" } </td>
            </tr>
            ))
        }

        {
            progress.items.length === 0 && (
                <tr>
                <td colSpan={ 4 }>
                    No inbound request items found.
                    </td>
                        </tr>
                )
        }
        </tbody>
            </table>
            </div>
            </>
      )
    }
    </section>
  );
}
