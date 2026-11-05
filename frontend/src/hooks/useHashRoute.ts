import { useEffect, useState } from "react";

// Hash URL giúp refresh/back giữ đúng màn hình mà chưa cần cài thêm router.
export default function useHashRoute() {
  const [route, setRoute] = useState(window.location.hash.slice(1) || "/goods-receipts");

  useEffect(() => {
    function handleHashChange() {
      setRoute(window.location.hash.slice(1) || "/goods-receipts");
    }
    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);

  return route;
}

export function navigateTo(path: string) {
  window.location.hash = path;
}
