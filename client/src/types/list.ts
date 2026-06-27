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
  shareCode: string;
  isArchived: boolean;
  itemCount: number;
  checkedCount: number;
}

export interface ListSummaryResponse {
  sharedLists: ListSummary[];
  archivedLists: ListSummary[];
}

export interface ListDetail {
  id: string;
  name: string;
  shareCode: string;
  isArchived: boolean;
  canEdit: boolean;
  items: ListItem[];
}

export interface CreateItemPayload {
  name: string;
  quantity?: string | null;
  category: string;
}

export interface UploadImageResponse {
  listId: string;
  items: ListItem[];
  message: string;
}
