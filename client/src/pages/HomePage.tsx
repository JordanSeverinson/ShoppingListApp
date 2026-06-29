import { ChefHat, ShoppingBag, ShoppingCart } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import * as listsApi from "../api/lists";
import { HomeNavCard } from "../components/HomeNavCard";
import { RecentListCard } from "../components/RecentListCard";
import { USER_DISPLAY_NAME } from "../lib/userDisplayName";
import type { ListSummary } from "../types/list";

export function HomePage() {
  const [recentLists, setRecentLists] = useState<ListSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listsApi.fetchMyLists();
      setRecentLists(data.sharedLists.slice(0, 5));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load lists");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div className="mx-auto min-h-screen max-w-lg px-4 py-8 sm:px-6 sm:py-12">
      <header className="mb-8">
        <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
          Collaborative grocery lists
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl">
          Welcome back, {USER_DISPLAY_NAME}
        </h1>
        <p className="mt-2 text-muted">Organize your shopping and recipes</p>
      </header>

      <nav className="mb-10 space-y-3" aria-label="Main">
        <HomeNavCard
          to="/lists"
          icon={ShoppingBag}
          title="Shopping Lists"
          description="Create, share, and manage lists"
        />
        <HomeNavCard
          to="/recipes"
          icon={ChefHat}
          title="Recipes"
          description="Save meals and generate shopping lists"
        />
      </nav>

      <section>
        <div className="mb-4 flex items-center justify-between gap-3">
          <h2 className="text-lg font-semibold text-ink">Recent Lists</h2>
          <Link
            to="/lists"
            className="text-sm font-medium text-brand-700 hover:text-brand-600"
          >
            View all
          </Link>
        </div>

        {error && (
          <p className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </p>
        )}

        {loading ? (
          <p className="text-center text-sm text-muted">Loading lists…</p>
        ) : recentLists.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-border bg-white/60 px-6 py-10 text-center">
            <p className="text-muted">No active lists yet.</p>
            <Link
              to="/lists"
              className="mt-3 inline-block text-sm font-medium text-brand-700 hover:text-brand-600"
            >
              Create your first list
            </Link>
          </div>
        ) : (
          <div className="space-y-3">
            {recentLists.map((list) => (
              <RecentListCard key={list.id} list={list} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
