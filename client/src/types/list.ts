export interface ListItem {
  id: string;
  shoppingListId: string;
  name: string;
  quantity: string | null;
  category: string;
  isChecked: boolean;
  sortOrder: number;
}

export interface ListSummary {
  id: string;
  name: string;
  isArchived: boolean;
  isOwner: boolean;
  itemCount: number;
  checkedCount: number;
  acceptedShareCount: number;
  ownerDisplayName: string;
}

export function getListOwnershipLabel(list: ListSummary): string {
  if (list.isOwner) {
    if (list.acceptedShareCount === 0) {
      return "Your List";
    }

    if (list.acceptedShareCount === 1) {
      return "Your List (Shared with 1 Person)";
    }

    return `Your List (Shared with ${list.acceptedShareCount} People)`;
  }

  const ownerName = list.ownerDisplayName.trim();
  return ownerName ? `${ownerName}'s List` : "Shared List";
}

export interface PendingListShare {
  id: string;
  listId: string;
  listName: string;
  invitedByName: string;
  invitedByUserId: string;
  itemCount: number;
  checkedCount: number;
  isArchived: boolean;
}

export interface ListSummaryResponse {
  activeLists: ListSummary[];
  archivedLists: ListSummary[];
  pendingShares: PendingListShare[];
}

export interface ListDetail {
  id: string;
  name: string;
  isArchived: boolean;
  isOwner: boolean;
  canEdit: boolean;
  items: ListItem[];
}

export interface ShareListResponse {
  invitedCount: number;
  skippedCount: number;
  message: string;
}

export interface CreateItemPayload {
  name: string;
  quantity?: string | null;
  category: string;
}

export interface UpdateListItemPayload {
  name?: string;
  quantity?: string | null;
  category?: string;
  isChecked?: boolean;
  base?: {
    name: string;
    quantity: string | null;
    category: string;
  };
}

export interface CheckAllItemsResponse {
  updatedCount: number;
  itemIds: string[];
}

export interface DeleteItemsResponse {
  deletedCount: number;
  itemIds: string[];
}
