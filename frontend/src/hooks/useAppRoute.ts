import { useEffect, useState } from "react";
function currentRoute() { return window.location.pathname + window.location.search; }
export default function useAppRoute() {
  const [route, setRoute] = useState(currentRoute);
  useEffect(() => {
    const changed = () => setRoute(currentRoute());
    window.addEventListener("popstate", changed);
    return () => window.removeEventListener("popstate", changed);
  }, []);
  return route;
}
export function navigateTo(path: string, replace = false) {
  if (!path.startsWith("/") || path.startsWith("//")) return;
  if (replace) window.history.replaceState(null, "", path);
  else window.history.pushState(null, "", path);
  window.dispatchEvent(new PopStateEvent("popstate"));
  window.scrollTo(0, 0);
}
