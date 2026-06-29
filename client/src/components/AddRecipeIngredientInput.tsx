import { Plus } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { CATEGORIES, suggestCategory } from "../lib/categories";
import type { CreateRecipeIngredientPayload } from "../types/recipe";

export function AddRecipeIngredientInput({
  onAdd,
}: {
  onAdd: (payload: CreateRecipeIngredientPayload) => Promise<void>;
}) {
  const [name, setName] = useState("");
  const [quantity, setQuantity] = useState("");
  const [category, setCategory] = useState<string>("Other");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (name.trim()) {
      setCategory(suggestCategory(name));
    }
  }, [name]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) {
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await onAdd({
        name: trimmed,
        quantity: quantity.trim() || null,
        category,
      });
      setName("");
      setQuantity("");
      setCategory("Other");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not add ingredient");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form
      onSubmit={(event) => void handleSubmit(event)}
      className="rounded-2xl border border-border bg-white p-4 shadow-sm"
    >
      <label className="mb-2 block text-sm font-medium text-ink" htmlFor="ingredient-name">
        Add ingredient
      </label>
      <div className="flex flex-col gap-3 sm:flex-row">
        <input
          id="ingredient-name"
          type="text"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="e.g. Cherry tomatoes"
          className="min-w-0 flex-1 rounded-xl border border-border px-4 py-2.5 text-ink outline-none ring-brand-500/30 placeholder:text-muted focus:border-brand-500 focus:ring-2"
          autoComplete="off"
        />
        <input
          type="text"
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          placeholder="Qty (optional)"
          className="w-full rounded-xl border border-border px-4 py-2.5 text-ink outline-none ring-brand-500/30 placeholder:text-muted focus:border-brand-500 focus:ring-2 sm:w-36"
        />
        <select
          value={category}
          onChange={(event) => setCategory(event.target.value)}
          className="w-full rounded-xl border border-border bg-white px-3 py-2.5 text-ink outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30 sm:w-40"
          aria-label="Category"
        >
          {CATEGORIES.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
        <button
          type="submit"
          disabled={submitting || !name.trim()}
          className="inline-flex items-center justify-center gap-2 rounded-xl bg-brand-600 px-5 py-2.5 font-medium text-white transition hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <Plus className="h-4 w-4" aria-hidden />
          Add
        </button>
      </div>
      {category !== "Other" && name.trim() && (
        <p className="mt-2 text-xs text-muted">
          Suggested category: <span className="font-medium text-brand-700">{category}</span>
        </p>
      )}
      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
    </form>
  );
}
