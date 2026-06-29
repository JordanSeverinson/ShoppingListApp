import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import * as listsApi from "../api/lists";
import * as recipesApi from "../api/recipes";
import { enqueueDelete, flushDeletesNow } from "../lib/deleteQueue";
import {
  createListHubConnection,
  mapHubState,
  type ConnectionStatus,
} from "../lib/listHub";
import type { CreateItemPayload, ListDetail, ListItem } from "../types/list";

interface ShoppingListContextValue {
  listId: string;
  listName: string;
  shareCode: string;
  isArchived: boolean;
  canEdit: boolean;
  items: ListItem[];
  loading: boolean;
  error: string | null;
  connectionStatus: ConnectionStatus;
  renameList: (name: string) => Promise<void>;
  addItem: (payload: CreateItemPayload) => Promise<void>;
  toggleItem: (itemId: string, isChecked: boolean) => Promise<void>;
  checkAllItems: (category?: string) => Promise<void>;
  updateItem: (
    itemId: string,
    patch: Partial<Pick<ListItem, "name" | "quantity" | "category">>,
  ) => Promise<void>;
  removeItem: (itemId: string) => void;
  importRecipe: (recipeId: string) => Promise<{ added: number; message: string }>;
}

const ShoppingListContext = createContext<ShoppingListContextValue | null>(null);

function sortItems(items: ListItem[]): ListItem[] {
  return [...items].sort(
    (a, b) => a.category.localeCompare(b.category) || a.sortOrder - b.sortOrder,
  );
}

function upsertItem(items: ListItem[], next: ListItem): ListItem[] {
  const index = items.findIndex((item) => item.id === next.id);
  if (index === -1) {
    return sortItems([...items, next]);
  }
  return items.map((item) => (item.id === next.id ? next : item));
}

function mergeItems(items: ListItem[], incoming: ListItem[]): ListItem[] {
  return incoming.reduce((acc, item) => upsertItem(acc, item), items);
}

export function ShoppingListProvider({
  listId,
  children,
}: {
  listId: string;
  children: ReactNode;
}) {
  const [detail, setDetail] = useState<ListDetail | null>(null);
  const canEdit = detail?.canEdit ?? false;
  const isArchived = detail?.isArchived ?? false;
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] =
    useState<ConnectionStatus>("connecting");
  const deleteKey = `list:${listId}`;

  const patchItems = useCallback((mutate: (items: ListItem[]) => ListItem[]) => {
    setDetail((current) =>
      current ? { ...current, items: mutate(current.items) } : current,
    );
  }, []);

  const applyDetail = useCallback((next: ListDetail) => {
    setDetail(next);
    setError(null);
  }, []);

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
      const data = await listsApi.fetchList(listId);
      applyDetail(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load list");
    } finally {
      setLoading(false);
    }
  }, [applyDetail, listId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    const connection = createListHubConnection();
    let disposed = false;

    const onItemAdded = (item: ListItem) => patchItems((items) => upsertItem(items, item));
    const onItemUpdated = (item: ListItem) => patchItems((items) => upsertItem(items, item));
    const onItemToggled = (itemId: string, isChecked: boolean) =>
      patchItems((items) =>
        items.map((item) =>
          item.id === itemId ? { ...item, isChecked } : item,
        ),
      );
    const onItemDeleted = (itemId: string) =>
      patchItems((items) => items.filter((item) => item.id !== itemId));
    const onItemsBulkDeleted = (itemIds: string[]) => {
      const idSet = new Set(itemIds.map(String));
      patchItems((items) => items.filter((item) => !idSet.has(item.id)));
    };
    const onItemsBulkAdded = (items: ListItem[]) =>
      patchItems((current) => mergeItems(current, items));
    const onItemsBulkToggled = (itemIds: string[], isChecked: boolean) => {
      const idSet = new Set(itemIds.map(String));
      patchItems((items) =>
        items.map((item) =>
          idSet.has(item.id) ? { ...item, isChecked } : item,
        ),
      );
    };

    connection.on("ItemAdded", onItemAdded);
    connection.on("ItemUpdated", onItemUpdated);
    connection.on("ItemToggled", onItemToggled);
    connection.on("ItemDeleted", onItemDeleted);
    connection.on("ItemsBulkDeleted", onItemsBulkDeleted);
    connection.on("ItemsBulkAdded", onItemsBulkAdded);
    connection.on("ItemsBulkToggled", onItemsBulkToggled);

    connection.onreconnecting(() => setConnectionStatus("reconnecting"));
    connection.onreconnected(async () => {
      setConnectionStatus("connected");
      try {
        await connection.invoke("JoinList", listId);
      } catch {
        /* join retried on next reconnect */
      }
    });
    connection.onclose(() => {
      if (!disposed) {
        setConnectionStatus("disconnected");
      }
    });

    async function startHub() {
      setConnectionStatus("connecting");
      try {
        await connection.start();
        if (disposed) {
          return;
        }
        await connection.invoke("JoinList", listId);
        setConnectionStatus(mapHubState(connection.state));
      } catch (err) {
        if (!disposed) {
          setConnectionStatus("disconnected");
          console.error("SignalR connection failed", err);
        }
      }
    }

    void startHub();

    return () => {
      disposed = true;
      void (async () => {
        try {
          if (connection.state === "Connected") {
            await connection.invoke("LeaveList", listId);
          }
        } finally {
          await connection.stop();
        }
      })();
    };
  }, [listId, patchItems]);

  const flushListDeletes = useCallback(
    (itemIds: string[], options?: { keepalive?: boolean }) =>
      listsApi.deleteItems(listId, itemIds, options),
    [listId],
  );

  const restoreItems = useCallback(
    (items: ListItem[]) => {
      patchItems((current) => sortItems([...current, ...items]));
      setError("Could not delete items");
    },
    [patchItems],
  );

  useEffect(() => {
    return () => {
      void flushDeletesNow(deleteKey, { keepalive: true });
    };
  }, [deleteKey]);

  const renameList = useCallback(
    async (name: string) => {
      await listsApi.renameList(listId, name);
      setDetail((current) => (current ? { ...current, name } : current));
    },
    [listId],
  );

  const addItem = useCallback(
    async (payload: CreateItemPayload) => {
      if (!canEdit) {
        return;
      }
      const created = await listsApi.createItem(listId, payload);
      patchItems((items) => upsertItem(items, created));
    },
    [canEdit, listId, patchItems],
  );

  const toggleItem = useCallback(
    async (itemId: string, isChecked: boolean) => {
      if (!canEdit) {
        return;
      }

      const current = detail?.items.find((item) => item.id === itemId);
      if (!current || current.isChecked === isChecked) {
        return;
      }

      const previousChecked = current.isChecked;

      patchItems((items) =>
        items.map((item) =>
          item.id === itemId ? { ...item, isChecked } : item,
        ),
      );

      try {
        await listsApi.updateItem(listId, itemId, { isChecked });
      } catch (err) {
        patchItems((items) =>
          items.map((item) =>
            item.id === itemId ? { ...item, isChecked: previousChecked } : item,
          ),
        );
        setError(err instanceof Error ? err.message : "Could not update item");
      }
    },
    [canEdit, detail?.items, listId, patchItems],
  );

  const checkAllItems = useCallback(
    async (category?: string) => {
      if (!canEdit) {
        return;
      }

      const targets =
        detail?.items.filter(
          (item) => !item.isChecked && (!category || item.category === category),
        ) ?? [];

      if (targets.length === 0) {
        return;
      }

      const previous = new Map(targets.map((item) => [item.id, item.isChecked]));
      const targetIds = new Set(targets.map((item) => item.id));

      patchItems((items) =>
        items.map((item) =>
          targetIds.has(item.id) ? { ...item, isChecked: true } : item,
        ),
      );

      try {
        await listsApi.checkAllItems(listId, category);
      } catch (err) {
        patchItems((items) =>
          items.map((item) =>
            previous.has(item.id)
              ? { ...item, isChecked: previous.get(item.id)! }
              : item,
          ),
        );
        setError(err instanceof Error ? err.message : "Could not check all items");
      }
    },
    [canEdit, detail?.items, listId, patchItems],
  );

  const updateItem = useCallback(
    async (
      itemId: string,
      patch: Partial<Pick<ListItem, "name" | "quantity" | "category">>,
    ) => {
      if (!canEdit) {
        return;
      }
      const updated = await listsApi.updateItem(listId, itemId, patch);
      patchItems((items) => upsertItem(items, updated));
    },
    [canEdit, listId, patchItems],
  );

  const removeItem = useCallback(
    (itemId: string) => {
      if (!canEdit) {
        return;
      }

      patchItems((items) => {
        const removed = items.find((item) => item.id === itemId);
        if (!removed) {
          return items;
        }

        enqueueDelete(deleteKey, removed, flushListDeletes, restoreItems);
        return items.filter((item) => item.id !== itemId);
      });
    },
    [canEdit, deleteKey, flushListDeletes, patchItems, restoreItems],
  );

  const importRecipe = useCallback(
    async (recipeId: string) => {
      if (!canEdit) {
        throw new Error("This list is archived and cannot be edited.");
      }
      const result = await recipesApi.importRecipeToList(listId, recipeId);
      if (result.items.length > 0) {
        patchItems((items) => mergeItems(items, result.items));
      }
      return { added: result.items.length, message: result.message };
    },
    [canEdit, listId, patchItems],
  );

  const value = useMemo<ShoppingListContextValue>(
    () => ({
      listId,
      listName: detail?.name ?? "Shopping List",
      shareCode: detail?.shareCode ?? "",
      isArchived,
      canEdit,
      items: detail?.items ?? [],
      loading,
      error,
      connectionStatus,
      renameList,
      addItem,
      toggleItem,
      checkAllItems,
      updateItem,
      removeItem,
      importRecipe,
    }),
    [
      listId,
      detail,
      isArchived,
      canEdit,
      loading,
      error,
      connectionStatus,
      renameList,
      addItem,
      toggleItem,
      checkAllItems,
      updateItem,
      removeItem,
      importRecipe,
    ],
  );

  return (
    <ShoppingListContext.Provider value={value}>{children}</ShoppingListContext.Provider>
  );
}

export function useShoppingList(): ShoppingListContextValue {
  const context = useContext(ShoppingListContext);
  if (!context) {
    throw new Error("useShoppingList must be used within ShoppingListProvider");
  }
  return context;
}
