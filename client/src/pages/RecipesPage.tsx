import { ArrowLeft, ChefHat, Link2, Plus } from "lucide-react";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import * as recipesApi from "../api/recipes";
import { RecipeCard } from "../components/RecipeCard";
import type { RecipeSummary } from "../types/recipe";

export function RecipesPage() {
  const navigate = useNavigate();
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [listLoading, setListLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newRecipeName, setNewRecipeName] = useState("");
  const [joinCode, setJoinCode] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setListLoading(true);
    setError(null);
    try {
      const data = await recipesApi.fetchMyRecipes();
      setRecipes(data.recipes);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load recipes");
    } finally {
      setListLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!newRecipeName.trim()) {
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const created = await recipesApi.createRecipe(newRecipeName.trim());
      if (!created.id) {
        throw new Error("Server did not return a recipe id.");
      }
      setNewRecipeName("");
      navigate(`/recipes/${created.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create recipe");
    } finally {
      setBusy(false);
    }
  }

  async function handleJoin(event: FormEvent) {
    event.preventDefault();
    const code = joinCode.trim().toUpperCase();
    if (code.length !== 11) {
      setError("Share code must be exactly 11 characters.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const joined = await recipesApi.joinRecipe(code);
      setJoinCode("");
      navigate(`/recipes/${joined.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not join recipe");
    } finally {
      setBusy(false);
    }
  }

  async function handleRename(recipeId: string, name: string) {
    await recipesApi.renameRecipe(recipeId, name);
    setRecipes((current) =>
      current.map((recipe) => (recipe.id === recipeId ? { ...recipe, name } : recipe)),
    );
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
          Shared recipes
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl">Your recipes</h1>
        <p className="mt-2 max-w-xl text-muted">
          Save meals and add their ingredients to any shopping list.
        </p>
      </header>

      <section className="mb-10 grid gap-4 sm:grid-cols-2">
        <form
          onSubmit={(event) => void handleCreate(event)}
          className="rounded-2xl border border-border bg-white p-4 shadow-sm"
        >
          <h2 className="mb-3 flex items-center gap-2 font-semibold text-ink">
            <Plus className="h-5 w-5 text-brand-600" aria-hidden />
            New recipe
          </h2>
          <input
            value={newRecipeName}
            onChange={(event) => setNewRecipeName(event.target.value)}
            placeholder="e.g. Weeknight pasta"
            className="mb-3 w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <button
            type="submit"
            disabled={busy || !newRecipeName.trim()}
            className="w-full rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
          >
            {busy ? "Creating…" : "Create recipe"}
          </button>
        </form>

        <form
          onSubmit={(event) => void handleJoin(event)}
          className="rounded-2xl border border-border bg-white p-4 shadow-sm"
        >
          <h2 className="mb-3 flex items-center gap-2 font-semibold text-ink">
            <Link2 className="h-5 w-5 text-brand-600" aria-hidden />
            Join with share code
          </h2>
          <input
            value={joinCode}
            onChange={(event) => setJoinCode(event.target.value.toUpperCase())}
            placeholder="11-character code"
            maxLength={11}
            className="mb-3 w-full rounded-xl border border-border px-4 py-2.5 font-mono tracking-widest uppercase outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <button
            type="submit"
            disabled={busy || joinCode.trim().length !== 11}
            className="w-full rounded-xl border border-brand-600 py-2.5 font-medium text-brand-700 hover:bg-brand-50 disabled:opacity-50"
          >
            Join recipe
          </button>
        </form>
      </section>

      {error && (
        <p className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {listLoading ? (
        <p className="text-center text-muted">Loading recipes…</p>
      ) : recipes.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
          No recipes yet. Create one or join with a share code.
        </p>
      ) : (
        <section>
          <h2 className="mb-4 text-lg font-semibold text-ink">Saved recipes</h2>
          <div className="space-y-4">
            {recipes.map((recipe) => (
              <RecipeCard key={recipe.id} recipe={recipe} onRename={handleRename} />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
