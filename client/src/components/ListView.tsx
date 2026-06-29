import { Check, CheckCheck, Pencil, Trash2, X } from "lucide-react";
import { memo, useMemo, useState } from "react";
import { useShoppingList } from "../context/ShoppingListContext";
import { CATEGORIES } from "../lib/categories";
import type { ListItem } from "../types/list";

function groupByCategory(items: ListItem[]): Map<string, ListItem[]> {
  const groups = new Map<string, ListItem[]>();

  for (const item of items) {
    const key = item.category || "Other";
    const bucket = groups.get(key) ?? [];
    bucket.push(item);
    groups.set(key, bucket);
  }

  const ordered: string[] = [...CATEGORIES];
  for (const key of groups.keys()) {
    if (!ordered.includes(key)) {
      ordered.push(key);
    }
  }

  return new Map(
    ordered
      .filter((category) => groups.has(category))
      .map((category) => [category, groups.get(category)!]),
  );
}

function itemsEqual(a: ListItem, b: ListItem): boolean {
  return (
    a.id === b.id
    && a.isChecked === b.isChecked
    && a.name === b.name
    && a.quantity === b.quantity
    && a.category === b.category
  );
}

const ItemRow = memo(function ItemRow({
  item,
  readOnly,
  onToggle,
  onUpdate,
  onRemove,
}: {
  item: ListItem;
  readOnly: boolean;
  onToggle: (itemId: string, isChecked: boolean) => void;
  onUpdate: (
    itemId: string,
    patch: Partial<Pick<ListItem, "name" | "quantity" | "category">>,
  ) => Promise<void>;
  onRemove: (itemId: string) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(item.name);
  const [quantity, setQuantity] = useState(item.quantity ?? "");
  const [category, setCategory] = useState(item.category);
  const [busy, setBusy] = useState(false);

  async function handleSave() {
    setBusy(true);
    try {
      await onUpdate(item.id, {
        name: name.trim(),
        quantity: quantity.trim() || null,
        category,
      });
      setEditing(false);
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete() {
    onRemove(item.id);
  }

  if (editing && !readOnly) {
    return (
      <li className="flex flex-col gap-2 rounded-xl border border-brand-200 bg-brand-50/50 p-3 sm:flex-row sm:items-center">
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="flex-1 rounded-lg border border-border px-3 py-2 text-sm"
        />
        <input
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          placeholder="Qty"
          className="w-full rounded-lg border border-border px-3 py-2 text-sm sm:w-28"
        />
        <select
          value={category}
          onChange={(event) => setCategory(event.target.value)}
          className="rounded-lg border border-border bg-white px-2 py-2 text-sm"
        >
          {CATEGORIES.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => void handleSave()}
            disabled={busy || !name.trim()}
            className="rounded-lg bg-brand-600 p-2 text-white hover:bg-brand-700 disabled:opacity-50"
            aria-label="Save"
          >
            <Check className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => setEditing(false)}
            className="rounded-lg border border-border p-2 text-muted hover:bg-white"
            aria-label="Cancel"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      </li>
    );
  }

  return (
    <li
      className={`group flex items-center gap-3 rounded-xl border px-3 py-2.5 transition ${
        item.isChecked
          ? "border-border/60 bg-stone-50 opacity-70"
          : "border-border bg-white hover:border-brand-200"
      }`}
    >
      <input
        type="checkbox"
        checked={item.isChecked}
        onChange={() => onToggle(item.id, !item.isChecked)}
        disabled={readOnly}
        className="h-5 w-5 shrink-0 rounded border-border text-brand-600 focus:ring-brand-500 disabled:cursor-default"
        aria-label={`Mark ${item.name} as ${item.isChecked ? "not done" : "done"}`}
      />
      <div className="min-w-0 flex-1">
        <p
          className={`truncate font-medium ${item.isChecked ? "text-muted line-through" : "text-ink"}`}
        >
          {item.name}
        </p>
        {item.quantity && (
          <p className="truncate text-xs text-muted">{item.quantity}</p>
        )}
      </div>
      {!readOnly && (
        <div className="flex shrink-0 gap-1 opacity-100 sm:opacity-0 sm:group-hover:opacity-100">
          <button
            type="button"
            onClick={() => setEditing(true)}
            className="rounded-lg p-2 text-muted hover:bg-stone-100 hover:text-ink"
            aria-label={`Edit ${item.name}`}
          >
            <Pencil className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => void handleDelete()}
            className="rounded-lg p-2 text-muted hover:bg-red-50 hover:text-red-600"
            aria-label={`Delete ${item.name}`}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )}
    </li>
  );
}, (prev, next) => prev.readOnly === next.readOnly && itemsEqual(prev.item, next.item));

export function ListView({ readOnly = false }: { readOnly?: boolean }) {
  const { items, loading, error, toggleItem, checkAllItems, updateItem, removeItem } =
    useShoppingList();

  const grouped = useMemo(() => groupByCategory(items), [items]);
  const checkedCount = useMemo(
    () => items.reduce((count, item) => count + (item.isChecked ? 1 : 0), 0),
    [items],
  );
  const uncheckedCount = items.length - checkedCount;

  async function handleCheckAll(category?: string) {
    await checkAllItems(category);
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16 text-muted">
        Loading list…
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-6 text-center text-red-700">
        {error}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-border bg-white/60 px-6 py-14 text-center">
        <p className="text-lg font-medium text-ink">Your list is empty</p>
        <p className="mt-1 text-sm text-muted">Add your first item above.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted">
          {checkedCount} of {items.length} checked off
        </p>
        {!readOnly && uncheckedCount > 0 && (
          <button
            type="button"
            onClick={() => void handleCheckAll()}
            className="inline-flex items-center gap-1.5 rounded-lg border border-brand-200 bg-brand-50 px-3 py-1.5 text-sm font-medium text-brand-700 transition hover:bg-brand-100"
          >
            <CheckCheck className="h-4 w-4" aria-hidden />
            Check all items
          </button>
        )}
      </div>
      {[...grouped.entries()].map(([category, categoryItems]) => {
        const categoryUnchecked = categoryItems.filter((item) => !item.isChecked).length;

        return (
        <section key={category}>
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-muted">
              <span className="h-2 w-2 rounded-full bg-brand-500" />
              {category}
              <span className="font-normal normal-case text-muted/80">
                ({categoryItems.length})
              </span>
            </h2>
            {!readOnly && categoryUnchecked > 0 && (
              <button
                type="button"
                onClick={() => void handleCheckAll(category)}
                className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-medium text-brand-700 transition hover:bg-brand-50"
              >
                <CheckCheck className="h-3.5 w-3.5" aria-hidden />
                Check all
              </button>
            )}
          </div>
          <ul className="space-y-2">
            {categoryItems.map((item) => (
              <ItemRow
                key={item.id}
                item={item}
                readOnly={readOnly}
                onToggle={toggleItem}
                onUpdate={updateItem}
                onRemove={removeItem}
              />
            ))}
          </ul>
        </section>
        );
      })}
    </div>
  );
}
