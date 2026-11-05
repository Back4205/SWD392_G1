import { apiRequest, resetCsrfToken } from "../../api/http";
import type { AuthUser } from "./auth.types";
export function getCurrentUser() { return apiRequest<AuthUser>("/api/auth/me"); }
export async function login(userName: string, password: string) {
  const result = await apiRequest<{ user: AuthUser; expiresAt: string }>("/api/auth/login", "POST", { userName, password });
  resetCsrfToken(); return result.user;
}
export async function logout() {
  await apiRequest<void>("/api/auth/logout", "POST"); resetCsrfToken();
}
export async function changePassword(currentPassword: string, newPassword: string, confirmPassword: string) {
  await apiRequest<void>("/api/auth/change-password", "POST", { currentPassword, newPassword, confirmPassword });
  resetCsrfToken();
}
export function notifyAuthChanged() { localStorage.setItem("ewms.auth.changed", String(Date.now())); }
