import { ApiError } from "./apiError";

export const API_BASE = import.meta.env.VITE_API_URL ?? "";

let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

function buildJsonHeaders(extra?: HeadersInit): HeadersInit {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (extra) {
    for (const [key, value] of Object.entries(extra)) {
      if (typeof value === "string") {
        headers[key] = value;
      }
    }
  }

  return headers;
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
    body: formData,
  });

  return handleResponse<T>(response);
}
