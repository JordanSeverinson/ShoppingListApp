import { ApiError } from "./apiError";
import { getCsrfToken } from "./csrf";

export const API_BASE = import.meta.env.VITE_API_URL ?? "";

export const CSRF_HEADER_NAME = "X-CSRF";

let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

function mergeHeaders(base: Record<string, string>, extra?: HeadersInit): HeadersInit {
  const headers: Record<string, string> = { ...base };

  const csrf = getCsrfToken();
  if (csrf) {
    headers[CSRF_HEADER_NAME] = csrf;
  }

  if (extra) {
    for (const [key, value] of Object.entries(extra)) {
      if (typeof value === "string") {
        headers[key] = value;
      }
    }
  }

  return headers;
}

function buildJsonHeaders(extra?: HeadersInit): HeadersInit {
  return mergeHeaders({ "Content-Type": "application/json" }, extra);
}

function buildCsrfHeaders(extra?: HeadersInit): HeadersInit {
  return mergeHeaders({}, extra);
}

export async function handleResponse<T>(
  response: Response,
  options?: { skipUnauthorizedHandler?: boolean },
): Promise<T> {
  if (response.status === 401 && !options?.skipUnauthorizedHandler) {
    unauthorizedHandler?.();
  }

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string; code?: string } | null;
    throw new ApiError(
      body?.error ?? `Request failed (${response.status})`,
      response.status,
      body?.code,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function apiRequest<T>(
  path: string,
  init?: RequestInit,
  options?: { skipUnauthorizedHandler?: boolean },
): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    credentials: "include",
    headers: {
      ...buildJsonHeaders(),
      ...init?.headers,
    },
  });

  return handleResponse<T>(response, options);
}

export async function apiKeepaliveRequest<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    keepalive: true,
    credentials: "include",
    headers: {
      ...buildJsonHeaders(),
      ...init.headers,
    },
  });

  return handleResponse<T>(response);
}

export async function apiFormRequest<T>(
  path: string,
  formData: FormData,
  init?: Omit<RequestInit, "body" | "headers">,
): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    method: init?.method ?? "POST",
    credentials: "include",
    headers: buildCsrfHeaders(),
    body: formData,
  });

  return handleResponse<T>(response);
}
