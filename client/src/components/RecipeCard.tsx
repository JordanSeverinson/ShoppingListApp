import { ChefHat, ChevronRight, Users } from "lucide-react";
import { Link } from "react-router-dom";
import type { RecipeSummary } from "../types/recipe";
import { EditableListName } from "./EditableListName";
import { ShareCodeCopy } from "./ShareCodeCopy";

export function RecipeCard({
  recipe,
  onRename,
}: {
  recipe: RecipeSummary;
  onRename: (recipeId: string, name: string) => Promise<void>;
}) {
  return (
    <article className="rounded-2xl border border-border bg-white p-4 shadow-sm transition hover:border-brand-200 hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <div className="mb-1 flex items-center gap-1.5 text-xs font-medium text-brand-700">
            {recipe.isOwner ? (
              <>
                <ChefHat className="h-3.5 w-3.5" aria-hidden />
                Your recipe
              </>
            ) : (
              <>
                <Users className="h-3.5 w-3.5" aria-hidden />
                Shared recipe
              </>
            )}
          </div>
          <EditableListName
            name={recipe.name}
            size="sm"
            onSave={(name) => onRename(recipe.id, name)}
          />
        </div>
        <Link
          to={`/recipes/${recipe.id}`}
          className="shrink-0 rounded-lg p-2 text-muted hover:bg-brand-50 hover:text-brand-700"
          aria-label={`Open ${recipe.name}`}
        >
          <ChevronRight className="h-5 w-5" />
        </Link>
      </div>

      <div className="mt-3 space-y-3">
        <ShareCodeCopy shareCode={recipe.shareCode} />
        <p className="text-sm text-muted">
          {recipe.ingredientCount} ingredient{recipe.ingredientCount === 1 ? "" : "s"}
        </p>
      </div>
    </article>
  );
}
