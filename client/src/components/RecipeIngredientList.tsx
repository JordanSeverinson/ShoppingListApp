import { Check, Trash2 } from "lucide-react";
import { useMemo } from "react";
import { CATEGORIES } from "../lib/categories";
import type { RecipeIngredient } from "../types/recipe";

function groupBySection(ingredients: RecipeIngredient[]): Map<string, RecipeIngredient[]> {
  const groups = new Map<string, RecipeIngredient[]>();
  const sectionOrder: string[] = [];

  for (const ingredient of [...ingredients].sort((a, b) => a.sortOrder - b.sortOrder)) {
    const key = ingredient.section?.trim() || "";
    if (!groups.has(key)) {
      groups.set(key, []);
      sectionOrder.push(key);
    }

    groups.get(key)!.push(ingredient);
  }

  return new Map(sectionOrder.map((section) => [section, groups.get(section)!]));
}

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

function groupIngredients(ingredients: RecipeIngredient[]): {
  groups: Map<string, RecipeIngredient[]>;
  mode: "section" | "category";
} {
  const hasSections = ingredients.some((ingredient) => ingredient.section?.trim());
  if (hasSections) {
    return { groups: groupBySection(ingredients), mode: "section" };
  }

  return { groups: groupByCategory(ingredients), mode: "category" };
}

export function RecipeIngredientList({
  ingredients,
  onRemove,
  readOnly = false,
}: {
  ingredients: RecipeIngredient[];
  onRemove?: (ingredientId: string) => void;
  readOnly?: boolean;
}) {
  const { groups, mode } = useMemo(() => groupIngredients(ingredients), [ingredients]);

  if (ingredients.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-border bg-white/60 px-6 py-14 text-center">
        <p className="text-lg font-medium text-ink">No ingredients yet</p>
        <p className="mt-1 text-sm text-muted">
          {readOnly
            ? "This recipe does not have any ingredients."
            : "Add ingredients manually or import from a screenshot."}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <p className="text-sm text-muted">
        {ingredients.length} ingredient{ingredients.length === 1 ? "" : "s"}
        {mode === "section" ? " · grouped by recipe section" : ""}
      </p>
      {[...groups.entries()].map(([groupLabel, groupIngredients]) => (
        <section key={groupLabel || "default"}>
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-muted">
            <span className="h-2 w-2 rounded-full bg-brand-500" />
            {groupLabel || (mode === "section" ? "Ingredients" : "Other")}
            <span className="font-normal normal-case text-muted/80">
              ({groupIngredients.length})
            </span>
          </h2>
          <ul className="space-y-2">
            {groupIngredients.map((ingredient) => (
              <IngredientRow
                key={ingredient.id}
                ingredient={ingredient}
                onRemove={onRemove}
                readOnly={readOnly}
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
  readOnly,
}: {
  ingredient: RecipeIngredient;
  onRemove?: (ingredientId: string) => void;
  readOnly: boolean;
}) {
  return (
    <li className="group flex items-center gap-3 rounded-xl border border-border bg-white px-3 py-2.5 transition hover:border-brand-200">
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">
          {ingredient.quantity ? (
            <>
              <span className="text-muted">{ingredient.quantity}</span> {ingredient.name}
            </>
          ) : (
            ingredient.name
          )}
        </p>
      </div>
      {!readOnly && onRemove && (
        <button
          type="button"
          onClick={() => onRemove(ingredient.id)}
          className="shrink-0 rounded-lg p-2 text-muted opacity-100 transition hover:bg-red-50 hover:text-red-600 sm:opacity-0 sm:group-hover:opacity-100"
          aria-label={`Remove ${ingredient.name}`}
        >
          <Trash2 className="h-4 w-4" />
        </button>
      )}
      {readOnly && (
        <Check className="h-4 w-4 shrink-0 text-brand-300" aria-hidden />
      )}
    </li>
  );
}
