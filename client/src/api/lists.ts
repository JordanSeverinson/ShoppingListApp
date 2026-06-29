import { DEMO_USER_ID } from "../lib/userId";
import type {
  CheckAllItemsResponse,
  CreateItemPayload,
  DeleteItemsResponse,
  ListDetail,
  ListItem,
  ListSummary,
  ListSummaryResponse,
} from "../types/list";

const API_BASE = import.meta.env.VITE_API_URL ?? "";

function apiHeaders(): HeadersInit {
  return {
    "Content-Type": "application/json",
    "X-User-Id": DEMO_USER_ID,
  };
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      ...apiHeaders(),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string } | null;
    throw new Error(body?.error ?? `Request failed (${response.status})`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function fetchMyLists(): Promise<ListSummaryResponse> {
  return request<ListSummaryResponse>("/api/lists");
}

export function createList(name: string): Promise<ListSummary> {
  return request<ListSummary>("/api/lists", {
    method: "POST",
    body: JSON.stringify({ name }),
  });
}

export function joinList(shareCode: string): Promise<ListSummary> {
  return request<ListSummary>("/api/lists/join", {
    method: "POST",
    body: JSON.stringify({ shareCode: shareCode.trim().toUpperCase() }),
  });
}

export function renameList(listId: string, name: string): Promise<ListSummary> {
  return request<ListSummary>(`/api/lists/${listId}`, {
    method: "PATCH",
    body: JSON.stringify({ name }),
  });
}

export function archiveList(listId: string): Promise<ListSummary> {
  return request<ListSummary>(`/api/lists/${listId}/archive`, { method: "POST" });
}

export function fetchList(listId: string): Promise<ListDetail> {
  return request<ListDetail>(`/api/lists/${listId}`);
}

export function createItem(listId: string, payload: CreateItemPayload): Promise<ListItem> {
  return request<ListItem>(`/api/lists/${listId}/items`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function updateItem(
  listId: string,
  itemId: string,
  payload: Partial<Pick<ListItem, "name" | "quantity" | "category" | "isChecked">>,
): Promise<ListItem> {
  return request<ListItem>(`/api/lists/${listId}/items/${itemId}`, {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}

export function deleteItem(listId: string, itemId: string): Promise<void> {
  return request<void>(`/api/lists/${listId}/items/${itemId}`, { method: "DELETE" });
}

export function deleteItems(
  listId: string,
  itemIds: string[],
  options?: { keepalive?: boolean },
): Promise<DeleteItemsResponse> {
  const init: RequestInit = {
    method: "POST",
    body: JSON.stringify({ itemIds }),
  };

  if (options?.keepalive) {
    return keepaliveRequest<DeleteItemsResponse>(
      `/api/lists/${listId}/items/delete-many`,
      init,
    );
  }

  return request<DeleteItemsResponse>(`/api/lists/${listId}/items/delete-many`, init);
}

async function keepaliveRequest<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    keepalive: true,
    headers: {
      ...apiHeaders(),
      ...init.headers,
    },
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string } | null;
    throw new Error(body?.error ?? `Request failed (${response.status})`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function checkAllItems(
  listId: string,
  category?: string,
): Promise<CheckAllItemsResponse> {
  return request<CheckAllItemsResponse>(`/api/lists/${listId}/items/check-all`, {
    method: "POST",
    body: JSON.stringify({ category: category ?? null }),
  });
}

