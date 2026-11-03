
import { useState } from "react";
import {
    updateSupplierCommitment,
    type UpdateSupplierCommitmentPayload,
} from "./inbound.api";
import "./SupplierCommitmentForm.css";

interface Props {
    requestId: number;
    initialSupplierId: number | null;
    initialStatus: string | null;
    initialDeliveryDate: string | null;
    onUpdated: () => void;
}

export default function SupplierCommitmentForm({
    requestId,
    initialSupplierId,
    initialStatus,
    initialDeliveryDate,
    onUpdated,
}: Props) {
    const [supplierId, setSupplierId] = useState(
        initialSupplierId?.toString() ?? ""
    );

    const [status, setStatus] = useState<"Confirmed" | "Rejected">(
        initialStatus === "Rejected" ? "Rejected" : "Confirmed"
    );

    const [deliveryDate, setDeliveryDate] = useState(
        initialDeliveryDate?.slice(0, 10) ?? ""
    );

    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        setError("");
        setSuccess("");

        const parsedSupplierId = Number(supplierId);

        if (!Number.isInteger(parsedSupplierId) || parsedSupplierId <= 0) {
            setError("Please enter a valid Supplier ID.");
            return;
        }

        if (status === "Confirmed" && !deliveryDate) {
            setError("Expected delivery date is required.");
            return;
        }

        const payload: UpdateSupplierCommitmentPayload = {
            supplierId: parsedSupplierId,
            commitmentStatus: status,
            expectedDeliveryDate:
                status === "Confirmed"
                    ? `${deliveryDate}T00:00:00`
                    : null,
        };

        try {
            setSaving(true);
            await updateSupplierCommitment(requestId, payload);
            setSuccess("Supplier commitment updated successfully.");
            onUpdated();
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : "Failed to update supplier commitment."
            );
        } finally {
            setSaving(false);
        }
    };

    return (
        <section className= "supplier-commitment-card" >
        <div className="supplier-commitment-heading" >
            <div className="supplier-commitment-step" >03 </div>
                < div >
                <h2>Update Supplier Commitment </h2>
                    <p>
            Record the supplier's response and expected delivery date.
        </p>
        </div>
        </div>

        < form onSubmit = { handleSubmit } >
            <div className="supplier-commitment-grid" >
                <div className="supplier-commitment-field" >
                    <label htmlFor="commitment-supplier-id" >
                        Supplier ID
                            </label>
                            < input
    id = "commitment-supplier-id"
    type = "number"
    min = "1"
    step = "1"
    value = { supplierId }
    onChange = {(event) => setSupplierId(event.target.value)
}
placeholder = "Enter Supplier ID"
required
disabled = { saving }
    />
    </div>

    < div className = "supplier-commitment-field" >
        <label htmlFor="commitment-status" >
            Commitment Status
                </label>
                < select
id = "commitment-status"
value = { status }
onChange = {(event) =>
setStatus(event.target.value as "Confirmed" | "Rejected")
              }
disabled = { saving }
    >
    <option value="Confirmed" > Confirmed </option>
        < option value = "Rejected" > Rejected </option>
            </select>
            </div>

{
    status === "Confirmed" && (
        <div className="supplier-commitment-field" >
            <label htmlFor="commitment-delivery-date" >
                Expected Delivery Date
                    </label>
                    < input
    id = "commitment-delivery-date"
    type = "date"
    value = { deliveryDate }
    min = { new Date().toLocaleDateString("en-CA") }
    onChange = {(event) => setDeliveryDate(event.target.value)
}
required
disabled = { saving }
    />
    </div>
          )}
</div>

{
    error && (
        <div className="supplier-commitment-error" role = "alert" >
        { error }
            </div>
        )
}

{
    success && (
        <div className="supplier-commitment-success" role = "status" >
        { success }
            </div>
        )
}

<div className="supplier-commitment-actions" >
    <button type="submit" disabled = { saving } >
    { saving? "Saving...": "Save Commitment" }
        </button>
        </div>
        </form>
        </section>
  );
}
