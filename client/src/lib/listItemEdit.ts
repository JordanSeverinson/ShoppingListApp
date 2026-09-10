import { isApiError } from "./apiError";
import type { ListItem } from "../types/list";

const ITEM_CONFLICT_CODE = "ITEM_CONFLICT";

export function isItemConflict(
  error: unknown,
): error is { body: { item: ListItem } } {
  if (!isApiError(error) || error.code !== ITEM_CONFLICT_CODE) {
    return false;
  }

  const item = error.body?.item;
  return Boolean(item && typeof item === "object");
}

export function conflictItem(error: unknown): ListItem | null {
  return isItemConflict(error) ? error.body.item : null;
}

type ItemEditFields = {
  name: string;
  quantity: string;
  category: string;
};

export function itemToEditFields(
  item: Pick<ListItem, "name" | "quantity" | "category">,
): ItemEditFields {
  return {
    name: item.name,
    quantity: item.quantity ?? "",
    category: item.category,
  };
}

export function editFieldsToBase(fields: ItemEditFields): {
  name: string;
  quantity: string | null;
  category: string;
} {
  return {
    name: fields.name,
    quantity: fields.quantity.trim() || null,
    category: fields.category,
  };
}

export function mergeRemoteEdit(
  draft: ItemEditFields,
  baseline: ItemEditFields,
  remote: ItemEditFields,
): { draft: ItemEditFields; baseline: ItemEditFields; conflicted: boolean } {
  const next = { ...draft };
  let conflicted = false;

  for (const field of ["name", "quantity", "category"] as const) {
    const localChanged = draft[field] !== baseline[field];
    const remoteChanged = remote[field] !== baseline[field];
    if (!localChanged && remoteChanged) {
      next[field] = remote[field];
    } else if (localChanged && remoteChanged && draft[field] !== remote[field]) {
      conflicted = true;
    }
  }

  return { draft: next, baseline: remote, conflicted };
}
