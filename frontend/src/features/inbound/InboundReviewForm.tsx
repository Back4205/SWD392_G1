
import { useState, type FormEvent } from "react";
import { reviewInboundRequest } from "./inbound.api";
import "./InboundReviewForm.css";

interface InboundReviewFormProps {
    requestId: number;
    onReviewed: () => void;
}

export default function InboundReviewForm({
    requestId,
    onReviewed,
}: InboundReviewFormProps) {
    const [showRejectForm, setShowRejectForm] = useState(false);
    const [rejectionReason, setRejectionReason] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const handleApprove = async () => {
        if (!window.confirm("Approve this inbound request?")) {
            return;
        }

        setLoading(true);
        setError("");

        try {
            await reviewInboundRequest(requestId, {
                decision: "Approved",
                rejectionReason: null,
            });

            onReviewed();
        } catch (err) {
            setError(
                err instanceof Error ? err.message : "Failed to approve request."
            );
        } finally {
            setLoading(false);
        }
    };

    const handleReject = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        if (!rejectionReason.trim()) {
            setError("Rejection reason is required.");
            return;
        }

        setLoading(true);
        setError("");

        try {
            await reviewInboundRequest(requestId, {
                decision: "Rejected",
                rejectionReason: rejectionReason.trim(),
            });

            onReviewed();
        } catch (err) {
            setError(
                err instanceof Error ? err.message : "Failed to reject request."
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <section className= "inbound-review-card" >
        <div className="inbound-review-header" >
            <div>
            <span className="inbound-review-label" >04 MANAGER REVIEW </span>
                < h2 > Approve or Reject Request </h2>
                    < p > Review the inbound request before making a decision.</p>
                        </div>
                        </div>

    { error && <div className="inbound-review-error" > { error } </div> }

    {
        !showRejectForm ? (
            <div className= "inbound-review-actions" >
            <button
            type="button"
        className = "inbound-review-approve"
        onClick = { handleApprove }
        disabled = { loading }
            >
        { loading? "Processing...": "✓ Approve Request" }
            </button>

            < button
        type = "button"
        className = "inbound-review-reject"
        onClick = {() => {
            setError("");
            setShowRejectForm(true);
        }
    }
    disabled = { loading }
        >
            ✕ Reject Request
        </button>
        </div>
      ) : (
        <form onSubmit= { handleReject } className = "inbound-review-form" >
            <label htmlFor="rejection-reason" > Rejection Reason </label>

                < textarea
    id = "rejection-reason"
    value = { rejectionReason }
    onChange = {(event) => setRejectionReason(event.target.value)
}
placeholder = "Enter the reason for rejecting this request..."
rows = { 4}
disabled = { loading }
    />

    <div className="inbound-review-actions" >
        <button
              type="submit"
className = "inbound-review-reject"
disabled = { loading }
    >
{ loading? "Processing...": "Confirm Rejection" }
    </button>

    < button
type = "button"
className = "inbound-review-cancel"
onClick = {() => {
    setShowRejectForm(false);
    setError("");
}}
disabled = { loading }
    >
    Cancel
    </button>
    </div>
    </form>
      )}
</section>
  );
}
