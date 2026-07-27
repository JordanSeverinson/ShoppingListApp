import { ChefHat, ChevronRight, Pencil, Trash2, UserPlus, Users } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router-dom";
import * as recipesApi from "../api/recipes";
import type { RecipeSummary } from "../types/recipe";
import { EditableListName } from "./EditableListName";
import { ShareWithFriendsModal } from "./ShareWithFriendsModal";

export function RecipeCard({
  recipe,
  onRename,
  onDelete,
  onShared,
}: {
  recipe: RecipeSummary;
  onRename: (recipeId: string, name: string) => Promise<void>;
  onDelete?: (recipe: RecipeSummary) => void;
  onShared?: (message: string) => void;
}) {
  const [shareOpen, setShareOpen] = useState(false);

  return (
    <>
      <article className="rounded-2xl border border-border bg-white p-4 shadow-sm transition hover:border-brand-200 hover:shadow-md">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 flex-1">
            <div className="mb-1 flex flex-wrap items-center gap-1.5 text-xs font-medium text-brand-700">
              {recipe.isOwner ? (
                <>
                  <ChefHat className="h-3.5 w-3.5" aria-hidden />
                  Your Recipe
                </>
              ) : (
                <>
                  <Users className="h-3.5 w-3.5" aria-hidden />
                  Shared Recipe
                </>
              )}
              {recipe.recipeType && (
                <span className="rounded-full bg-surface px-2 py-0.5 font-medium text-muted">
                  {recipe.recipeType}
                </span>
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
          <p className="text-sm text-muted">
            {recipe.ingredientCount} ingredient{recipe.ingredientCount === 1 ? "" : "s"}
          </p>

          <div className="flex flex-wrap gap-2">
            {recipe.isOwner && (
              <button
                type="button"
                onClick={() => setShareOpen(true)}
                className="inline-flex items-center gap-2 rounded-lg border border-brand-200 bg-brand-50 px-3 py-1.5 text-sm font-medium text-brand-800 transition hover:border-brand-300 hover:bg-brand-100"
              >
                <UserPlus className="h-4 w-4" aria-hidden />
                Share with Friends
              </button>
            )}

            {recipe.isOwner && (
              <Link
                to={`/recipes/${recipe.id}/edit`}
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:border-brand-200 hover:bg-brand-50 hover:text-brand-700"
              >
                <Pencil className="h-4 w-4" aria-hidden />
                Edit Recipe
              </Link>
            )}

            {recipe.isOwner && onDelete && (
              <button
                type="button"
                onClick={() => onDelete(recipe)}
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-700"
              >
                <Trash2 className="h-4 w-4" aria-hidden />
                Delete Recipe
              </button>
            )}
          </div>
        </div>
      </article>

      <ShareWithFriendsModal
        open={shareOpen}
        itemName={recipe.name}
        itemLabel="recipe"
        onClose={() => setShareOpen(false)}
        onShared={onShared}
        onShare={(friendUserIds) => recipesApi.shareRecipe(recipe.id, friendUserIds)}
      />
    </>
  );
}
