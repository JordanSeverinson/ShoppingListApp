import { ChefHat, Loader2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import * as recipesApi from "../api/recipes";
import { useShoppingList } from "../context/ShoppingListContext";
import type { RecipeSummary } from "../types/recipe";

export function RecipeImporter() {
  const { importRecipe } = useShoppingList();
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [selectedId, setSelectedId] = useState("");
  const [loading, setLoading] = useState(true);
  const [importing, setImporting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const data = await recipesApi.fetchMyRecipes();
        if (!cancelled) {
          const withIngredients = data.recipes.filter((recipe) => recipe.ingredientCount > 0);
          setRecipes(withIngredients);
          if (withIngredients.length > 0) {
            setSelectedId((current) =>
              current && withIngredients.some((recipe) => recipe.id === current)
                ? current
                : withIngredients[0].id,
            );
          }
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load recipes");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  async function handleImport() {
    if (!selectedId) {
      return;
    }

    setImporting(true);
    setError(null);
    setMessage(null);

    try {
      const result = await importRecipe(selectedId);
      setMessage(result.message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not import recipe");
    } finally {
      setImporting(false);
    }
  }

  return (
    <section className="rounded-2xl border border-border bg-white p-4 shadow-sm">
      <div className="mb-3 flex items-center gap-2">
        <ChefHat className="h-5 w-5 text-brand-600" aria-hidden />
        <h2 className="font-semibold text-ink">Add from Recipe</h2>
      </div>
      <p className="mb-4 text-sm text-muted">
        Pick a saved recipe to add its ingredients to this list for everyone connected in real
        time.
      </p>

      {loading ? (
        <p className="text-sm text-muted">Loading Recipes…</p>
      ) : recipes.length === 0 ? (
        <p className="rounded-xl border border-dashed border-border bg-stone-50/80 px-4 py-6 text-center text-sm text-muted">
          No recipes with ingredients yet.{" "}
          <Link to="/recipes" className="font-medium text-brand-700 hover:text-brand-600">
            Create a recipe
          </Link>{" "}
          first.
        </p>
      ) : (
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <select
            value={selectedId}
            onChange={(event) => setSelectedId(event.target.value)}
            className="min-w-0 flex-1 rounded-xl border border-border bg-white px-4 py-2.5 text-ink outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
            aria-label="Select recipe"
          >
            {recipes.map((recipe) => (
              <option key={recipe.id} value={recipe.id}>
                {recipe.name} ({recipe.ingredientCount} ingredients)
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={() => void handleImport()}
            disabled={importing || !selectedId}
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-brand-600 px-5 py-2.5 font-medium text-white transition hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {importing ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
                Adding…
              </>
            ) : (
              "Add to list"
            )}
          </button>
        </div>
      )}

      {message && (
        <p className="mt-3 text-sm font-medium text-brand-700" role="status">
          {message}
        </p>
      )}
      {error && (
        <p className="mt-3 text-sm text-red-600" role="alert">
          {error}
        </p>
      )}
    </section>
  );
}
