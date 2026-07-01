import { API_BASE, apiRequest, buildAuthHeaders } from "../lib/apiClient";
import type {
  CheckAllItemsResponse,
  CreateItemPayload,
  DeleteItemsResponse,
  ListDetail,
  ListItem,
  ListSummary,
  ListSummaryResponse,
  ShareListResponse,
} from "../types/list";

export function fetchMyLists(): Promise<ListSummaryResponse> {
  return apiRequest<ListSummaryResponse>("/api/lists");
}

export function createList(name: string): Promise<ListSummary> {
  return apiRequest<ListSummary>("/api/lists", {
    method: "POST",
    body: JSON.stringify({ name }),
  });
}

export function shareList(listId: string, friendUserIds: string[]): Promise<ShareListResponse> {
  return apiRequest<ShareListResponse>(`/api/lists/${listId}/shares`, {
    method: "POST",
    body: JSON.stringify({ friendUserIds }),
  });
}

export function acceptListShare(permissionId: string): Promise<ListSummary> {
  return apiRequest<ListSummary>(`/api/lists/shares/${permissionId}/accept`, {
    method: "POST",
  });
}

export function declineListShare(permissionId: string): Promise<void> {
  return apiRequest<void>(`/api/lists/shares/${permissionId}/decline`, {
    method: "POST",
  });
}

export function leaveList(listId: string): Promise<void> {
  return apiRequest<void>(`/api/lists/${listId}/leave`, {
    method: "POST",
  });
}

export function renameList(listId: string, name: string): Promise<ListSummary> {
  return apiRequest<ListSummary>(`/api/lists/${listId}`, {
    method: "PATCH",
    body: JSON.stringify({ name }),
  });
}

export function archiveList(listId: string): Promise<ListSummary> {
  return apiRequest<ListSummary>(`/api/lists/${listId}/archive`, { method: "POST" });
}

export function deleteList(listId: string): Promise<void> {
  return apiRequest<void>(`/api/lists/${listId}`, { method: "DELETE" });
}

export function fetchList(listId: string): Promise<ListDetail> {
  return apiRequest<ListDetail>(`/api/lists/${listId}`);
}

export function createItem(listId: string, payload: CreateItemPayload): Promise<ListItem> {
  return apiRequest<ListItem>(`/api/lists/${listId}/items`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function updateItem(
  listId: string,
  itemId: string,
  payload: Partial<Pick<ListItem, "name" | "quantity" | "category" | "isChecked">>,
): Promise<ListItem> {
  return apiRequest<ListItem>(`/api/lists/${listId}/items/${itemId}`, {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}

export function deleteItem(listId: string, itemId: string): Promise<void> {
  return apiRequest<void>(`/api/lists/${listId}/items/${itemId}`, { method: "DELETE" });
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

  return apiRequest<DeleteItemsResponse>(`/api/lists/${listId}/items/delete-many`, init);
}

async function keepaliveRequest<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    keepalive: true,
    headers: {
      ...buildAuthHeaders(),
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
  return apiRequest<CheckAllItemsResponse>(`/api/lists/${listId}/items/check-all`, {
    method: "POST",
    body: JSON.stringify({ category: category ?? null }),
  });
}
