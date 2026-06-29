import { ArrowLeft, ChefHat } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import * as recipesApi from "../api/recipes";
import { AddRecipeIngredientInput } from "../components/AddRecipeIngredientInput";
import { EditableListName } from "../components/EditableListName";
import { RecipeImageUploader } from "../components/RecipeImageUploader";
import { RecipeIngredientList } from "../components/RecipeIngredientList";
import { ShareCodeCopy } from "../components/ShareCodeCopy";
import { enqueueDelete, flushDeletesNow, hasPendingDeletes } from "../lib/deleteQueue";
import type {
  CreateRecipeIngredientPayload,
  RecipeDetail,
  RecipeIngredient,
} from "../types/recipe";

function sortIngredients(ingredients: RecipeIngredient[]): RecipeIngredient[] {
  return [...ingredients].sort(
    (a, b) => a.category.localeCompare(b.category) || a.sortOrder - b.sortOrder,
  );
}

export function RecipeDetailPage() {
  const navigate = useNavigate();
  const { recipeId } = useParams<{ recipeId: string }>();
  const [detail, setDetail] = useState<RecipeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const deleteKey = recipeId ? `recipe:${recipeId}` : null;

  const patchIngredients = useCallback(
    (mutate: (ingredients: RecipeIngredient[]) => RecipeIngredient[]) => {
      setDetail((current) =>
        current ? { ...current, ingredients: mutate(current.ingredients) } : current,
      );
    },
    [],
  );

  const refresh = useCallback(async () => {
    if (!recipeId) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await recipesApi.fetchRecipe(recipeId);
      setDetail(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load recipe");
    } finally {
      setLoading(false);
    }
  }, [recipeId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const flushRecipeDeletes = useCallback(
    (ingredientIds: string[], options?: { keepalive?: boolean }) =>
      recipesApi.deleteRecipeIngredients(recipeId!, ingredientIds, options),
    [recipeId],
  );

  const restoreIngredients = useCallback(
    (ingredients: RecipeIngredient[]) => {
      patchIngredients((current) => sortIngredients([...current, ...ingredients]));
      setError("Could not delete ingredients");
    },
    [patchIngredients],
  );

  useEffect(() => {
    if (!deleteKey) {
      return;
    }

    return () => {
      void flushDeletesNow(deleteKey, { keepalive: true });
    };
  }, [deleteKey]);

  async function leaveRecipePage() {
    if (deleteKey && hasPendingDeletes(deleteKey)) {
      await flushDeletesNow(deleteKey);
    }
    navigate("/recipes");
  }

  async function handleRename(name: string) {
    if (!recipeId) {
      return;
    }
    const updated = await recipesApi.renameRecipe(recipeId, name);
    setDetail((current) => (current ? { ...current, name: updated.name } : current));
  }

  async function handleAddIngredient(payload: CreateRecipeIngredientPayload) {
    if (!recipeId) {
      return;
    }
    const created = await recipesApi.createRecipeIngredient(recipeId, payload);
    patchIngredients((ingredients) => sortIngredients([...ingredients, created]));
  }

  const handleRemoveIngredient = useCallback(
    (ingredientId: string) => {
      if (!deleteKey) {
        return;
      }

      patchIngredients((ingredients) => {
        const removed = ingredients.find((item) => item.id === ingredientId);
        if (!removed) {
          return ingredients;
        }

        enqueueDelete(deleteKey, removed, flushRecipeDeletes, restoreIngredients);
        return ingredients.filter((item) => item.id !== ingredientId);
      });
    },
    [deleteKey, flushRecipeDeletes, patchIngredients, restoreIngredients],
  );

  if (!recipeId) {
    return (
      <div className="mx-auto max-w-lg px-4 py-20 text-center text-red-700">
        Missing recipe id in URL.
      </div>
    );
  }

  return (
    <div className="mx-auto min-h-screen max-w-2xl px-4 py-8 sm:px-6 sm:py-12">
      <button
        type="button"
        onClick={() => void leaveRecipePage()}
        className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-brand-700"
      >
        <ArrowLeft className="h-4 w-4" aria-hidden />
        All recipes
      </button>

      {error && detail && (
        <p className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {loading && !detail ? (
        <p className="text-center text-muted">Loading recipe…</p>
      ) : error && !detail ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-6 text-center text-red-700">
          {error}
        </div>
      ) : detail ? (
        <>
          <header className="mb-8">
            <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
              <ChefHat className="h-3.5 w-3.5" aria-hidden />
              Shared recipe
            </div>

            <EditableListName name={detail.name} size="lg" onSave={handleRename} />

            <div className="mt-4">
              <ShareCodeCopy shareCode={detail.shareCode} />
            </div>
          </header>

          <div className="space-y-8">
            <RecipeImageUploader recipeId={recipeId} onIngredientsAdded={() => void refresh()} />
            <AddRecipeIngredientInput onAdd={handleAddIngredient} />
            <RecipeIngredientList
              ingredients={detail.ingredients}
              onRemove={handleRemoveIngredient}
            />
          </div>
        </>
      ) : null}
    </div>
  );
}
