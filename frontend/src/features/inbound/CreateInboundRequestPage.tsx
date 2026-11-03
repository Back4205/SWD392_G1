
import { useState, type FormEvent } from "react";
import { createInboundRequest } from "./inbound.api";
import type { CreateInboundRequestItem } from "./inbound.types";
import "./CreateInboundRequestPage.css";

export default function CreateInboundRequestPage() {
    const [warehouseId, setWarehouseId] = useState("");
    const [items, setItems] = useState<CreateInboundRequestItem[]>([
        { productVariantId: 0, expectedQty: 1 },
    ]);
    const [loading, setLoading] = useState(false);
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");

    const updateItem = (
        index: number,
        field: keyof CreateInboundRequestItem,
        value: number
    ) => {
        setItems((current) =>
            current.map((item, i) =>
                i === index ? { ...item, [field]: value } : item
            )
        );
    };

    const addItem = () => {
        setItems((current) => [
            ...current,
            { productVariantId: 0, expectedQty: 1 },
        ]);
    };

    const removeItem = (index: number) => {
        setItems((current) => current.filter((_, i) => i !== index));
    };

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setMessage("");
        setError("");

        const warehouse = Number(warehouseId);

        if (!Number.isInteger(warehouse) || warehouse <= 0) {
            setError("Please enter a valid Warehouse ID.");
            return;
        }

        if (
            items.length === 0 ||
            items.some(
                (item) =>
                    !Number.isInteger(item.productVariantId) ||
                    item.productVariantId <= 0 ||
                    !Number.isInteger(item.expectedQty) ||
                    item.expectedQty <= 0
            )
        ) {
            setError("Please enter valid product IDs and quantities.");
            return;
        }

        const productIds = items.map((item) => item.productVariantId);

        if (new Set(productIds).size !== productIds.length) {
            setError("Duplicate ProductVariant is not allowed.");
            return;
        }

        setLoading(true);

        try {
            const result = await createInboundRequest({
                warehouseId: warehouse,
                items,
            });

            setMessage(
                `Inbound Request #${result.id} created successfully.`
            );

            setWarehouseId("");
            setItems([{ productVariantId: 0, expectedQty: 1 }]);
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : "Failed to create inbound request."
            );
        } finally {
            setLoading(false);
        }
    };

    const totalQuantity = items.reduce(
        (sum, item) => sum + (item.expectedQty || 0),
        0
    );

    return (
        <main className= "inbound-page" >
        <div className="inbound-container" >
            <header className="inbound-header" >
                <div>
                <div className="inbound-breadcrumb" >
                    Warehouse Management / Inbound Requests
                        </div>

                        < h1 > Create Inbound Request </h1>

                            <p>
              Create a new request to receive products into your warehouse.
            </p>
        </div>

        < span className = "inbound-status" > New Request </span>
            </header>

            < form onSubmit = { handleSubmit } >
                <section className="inbound-card" >
                    <div className="inbound-card-heading" >
                        <div className="inbound-section-icon" >01 </div>

                            < div >
                            <h2>Warehouse Information </h2>
                                < p > Select the destination warehouse.</p>
                                    </div>
                                    </div>

                                    < div className = "inbound-field" >
                                        <label htmlFor="warehouseId" >
                                            Warehouse ID
                                                </label>

                                                < input
    id = "warehouseId"
    type = "number"
    min = "1"
    step = "1"
    placeholder = "Enter warehouse ID"
    value = { warehouseId }
    onChange = {(e) =>
    setWarehouseId(e.target.value)
}
required
    />

    <small>
    Enter the ID of an existing warehouse.
              </small>
        </div>
        </section>

        < section className = "inbound-card" >
            <div className="inbound-products-heading" >
                <div className="inbound-card-heading" >
                    <div className="inbound-section-icon" >02 </div>

                        < div >
                        <h2>Product Details </h2>
                            <p>
                    Add the products and expected quantities.
                  </p>
    </div>
    </div>

    < button
type = "button"
className = "inbound-btn inbound-btn-outline"
onClick = { addItem }
disabled = { loading }
    >
    + Add Product
        </button>
        </div>

        < div className = "inbound-table-wrap" >
            <table className="inbound-table" >
                <thead>
                <tr>
                <th># </th>
                < th > Product Variant ID </th>
                    < th > Expected Quantity </th>
                        < th > Action </th>
                        </tr>
                        </thead>

                        <tbody>
{
    items.map((item, index) => (
        <tr key= { index } >
        <td className="inbound-row-number" >
    { String(index + 1).padStart(2, "0")
}
</td>

    < td >
    <input
                          type="number"
min = "1"
step = "1"
placeholder = "Product ID"
title = { "Product Variant ID row " + (index + 1) }
value = { item.productVariantId || "" }
onChange = {(e) =>
updateItem(
    index,
    "productVariantId",
    Number(e.target.value)
)
                          }
required
    />
    </td>

    < td >
    <input
                          type="number"
min = "1"
step = "1"
title = { "Expected Quantity row " + (index + 1) }
value = { item.expectedQty }
onChange = {(e) =>
updateItem(
    index,
    "expectedQty",
    Number(e.target.value)
)
                          }
required
    />
    </td>

    < td >
    <button
                          type="button"
className = "inbound-remove"
onClick = {() => removeItem(index)}
disabled = { items.length === 1 || loading }
    >
    Remove
    </button>
    </td>
    </tr>
                  ))}
</tbody>
    </table>
    </div>

    < div className = "inbound-summary" >
        <div>
        <span>Total Products </span>
            < strong > { items.length } </strong>
            </div>

            < div >
            <span>Total Expected Quantity </span>
                < strong > { totalQuantity } </strong>
                </div>
                </div>
                </section>

{
    message && (
        <div
              className="inbound-alert inbound-alert-success"
    role = "status"
        >
              ✓ { message }
    </div>
          )
}

{
    error && (
        <div
              className="inbound-alert inbound-alert-error"
    role = "alert"
        >
    { error }
        </div>
          )
}

<div className="inbound-actions" >
    <span>
    Review the information before submitting.
            </span>

        < button
type = "submit"
className = "inbound-btn inbound-btn-primary"
disabled = { loading }
    >
{
    loading
    ? "Creating..."
        : "Create Inbound Request"
}
    </button>
    </div>
    </form>
    </div>
    </main>
  );
}
