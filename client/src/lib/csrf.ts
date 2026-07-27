const API_BASE = import.meta.env.VITE_API_URL ?? "";
const CSRF_COOKIE = "csrf_token";

let memoryToken: string | null = null;

function readCookie(name: string): string | null {
  if (typeof document === "undefined") {
    return null;
  }

  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

/** Current CSRF token for the X-CSRF header (double-submit cookie pattern). */
export function getCsrfToken(): string | null {
  return memoryToken ?? readCookie(CSRF_COOKIE);
}

export function clearCsrfToken(): void {
  memoryToken = null;
}

/**
 * Fetch a fresh CSRF cookie + token from the API.
 * Safe to call while anonymous; required after login when the auth cookie is set.
 */
export async function ensureCsrfToken(): Promise<string> {
  const response = await fetch(`${API_BASE}/api/auth/csrf`, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(`Failed to obtain CSRF token (${response.status})`);
  }

  const body = (await response.json()) as { csrfToken?: string };
  if (!body.csrfToken) {
    throw new Error("CSRF token missing from response.");
  }

  memoryToken = body.csrfToken;
  return memoryToken;
}
