import { Check, Pencil, Trash2, X } from "lucide-react";
import { useMemo, useState } from "react";
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

function ItemRow({ item, readOnly }: { item: ListItem; readOnly: boolean }) {
  const { toggleItem, updateItem, removeItem } = useShoppingList();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(item.name);
  const [quantity, setQuantity] = useState(item.quantity ?? "");
  const [category, setCategory] = useState(item.category);
  const [busy, setBusy] = useState(false);

  async function handleToggle() {
    setBusy(true);
    try {
      await toggleItem(item.id, !item.isChecked);
    } finally {
      setBusy(false);
    }
  }

  async function handleSave() {
    setBusy(true);
    try {
      await updateItem(item.id, {
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
    setBusy(true);
    try {
      await removeItem(item.id);
    } finally {
      setBusy(false);
    }
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
        onChange={() => void handleToggle()}
        disabled={busy || readOnly}
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
            disabled={busy}
            className="rounded-lg p-2 text-muted hover:bg-red-50 hover:text-red-600"
            aria-label={`Delete ${item.name}`}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )}
    </li>
  );
}

export function ListView({ readOnly = false }: { readOnly?: boolean }) {
  const { items, loading, error } = useShoppingList();

  const grouped = useMemo(() => groupByCategory(items), [items]);
  const checkedCount = items.filter((item) => item.isChecked).length;

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
      <p className="text-sm text-muted">
        {checkedCount} of {items.length} checked off
      </p>
      {[...grouped.entries()].map(([category, categoryItems]) => (
        <section key={category}>
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-muted">
            <span className="h-2 w-2 rounded-full bg-brand-500" />
            {category}
            <span className="font-normal normal-case text-muted/80">
              ({categoryItems.length})
            </span>
          </h2>
          <ul className="space-y-2">
            {categoryItems.map((item) => (
              <ItemRow key={item.id} item={item} readOnly={readOnly} />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}
