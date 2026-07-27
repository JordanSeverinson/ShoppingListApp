import { ArrowLeft, ChefHat, Eye, Pencil, UserPlus } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { Link, Navigate, useNavigate, useParams } from "react-router";
import * as recipesApi from "../api/recipes";
import { EditableName } from "../components/EditableName";
import { RecipeImageUploader } from "../components/RecipeImageUploader";
import { RecipeReadOnlyView } from "../components/RecipeReadOnlyView";
import { RecipeSectionsEditor } from "../components/RecipeSectionsEditor";
import { RecipeStepsEditor } from "../components/RecipeStepsEditor";
import { ShareWithFriendsModal } from "../components/ShareWithFriendsModal";
import { enqueueDelete, flushDeletesNow, hasPendingDeletes } from "../lib/deleteQueue";
import { RECIPE_TYPES, type RecipeType } from "../lib/recipeTypes";
import { sectionLabelFromIngredient } from "../lib/recipeSections";
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

function cookingStepsFromRecipe(recipe: RecipeDetail): string[] {
  return recipe.content.recipe.cookingSteps.length
    ? recipe.content.recipe.cookingSteps
    : recipe.steps.map((step) => step.text);
}

export function RecipeDetailPage({ readOnly = false }: { readOnly?: boolean }) {
  const navigate = useNavigate();
  const { recipeId } = useParams<{ recipeId: string }>();
  const [recipe, setRecipe] = useState<RecipeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [shareOpen, setShareOpen] = useState(false);
  const [shareStatusMessage, setShareStatusMessage] = useState<string | null>(null);
  const deleteQueueKey = recipeId ? `recipe:${recipeId}` : null;

  const patchIngredients = useCallback(
    (mutate: (ingredients: RecipeIngredient[]) => RecipeIngredient[]) => {
      setRecipe((previous) =>
        previous ? { ...previous, ingredients: mutate(previous.ingredients) } : previous,
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
      const fetchedRecipe = await recipesApi.fetchRecipe(recipeId);
      setRecipe(fetchedRecipe);
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
    if (!shareStatusMessage) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setShareStatusMessage(null);
    }, 3000);

    return () => window.clearTimeout(timeoutId);
  }, [shareStatusMessage]);

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
    if (!deleteQueueKey || readOnly) {
      return;
    }

    return () => {
      void flushDeletesNow(deleteQueueKey, { keepalive: true });
    };
  }, [deleteQueueKey, readOnly]);

  const syncRecipeContent = useCallback(
    async (ingredients: RecipeIngredient[], cookingSteps: string[]) => {
      if (!recipeId) {
        return;
      }

      const content = buildRecipeContentDocument(ingredients, cookingSteps);
      try {
        const savedContent = await recipesApi.saveRecipeContent(recipeId, content);
        setRecipe((previous) =>
          previous ? { ...previous, content: savedContent } : previous,
        );
      } catch {
        // Ingredient edits already saved; content sync can retry on next mutation.
      }
    },
    [recipeId],
  );

  async function leaveEditPage(destination: "view" | "list" = "view") {
    if (deleteQueueKey && hasPendingDeletes(deleteQueueKey)) {
      await flushDeletesNow(deleteQueueKey);
    }

    if (recipeId) {
      try {
        const fetchedRecipe = await recipesApi.fetchRecipe(recipeId);
        const cookingSteps = cookingStepsFromRecipe(fetchedRecipe);
        const content = buildRecipeContentDocument(
          fetchedRecipe.ingredients,
          cookingSteps,
        );
        const savedContent = await recipesApi.saveRecipeContent(recipeId, content);
        setRecipe({ ...fetchedRecipe, content: savedContent });
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
    setRecipe((previous) => (previous ? { ...previous, name: updated.name } : previous));
  }

  async function handleRecipeTypeChange(recipeType: RecipeType) {
    if (!recipeId) {
      return;
    }
    const updated = await recipesApi.updateRecipe(recipeId, { recipeType });
    setRecipe((previous) =>
      previous ? { ...previous, recipeType: updated.recipeType } : previous,
    );
  }

  async function handleAddIngredient(payload: CreateRecipeIngredientPayload) {
    if (!recipeId) {
      return;
    }
    const createdIngredient = await recipesApi.createRecipeIngredient(recipeId, payload);
    let cookingSteps: string[] = [];
    let nextIngredients: RecipeIngredient[] = [];
    setRecipe((previous) => {
      if (!previous) {
        return previous;
      }
      cookingSteps = cookingStepsFromRecipe(previous);
      nextIngredients = sortIngredients([...previous.ingredients, createdIngredient]);
      return { ...previous, ingredients: nextIngredients };
    });
    await syncRecipeContent(nextIngredients, cookingSteps);
  }

  async function handleUpdateIngredient(
    ingredientId: string,
    payload: UpdateRecipeIngredientPayload,
  ) {
    if (!recipeId) {
      return;
    }
    const updatedIngredient = await recipesApi.updateRecipeIngredient(
      recipeId,
      ingredientId,
      payload,
    );
    let cookingSteps: string[] = [];
    let nextIngredients: RecipeIngredient[] = [];
    setRecipe((previous) => {
      if (!previous) {
        return previous;
      }
      cookingSteps = cookingStepsFromRecipe(previous);
      nextIngredients = sortIngredients(
        previous.ingredients.map((item) =>
          item.id === ingredientId ? updatedIngredient : item,
        ),
      );
      return { ...previous, ingredients: nextIngredients };
    });
    await syncRecipeContent(nextIngredients, cookingSteps);
  }

  async function handleRenameSection(from: string, to: string) {
    if (!recipeId) {
      return;
    }
    await recipesApi.renameRecipeSection(recipeId, from, to);
    let cookingSteps: string[] = [];
    let nextIngredients: RecipeIngredient[] = [];
    setRecipe((previous) => {
      if (!previous) {
        return previous;
      }
      cookingSteps = cookingStepsFromRecipe(previous);
      nextIngredients = sortIngredients(
        previous.ingredients.map((item) =>
          sectionLabelFromIngredient(item) === from ? { ...item, section: to } : item,
        ),
      );
      return { ...previous, ingredients: nextIngredients };
    });
    await syncRecipeContent(nextIngredients, cookingSteps);
  }

  async function handleSaveSteps(steps: string[]) {
    if (!recipeId) {
      return;
    }
    const savedSteps = await recipesApi.saveRecipeSteps(recipeId, steps);
    let ingredients: RecipeIngredient[] = [];
    setRecipe((previous) => {
      if (!previous) {
        return previous;
      }
      ingredients = previous.ingredients;
      return { ...previous, steps: savedSteps };
    });
    await syncRecipeContent(ingredients, steps);
  }

  const handleRemoveIngredient = useCallback(
    (ingredientId: string) => {
      if (!deleteQueueKey) {
        return;
      }

      patchIngredients((ingredients) => {
        const removed = ingredients.find((item) => item.id === ingredientId);
        if (!removed) {
          return ingredients;
        }

        enqueueDelete(deleteQueueKey, removed, flushRecipeDeletes, restoreIngredients);
        return ingredients.filter((item) => item.id !== ingredientId);
      });
    },
    [deleteQueueKey, flushRecipeDeletes, patchIngredients, restoreIngredients],
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

  if (!readOnly && recipe && !recipe.isOwner) {
    return <Navigate to={`/recipes/${recipeId}`} replace />;
  }

  const cookingSteps = recipe ? cookingStepsFromRecipe(recipe) : [];

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
        All recipes
      </button>

      {error && recipe && (
        <p className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {loading && !recipe ? (
        <p className="text-center text-muted">Loading recipe…</p>
      ) : error && !recipe ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-6 text-center text-red-700">
          {error}
        </div>
      ) : recipe ? (
        <>
          <header className="mb-8">
            <div className="mb-3 flex flex-wrap items-center gap-2">
              <div className="inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
                <ChefHat className="h-3.5 w-3.5" aria-hidden />
                {recipe.isOwner ? "Your Recipe" : "Shared Recipe"}
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
              {readOnly && recipe.isOwner && (
                <Link
                  to={`/recipes/${recipeId}/edit`}
                  className="inline-flex items-center gap-2 rounded-full border border-border bg-white px-4 py-1.5 text-xs font-semibold text-brand-700 transition hover:border-brand-300 hover:bg-brand-50"
                >
                  <Pencil className="h-3.5 w-3.5" aria-hidden />
                  Edit Recipe
                </Link>
              )}
            </div>

            <EditableName
              name={recipe.name}
              size="lg"
              readOnly={readOnly}
              onSave={handleRename}
            />

            {readOnly ? (
              recipe.recipeType ? (
                <p className="mt-3 text-sm font-medium text-muted">{recipe.recipeType}</p>
              ) : null
            ) : (
              <label className="mt-4 block max-w-xs">
                <span className="mb-1.5 block text-sm font-medium text-ink">Type</span>
                <select
                  value={recipe.recipeType ?? ""}
                  onChange={(event) => {
                    const value = event.target.value;
                    if (value) {
                      void handleRecipeTypeChange(value as RecipeType);
                    }
                  }}
                  className="w-full rounded-xl border border-border bg-white px-4 py-2.5 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
                >
                  {!recipe.recipeType && <option value="">Select a type</option>}
                  {RECIPE_TYPES.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </select>
              </label>
            )}

            {recipe.isOwner && (
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

            {shareStatusMessage && (
              <p className="mt-3 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
                {shareStatusMessage}
              </p>
            )}
          </header>

          <div className="space-y-8">
            {readOnly ? (
              <RecipeReadOnlyView
                recipeName={recipe.name}
                content={recipe.content}
                ingredients={recipe.ingredients}
                steps={cookingSteps}
              />
            ) : (
              <>
                <RecipeImageUploader recipeId={recipeId} onImported={() => void refresh()} />
                <RecipeSectionsEditor
                  ingredients={recipe.ingredients}
                  onAdd={handleAddIngredient}
                  onUpdate={handleUpdateIngredient}
                  onRemove={handleRemoveIngredient}
                  onRenameSection={handleRenameSection}
                  onDeleteSectionIngredients={handleDeleteSectionIngredients}
                />
                <RecipeStepsEditor steps={cookingSteps} onSave={handleSaveSteps} />
              </>
            )}
          </div>

          {recipeId && (
            <ShareWithFriendsModal
              open={shareOpen}
              itemName={recipe.name}
              itemLabel="recipe"
              onClose={() => setShareOpen(false)}
              onShared={(message) => setShareStatusMessage(message)}
              onShare={(friendUserIds) => recipesApi.shareRecipe(recipeId, friendUserIds)}
            />
          )}
        </>
      ) : null}
    </div>
  );
}
