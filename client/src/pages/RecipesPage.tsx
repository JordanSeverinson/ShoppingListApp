import { ArrowLeft, ChefHat, Plus } from "lucide-react";
import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import * as recipesApi from "../api/recipes";
import { ConfirmModal } from "../components/ConfirmModal";
import { PendingRecipeShareCard } from "../components/PendingRecipeShareCard";
import { RecipeCard } from "../components/RecipeCard";
import { APP_NAME } from "../lib/appName";
import { clearDeleteQueue } from "../lib/deleteQueue";
import { RECIPE_TYPES, type RecipeType } from "../lib/recipeTypes";
import type { PendingRecipeShare, RecipeSummary } from "../types/recipe";

export function RecipesPage() {
  const navigate = useNavigate();
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [pendingShares, setPendingShares] = useState<PendingRecipeShare[]>([]);
  const [listLoading, setListLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [newRecipeName, setNewRecipeName] = useState("");
  const [newRecipeType, setNewRecipeType] = useState<RecipeType>("Main Course");
  const [typeFilter, setTypeFilter] = useState<RecipeType | "All">("All");
  const [searchQuery, setSearchQuery] = useState("");
  const [creating, setCreating] = useState(false);
  const [shareActionBusy, setShareActionBusy] = useState(false);
  const [recipeToDelete, setRecipeToDelete] = useState<RecipeSummary | null>(null);
  const [deleting, setDeleting] = useState(false);

  const loadRecipes = useCallback(async () => {
    setListLoading(true);
    setError(null);
    try {
      const summary = await recipesApi.fetchMyRecipes();
      setRecipes(summary.recipes);
      setPendingShares(summary.pendingShares);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load recipes");
    } finally {
      setListLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadRecipes();
  }, [loadRecipes]);

  useEffect(() => {
    if (!listLoading && window.location.hash === "#pending-shares") {
      document.getElementById("pending-shares")?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    }
  }, [listLoading, pendingShares.length]);

  useEffect(() => {
    if (!statusMessage) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setStatusMessage(null);
    }, 3000);

    return () => window.clearTimeout(timeoutId);
  }, [statusMessage]);

  const filteredRecipes = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    return recipes.filter((recipe) => {
      if (typeFilter !== "All" && recipe.recipeType !== typeFilter) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return recipe.name.toLowerCase().includes(normalizedQuery);
    });
  }, [recipes, typeFilter, searchQuery]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!newRecipeName.trim()) {
      return;
    }

    setCreating(true);
    setError(null);
    try {
      const createdRecipe = await recipesApi.createRecipe(
        newRecipeName.trim(),
        newRecipeType,
      );
      if (!createdRecipe.id) {
        throw new Error("Server did not return a recipe id.");
      }
      setNewRecipeName("");
      setNewRecipeType("Main Course");
      navigate(`/recipes/${createdRecipe.id}/edit`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create recipe");
    } finally {
      setCreating(false);
    }
  }

  async function handleRename(recipeId: string, name: string) {
    await recipesApi.renameRecipe(recipeId, name);
    setRecipes((current) =>
      current.map((recipe) => (recipe.id === recipeId ? { ...recipe, name } : recipe)),
    );
  }

  function requestDelete(recipe: RecipeSummary) {
    setRecipeToDelete(recipe);
  }

  async function confirmDelete() {
    if (!recipeToDelete) {
      return;
    }

    setDeleting(true);
    setError(null);
    try {
      await recipesApi.deleteRecipe(recipeToDelete.id);
      clearDeleteQueue(`recipe:${recipeToDelete.id}`);
      setRecipes((current) => current.filter((recipe) => recipe.id !== recipeToDelete.id));
      setRecipeToDelete(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not delete recipe");
    } finally {
      setDeleting(false);
    }
  }

  async function handleAcceptShare(share: PendingRecipeShare) {
    setShareActionBusy(true);
    setError(null);
    try {
      await recipesApi.acceptRecipeShare(share.id);
      setPendingShares((current) => current.filter((item) => item.id !== share.id));
      setStatusMessage(`You joined ${share.recipeName}.`);
      await loadRecipes();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not accept share");
    } finally {
      setShareActionBusy(false);
    }
  }

  async function handleDeclineShare(share: PendingRecipeShare) {
    setShareActionBusy(true);
    setError(null);
    try {
      await recipesApi.declineRecipeShare(share.id);
      setPendingShares((current) => current.filter((item) => item.id !== share.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not decline share");
    } finally {
      setShareActionBusy(false);
    }
  }

  return (
    <div className="mx-auto min-h-screen max-w-3xl px-4 py-8 sm:px-6 sm:py-12">
      <Link
        to="/"
        className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-brand-700"
      >
        <ArrowLeft className="h-4 w-4" aria-hidden />
        Home
      </Link>

      <header className="mb-10">
        <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <ChefHat className="h-3.5 w-3.5" aria-hidden />
          {APP_NAME}
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl">Your Recipes</h1>
        <p className="mt-2 max-w-xl text-muted">
          Save meals and add their ingredients to any shopping list.
        </p>
      </header>

      <section className="mb-10">
        <form
          onSubmit={(event) => void handleCreate(event)}
          className="rounded-2xl border border-border bg-white p-4 shadow-sm"
        >
          <h2 className="mb-3 flex items-center gap-2 font-semibold text-ink">
            <Plus className="h-5 w-5 text-brand-600" aria-hidden />
            Recipe
          </h2>
          <input
            value={newRecipeName}
            onChange={(event) => setNewRecipeName(event.target.value)}
            placeholder="e.g. Weeknight pasta"
            className="mb-3 w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <label className="mb-3 block">
            <span className="mb-1.5 block text-sm font-medium text-ink">Type</span>
            <select
              value={newRecipeType}
              onChange={(event) => setNewRecipeType(event.target.value as RecipeType)}
              className="w-full rounded-xl border border-border bg-white px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
            >
              {RECIPE_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <button
            type="submit"
            disabled={creating || !newRecipeName.trim()}
            className="w-full rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
          >
            {creating ? "Creating…" : "Create recipe"}
          </button>
        </form>
      </section>

      {statusMessage && (
        <p className="mb-6 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
          {statusMessage}
        </p>
      )}

      {error && (
        <p className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {listLoading ? (
        <p className="text-center text-muted">Loading recipes…</p>
      ) : (
        <>
          {pendingShares.length > 0 && (
            <section id="pending-shares" className="mb-10 scroll-mt-8">
              <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold text-ink">Pending shared recipes</h2>
                  <p className="mt-1 text-sm text-muted">
                    Accept an invitation before you can collaborate on a friend&apos;s recipe.
                  </p>
                </div>
              </div>
              <div className="space-y-4">
                {pendingShares.map((share) => (
                  <PendingRecipeShareCard
                    key={share.id}
                    share={share}
                    busy={shareActionBusy}
                    onAccept={handleAcceptShare}
                    onDecline={handleDeclineShare}
                  />
                ))}
              </div>
            </section>
          )}

          {recipes.length === 0 ? (
            <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
              No recipes yet. Create one or accept a shared recipe invitation.
            </p>
          ) : (
            <section>
              <div className="mb-4 flex flex-col gap-3">
                <h2 className="text-lg font-semibold text-ink">Saved recipes</h2>
                <div className="flex flex-col gap-3 sm:flex-row">
                  <label className="min-w-0 flex-1">
                    <span className="mb-1.5 block text-sm font-medium text-ink">Search</span>
                    <input
                      type="search"
                      value={searchQuery}
                      onChange={(event) => setSearchQuery(event.target.value)}
                      placeholder="Search recipes…"
                      className="w-full rounded-xl border border-border bg-white px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
                    />
                  </label>
                  <label className="sm:w-56">
                    <span className="mb-1.5 block text-sm font-medium text-ink">Filter by type</span>
                    <select
                      value={typeFilter}
                      onChange={(event) =>
                        setTypeFilter(event.target.value as RecipeType | "All")
                      }
                      className="w-full rounded-xl border border-border bg-white px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
                    >
                      <option value="All">All types</option>
                      {RECIPE_TYPES.map((type) => (
                        <option key={type} value={type}>
                          {type}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
              </div>

              {filteredRecipes.length === 0 ? (
                <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
                  No recipes match your search.
                </p>
              ) : (
                <div className="space-y-4">
                  {filteredRecipes.map((recipe) => (
                    <RecipeCard
                      key={recipe.id}
                      recipe={recipe}
                      onRename={handleRename}
                      onDelete={requestDelete}
                      onShared={setStatusMessage}
                    />
                  ))}
                </div>
              )}
            </section>
          )}
        </>
      )}

      <ConfirmModal
        open={recipeToDelete !== null}
        message={
          recipeToDelete
            ? `Are you sure you want to delete ${recipeToDelete.name}?`
            : ""
        }
        confirmLabel="Yes"
        cancelLabel="No"
        busy={deleting}
        onConfirm={() => void confirmDelete()}
        onCancel={() => {
          if (!deleting) {
            setRecipeToDelete(null);
          }
        }}
      />
    </div>
  );
}
