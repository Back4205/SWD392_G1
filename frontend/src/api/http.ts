// Cookie HttpOnly được trình duyệt gửi tự động; JavaScript không lưu JWT.
export class ApiError extends Error {
  status: number;
  code: string;
  constructor(status: number, code: string, message: string) {
    super(message); this.status = status; this.code = code;
  }
}
let csrfToken: string | null = null;
export function resetCsrfToken() { csrfToken = null; }
async function getCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken;
  const response = await fetch("/api/auth/csrf", { credentials: "include" });
  if (!response.ok) throw new ApiError(response.status, "CSRF", "Không lấy được mã bảo vệ. Hãy tải lại trang.");
  const data = await response.json();
  csrfToken = data.requestToken;
  return data.requestToken;
}
export async function apiRequest<T>(path: string, method = "GET", body?: object): Promise<T> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (body) headers["Content-Type"] = "application/json";
  try {
    if (!["GET", "HEAD"].includes(method)) headers["X-CSRF-TOKEN"] = await getCsrfToken();
    const response = await fetch(path, { method, headers, credentials: "include", body: body ? JSON.stringify(body) : undefined });
    const data = response.status === 204 ? null : await response.json().catch(() => null);
    if (!response.ok) {
      if (response.status === 401 && !["/api/auth/login", "/api/auth/me"].includes(path)) {
        resetCsrfToken(); window.dispatchEvent(new Event("ewms:session-expired"));
      }
      const errors = data?.errors as Record<string, string[]> | undefined;
      const authMessages: Record<string, string> = {
        AUTH_REQUIRED: "Phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại.",
        AUTH_FORBIDDEN: "Tài khoản của bạn không có quyền thực hiện thao tác này.",
        AUTH_CURRENT_PASSWORD: "Mật khẩu hiện tại chưa đúng.",
        AUTH_PASSWORD_POLICY: "Mật khẩu mới phải có 12–128 ký tự, khác mật khẩu cũ và khớp phần xác nhận.",
      };
      const message = authMessages[data?.code] || data?.message || (errors ? Object.values(errors).flat().join(" ") : data?.title);
      throw new ApiError(response.status, data?.code || "HTTP_ERROR", message || `Yêu cầu thất bại (${response.status}).`);
    }
    return data as T;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    throw new ApiError(0, "NETWORK", "Không kết nối được máy chủ. Kiểm tra kết nối rồi thử lại.");
  }
}
