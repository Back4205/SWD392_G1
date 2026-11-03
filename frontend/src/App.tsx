
import { useState } from "react";

import CreateInboundRequestPage from "./features/inbound/CreateInboundRequestPage";
import InboundRequestListPage from "./features/inbound/InboundRequestListPage";
import InboundRequestDetailPage from "./features/inbound/InboundRequestDetailPage";

type InboundPage = "list" | "create" | "detail";

function App() {
    const [currentPage, setCurrentPage] =
        useState<InboundPage>("list");

    const [selectedRequestId, setSelectedRequestId] =
        useState<number | null>(null);

    const goToList = () => {
        setSelectedRequestId(null);
        setCurrentPage("list");
    };

    const goToDetail = (requestId: number) => {
        setSelectedRequestId(requestId);
        setCurrentPage("detail");
    };

    if (currentPage === "create") {
        return (
            <div>
            <button
          type= "button"
        onClick = { goToList }
        style = {{
            margin: "20px 24px",
                padding: "10px 18px",
                    borderRadius: 8,
                        border: "1px solid #d7e3f5",
                            background: "#ffffff",
                                color: "#2563eb",
                                    fontWeight: 700,
                                        cursor: "pointer",
          }
    }
        >
          ← Back to Inbound Requests
        </button>

        < CreateInboundRequestPage />
        </div>
    );
}

if (currentPage === "detail" && selectedRequestId !== null) {
    return (
        <InboundRequestDetailPage
        requestId= { selectedRequestId }
    onBack = { goToList }
        />
    );
}

return (
    <InboundRequestListPage
      onCreateNew= {() => setCurrentPage("create")}
onViewDetail = { goToDetail }
    />
  );
}

export default App;
