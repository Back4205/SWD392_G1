import { useEffect, useState } from "react";
import { getInboundLookups, getInboundRequestById, type InboundLookups, type InboundRequestDetail } from "../features/inbound/inbound.api";
import { inboundError } from "../features/inbound/inbound.helpers";

export default function useInboundRequestDetail(requestId: number) {
  const [request, setRequest] = useState<InboundRequestDetail | null>(null);
  const [lookups, setLookups] = useState<InboundLookups | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [refresh, setRefresh] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      setLoading(true); setError("");
      try {
        const [data, options] = await Promise.all([getInboundRequestById(requestId), getInboundLookups()]);
        if (active) { setRequest(data); setLookups(options); }
      } catch (err) { if (active) setError(inboundError(err)); }
      finally { if (active) setLoading(false); }
    }
    void load();
    return () => { active = false; };
  }, [requestId, refresh]);

  function reload() { setRefresh(value => value + 1); }
  return { request, lookups, loading, error, refresh, reload };
}
