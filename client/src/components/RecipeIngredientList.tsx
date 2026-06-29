import { Trash2 } from "lucide-react";
import { useMemo } from "react";
import { CATEGORIES } from "../lib/categories";
import type { RecipeIngredient } from "../types/recipe";

function groupByCategory(ingredients: RecipeIngredient[]): Map<string, RecipeIngredient[]> {
  const groups = new Map<string, RecipeIngredient[]>();

  for (const ingredient of ingredients) {
    const key = ingredient.category || "Other";
    const bucket = groups.get(key) ?? [];
    bucket.push(ingredient);
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

export function RecipeIngredientList({
  ingredients,
  onRemove,
}: {
  ingredients: RecipeIngredient[];
  onRemove: (ingredientId: string) => void;
}) {
  const grouped = useMemo(() => groupByCategory(ingredients), [ingredients]);

  if (ingredients.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-border bg-white/60 px-6 py-14 text-center">
        <p className="text-lg font-medium text-ink">No ingredients yet</p>
        <p className="mt-1 text-sm text-muted">
          Add ingredients manually or import from a screenshot.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <p className="text-sm text-muted">
        {ingredients.length} ingredient{ingredients.length === 1 ? "" : "s"}
      </p>
      {[...grouped.entries()].map(([category, categoryIngredients]) => (
        <section key={category}>
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-muted">
            <span className="h-2 w-2 rounded-full bg-brand-500" />
            {category}
            <span className="font-normal normal-case text-muted/80">
              ({categoryIngredients.length})
            </span>
          </h2>
          <ul className="space-y-2">
            {categoryIngredients.map((ingredient) => (
              <IngredientRow
                key={ingredient.id}
                ingredient={ingredient}
                onRemove={onRemove}
              />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

function IngredientRow({
  ingredient,
  onRemove,
}: {
  ingredient: RecipeIngredient;
  onRemove: (ingredientId: string) => void;
}) {
  return (
    <li className="group flex items-center gap-3 rounded-xl border border-border bg-white px-3 py-2.5 transition hover:border-brand-200">
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{ingredient.name}</p>
        {ingredient.quantity && (
          <p className="truncate text-xs text-muted">{ingredient.quantity}</p>
        )}
      </div>
      <button
        type="button"
        onClick={() => onRemove(ingredient.id)}
        className="shrink-0 rounded-lg p-2 text-muted opacity-100 transition hover:bg-red-50 hover:text-red-600 sm:opacity-0 sm:group-hover:opacity-100"
        aria-label={`Remove ${ingredient.name}`}
      >
        <Trash2 className="h-4 w-4" />
      </button>
    </li>
  );
}
