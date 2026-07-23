import { ArrowLeft, ChefHat, Eye, Pencil, UserPlus } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { Link, Navigate, useNavigate, useParams } from "react-router-dom";
import * as recipesApi from "../api/recipes";
import { EditableListName } from "../components/EditableListName";
import { RecipeImageUploader } from "../components/RecipeImageUploader";
import { RecipeReadOnlyView } from "../components/RecipeReadOnlyView";
import { RecipeSectionsEditor } from "../components/RecipeSectionsEditor";
import { RecipeStepsEditor } from "../components/RecipeStepsEditor";
import { ShareWithFriendsModal } from "../components/ShareWithFriendsModal";
import { enqueueDelete, flushDeletesNow, hasPendingDeletes } from "../lib/deleteQueue";
import type {
  CreateRecipeIngredientPayload,
  RecipeDetail,
  RecipeIngredient,
  UpdateRecipeIngredientPayload,
} from "../types/recipe";
import { buildRecipeContentDocument } from "../types/recipe";

function sortIngredients(ingredients: RecipeIngredient[]): RecipeIngredient[] {
  return [...ingredients].sort(
    (a, b) =>
      (a.section ?? "").localeCompare(b.section ?? "") || a.sortOrder - b.sortOrder,
  );
}

function sectionLabel(ingredient: RecipeIngredient): string {
  return ingredient.section?.trim() || "Ingredients";
}

export function RecipeDetailPage({ readOnly = false }: { readOnly?: boolean }) {
  const navigate = useNavigate();
  const { recipeId } = useParams<{ recipeId: string }>();
  const [detail, setDetail] = useState<RecipeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [shareOpen, setShareOpen] = useState(false);
  const [shareMessage, setShareMessage] = useState<string | null>(null);
  const deleteKey = recipeId ? `recipe:${recipeId}` : null;

  const patchIngredients = useCallback(
    (mutate: (ingredients: RecipeIngredient[]) => RecipeIngredient[]) => {
      setDetail((current) =>
        current ? { ...current, ingredients: mutate(current.ingredients) } : current,
      );
    },
    [],
  );

  const refresh = useCallback(async (options?: { showLoading?: boolean }) => {
    if (!recipeId) {
      return;
    }

    const showLoading = options?.showLoading ?? true;
    if (showLoading) {
      setLoading(true);
    }
    setError(null);
    try {
      const data = await recipesApi.fetchRecipe(recipeId);
      setDetail(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load recipe");
    } finally {
      if (showLoading) {
        setLoading(false);
      }
    }
  }, [recipeId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (!shareMessage) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setShareMessage(null);
    }, 3000);

    return () => window.clearTimeout(timeoutId);
  }, [shareMessage]);

  const readOnlyRef = useRef(readOnly);
  useEffect(() => {
    if (readOnlyRef.current === readOnly) {
      return;
    }

    readOnlyRef.current = readOnly;
    void refresh({ showLoading: false });
  }, [readOnly, refresh]);

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
    if (!deleteKey || readOnly) {
      return;
    }

    return () => {
      void flushDeletesNow(deleteKey, { keepalive: true });
    };
  }, [deleteKey, readOnly]);

  function stepTextsFromDetail(recipe: RecipeDetail): string[] {
    return recipe.content.recipe.cookingSteps.length
      ? recipe.content.recipe.cookingSteps
      : recipe.steps.map((step) => step.text);
  }

  const syncContent = useCallback(
    async (ingredients: RecipeIngredient[], cookingSteps: string[]) => {
      if (!recipeId) {
        return;
      }

      const content = buildRecipeContentDocument(ingredients, cookingSteps);
      try {
        const saved = await recipesApi.saveRecipeContent(recipeId, content);
        setDetail((current) => (current ? { ...current, content: saved } : current));
      } catch {
        // Ingredient edits already saved; content sync can retry on next mutation.
      }
    },
    [recipeId],
  );

  async function leaveEditPage(destination: "view" | "list" = "view") {
    if (deleteKey && hasPendingDeletes(deleteKey)) {
      await flushDeletesNow(deleteKey);
    }

    if (recipeId) {
      try {
        const data = await recipesApi.fetchRecipe(recipeId);
        const cookingSteps = stepTextsFromDetail(data);
        const content = buildRecipeContentDocument(data.ingredients, cookingSteps);
        const saved = await recipesApi.saveRecipeContent(recipeId, content);
        setDetail({ ...data, content: saved });
      } catch {
        await refresh({ showLoading: false });
      }
    }

    navigate(destination === "list" ? "/recipes" : `/recipes/${recipeId}`);
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
    let cookingSteps: string[] = [];
    let nextIngredients: RecipeIngredient[] = [];
    setDetail((current) => {
      if (!current) {
        return current;
      }
      cookingSteps = stepTextsFromDetail(current);
      nextIngredients = sortIngredients([...current.ingredients, created]);
      return { ...current, ingredients: nextIngredients };
    });
    await syncContent(nextIngredients, cookingSteps);
  }

  async function handleUpdateIngredient(
    ingredientId: string,
    payload: UpdateRecipeIngredientPayload,
  ) {
    if (!recipeId) {
      return;
    }
    const updated = await recipesApi.updateRecipeIngredient(recipeId, ingredientId, payload);
    let cookingSteps: string[] = [];
    let nextIngredients: RecipeIngredient[] = [];
    setDetail((current) => {
      if (!current) {
        return current;
      }
      cookingSteps = stepTextsFromDetail(current);
      nextIngredients = sortIngredients(
        current.ingredients.map((item) => (item.id === ingredientId ? updated : item)),
      );
      return { ...current, ingredients: nextIngredients };
    });
    await syncContent(nextIngredients, cookingSteps);
  }

  async function handleRenameSection(from: string, to: string) {
    if (!recipeId) {
      return;
    }
    await recipesApi.renameRecipeSection(recipeId, from, to);
    let cookingSteps: string[] = [];
    let nextIngredients: RecipeIngredient[] = [];
    setDetail((current) => {
      if (!current) {
        return current;
      }
      cookingSteps = stepTextsFromDetail(current);
      nextIngredients = sortIngredients(
        current.ingredients.map((item) =>
          sectionLabel(item) === from ? { ...item, section: to } : item,
        ),
      );
      return { ...current, ingredients: nextIngredients };
    });
    await syncContent(nextIngredients, cookingSteps);
  }

  async function handleSaveSteps(steps: string[]) {
    if (!recipeId) {
      return;
    }
    const saved = await recipesApi.replaceRecipeSteps(recipeId, steps);
    let ingredients: RecipeIngredient[] = [];
    setDetail((current) => {
      if (!current) {
        return current;
      }
      ingredients = current.ingredients;
      return { ...current, steps: saved };
    });
    await syncContent(ingredients, steps);
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

  const handleDeleteSectionIngredients = useCallback(
    (ingredientIds: string[]) => {
      for (const ingredientId of ingredientIds) {
        handleRemoveIngredient(ingredientId);
      }
    },
    [handleRemoveIngredient],
  );

  if (!recipeId) {
    return (
      <div className="mx-auto max-w-lg px-4 py-20 text-center text-red-700">
        Missing recipe id in URL.
      </div>
    );
  }

  if (!readOnly && detail && !detail.isOwner) {
    return <Navigate to={`/recipes/${recipeId}`} replace />;
  }

  const stepTexts =
    detail?.content.recipe.cookingSteps.length
      ? detail.content.recipe.cookingSteps
      : (detail?.steps.map((step) => step.text) ?? []);

  const backLabel = "All recipes";

  return (
    <div className="mx-auto min-h-screen max-w-2xl px-4 py-8 sm:px-6 sm:py-12">
      <button
        type="button"
        onClick={() => {
          if (readOnly) {
            navigate("/recipes");
            return;
          }
          void leaveEditPage("list");
        }}
        className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-brand-700"
      >
        <ArrowLeft className="h-4 w-4" aria-hidden />
        {backLabel}
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
            <div className="mb-3 flex flex-wrap items-center gap-2">
              <div className="inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
                <ChefHat className="h-3.5 w-3.5" aria-hidden />
                {detail.isOwner ? "Your Recipe" : "Shared Recipe"}
              </div>
              {!readOnly && (
                <button
                  type="button"
                  onClick={() => void leaveEditPage()}
                  className="inline-flex items-center gap-2 rounded-full bg-brand-600 px-4 py-1.5 text-xs font-semibold text-white transition hover:bg-brand-700"
                >
                  <Eye className="h-3.5 w-3.5" aria-hidden />
                  View recipe
                </button>
              )}
              {readOnly && detail.isOwner && (
                <Link
                  to={`/recipes/${recipeId}/edit`}
                  className="inline-flex items-center gap-2 rounded-full border border-border bg-white px-4 py-1.5 text-xs font-semibold text-brand-700 transition hover:border-brand-300 hover:bg-brand-50"
                >
                  <Pencil className="h-3.5 w-3.5" aria-hidden />
                  Edit Recipe
                </Link>
              )}
            </div>

            <EditableListName
              name={detail.name}
              size="lg"
              readOnly={readOnly}
              onSave={handleRename}
            />

            {detail.isOwner && (
              <div className="mt-4">
                <button
                  type="button"
                  onClick={() => setShareOpen(true)}
                  className="inline-flex items-center gap-2 rounded-xl border border-brand-200 bg-brand-50 px-4 py-2 text-sm font-medium text-brand-800 transition hover:border-brand-300 hover:bg-brand-100"
                >
                  <UserPlus className="h-4 w-4" aria-hidden />
                  Share with Friends
                </button>
              </div>
            )}

            {shareMessage && (
              <p className="mt-3 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
                {shareMessage}
              </p>
            )}
          </header>

          <div className="space-y-8">
            {readOnly ? (
              <RecipeReadOnlyView
                recipeName={detail.name}
                content={detail.content}
                ingredients={detail.ingredients}
                steps={stepTexts}
              />
            ) : (
              <>
                <RecipeImageUploader recipeId={recipeId} onImported={() => void refresh()} />
                <RecipeSectionsEditor
                  ingredients={detail.ingredients}
                  onAdd={handleAddIngredient}
                  onUpdate={handleUpdateIngredient}
                  onRemove={handleRemoveIngredient}
                  onRenameSection={handleRenameSection}
                  onDeleteSectionIngredients={handleDeleteSectionIngredients}
                />
                <RecipeStepsEditor steps={stepTexts} onSave={handleSaveSteps} />
              </>
            )}
          </div>

          {recipeId && (
            <ShareWithFriendsModal
              open={shareOpen}
              itemName={detail.name}
              itemLabel="recipe"
              onClose={() => setShareOpen(false)}
              onShared={(message) => setShareMessage(message)}
              onShare={(friendUserIds) => recipesApi.shareRecipe(recipeId, friendUserIds)}
            />
          )}
        </>
      ) : null}
    </div>
  );
}
