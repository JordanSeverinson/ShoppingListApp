import { DEMO_USER_ID } from "../lib/userId";
import type {
  CreateItemPayload,
  ListDetail,
  ListItem,
  ListSummary,
  ListSummaryResponse,
  UploadImageResponse,
} from "../types/list";

const API_BASE = import.meta.env.VITE_API_URL ?? "";

function readString(raw: Record<string, unknown>, camel: string, pascal: string): string {
  const value = raw[camel] ?? raw[pascal];
  return value == null ? "" : String(value);
}

function readNumber(raw: Record<string, unknown>, camel: string, pascal: string): number {
  const value = raw[camel] ?? raw[pascal];
  return typeof value === "number" ? value : Number(value ?? 0);
}

function readBool(raw: Record<string, unknown>, camel: string, pascal: string): boolean {
  const value = raw[camel] ?? raw[pascal];
  return Boolean(value);
}

function normalizeSummary(raw: Record<string, unknown>): ListSummary {
  return {
    id: readString(raw, "id", "Id"),
    name: readString(raw, "name", "Name"),
    shareCode: readString(raw, "shareCode", "ShareCode"),
    isArchived: readBool(raw, "isArchived", "IsArchived"),
    isOwner: readBool(raw, "isOwner", "IsOwner"),
    updatedAt: (raw.updatedAt ?? raw.UpdatedAt ?? null) as string | null,
    itemCount: readNumber(raw, "itemCount", "ItemCount"),
    checkedCount: readNumber(raw, "checkedCount", "CheckedCount"),
  };
}

function normalizeListSummaryResponse(raw: Record<string, unknown>): ListSummaryResponse {
  const shared = (raw.sharedLists ?? raw.SharedLists ?? []) as unknown[];
  const archived = (
    raw.archivedLists
    ?? []
  ) as unknown[];

  return {
    sharedLists: shared.map((entry) => normalizeSummary(entry as Record<string, unknown>)),
    archivedLists: archived.map((entry) => normalizeSummary(entry as Record<string, unknown>)),
  };
}

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

  const json = (await response.json()) as unknown;
  return json as T;
}

async function requestSummary(path: string, init?: RequestInit): Promise<ListSummary> {
  const json = (await request(path, init)) as Record<string, unknown>;
  return normalizeSummary(json);
}

export async function fetchMyLists(): Promise<ListSummaryResponse> {
  const json = (await request("/api/lists")) as Record<string, unknown>;
  return normalizeListSummaryResponse(json);
}

export function createList(name: string): Promise<ListSummary> {
  return requestSummary("/api/lists", {
    method: "POST",
    body: JSON.stringify({ name }),
  });
}

export function joinList(shareCode: string): Promise<ListSummary> {
  return requestSummary("/api/lists/join", {
    method: "POST",
    body: JSON.stringify({ shareCode: shareCode.trim().toUpperCase() }),
  });
}

export function renameList(listId: string, name: string): Promise<ListSummary> {
  return requestSummary(`/api/lists/${listId}`, {
    method: "PATCH",
    body: JSON.stringify({ name }),
  });
}

export function archiveList(listId: string): Promise<ListSummary> {
  return requestSummary(`/api/lists/${listId}/archive`, { method: "POST" });
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

export async function uploadImage(
  listId: string,
  file: File,
): Promise<UploadImageResponse> {
  const formData = new FormData();
  formData.append("image", file);

  const response = await fetch(`${API_BASE}/api/lists/${listId}/upload-image`, {
    method: "POST",
    headers: { "X-User-Id": DEMO_USER_ID },
    body: formData,
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string } | null;
    throw new Error(body?.error ?? `Upload failed (${response.status})`);
  }

  return (await response.json()) as UploadImageResponse;
}
