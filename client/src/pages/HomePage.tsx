import { Bell, ChefHat, LogIn, LogOut, ShoppingBag, ShoppingCart, UserCircle, Users } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import * as listsApi from "../api/lists";
import * as recipesApi from "../api/recipes";
import { useAuth } from "../context/AuthContext";
import { HomeNavCard } from "../components/HomeNavCard";
import { RecentListCard } from "../components/RecentListCard";
import { APP_NAME } from "../lib/appName";
import type { ListSummary } from "../types/list";

export function HomePage() {
  const navigate = useNavigate();
  const { user, isAuthenticated, loading: authLoading, logout } = useAuth();
  const [recentLists, setRecentLists] = useState<ListSummary[]>([]);
  const [pendingListShareCount, setPendingListShareCount] = useState(0);
  const [pendingRecipeShareCount, setPendingRecipeShareCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!isAuthenticated) {
      setRecentLists([]);
      setPendingListShareCount(0);
      setPendingRecipeShareCount(0);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const [listsData, recipesData] = await Promise.all([
        listsApi.fetchMyLists(),
        recipesApi.fetchMyRecipes(),
      ]);
      setRecentLists(listsData.activeLists.slice(0, 5));
      setPendingListShareCount(listsData.pendingShares.length);
      setPendingRecipeShareCount(recipesData.pendingShares.length);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load lists");
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated]);
  useEffect(() => {
    if (!authLoading) {
      void load();
    }
  }, [authLoading, load]);

  const greetingName = user?.displayName ?? "there";

  return (
    <div className="mx-auto min-h-screen max-w-lg px-4 py-8 sm:px-6 sm:py-12">
      <header className="mb-8">
        <div className="mb-4 flex items-start justify-between gap-4">
          <div className="inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
            <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
            {APP_NAME}
          </div>

          <div className="flex shrink-0 items-center gap-2">
            {isAuthenticated ? (
              <>
                <Link
                  to="/profile"
                  className="inline-flex items-center gap-1.5 rounded-xl border border-border bg-white px-3 py-2 text-sm font-medium text-ink transition hover:border-brand-300 hover:bg-brand-50"
                >
                  <UserCircle className="h-4 w-4" aria-hidden />
                  Profile
                </Link>
                <button
                  type="button"
                  onClick={() => {
                    void logout().then(() => navigate("/"));
                  }}
                  className="inline-flex items-center gap-1.5 rounded-xl border border-border bg-white px-3 py-2 text-sm font-medium text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-700"
                >
                  <LogOut className="h-4 w-4" aria-hidden />
                  Log out
                </button>
              </>
            ) : (
              <Link
                to="/login"
                className="inline-flex items-center gap-1.5 rounded-xl bg-brand-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-brand-700"
              >
                <LogIn className="h-4 w-4" aria-hidden />
                Log in
              </Link>
            )}
          </div>
        </div>

        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl">
          {isAuthenticated ? `Welcome back, ${greetingName}` : "Welcome"}
        </h1>
        <p className="mt-2 text-muted">
          {isAuthenticated
            ? "Organize your shopping and recipes"
            : "Sign in to access your lists, recipes, and friends."}
        </p>
      </header>

      {isAuthenticated && pendingListShareCount > 0 && (
        <Link
          to="/lists#pending-shares"
          className="mb-6 flex items-center justify-between gap-3 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-4 text-amber-950 shadow-sm transition hover:border-amber-300 hover:bg-amber-100/80"
        >
          <span className="flex items-center gap-3">
            <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-amber-100 text-amber-800">
              <Bell className="h-5 w-5" aria-hidden />
            </span>
            <span>
              <span className="block font-semibold text-ink">
                {pendingListShareCount === 1
                  ? "1 pending list invitation"
                  : `${pendingListShareCount} pending list invitations`}
              </span>
              <span className="block text-sm text-amber-900/80">
                Review and accept shared grocery lists
              </span>
            </span>
          </span>
          <span className="shrink-0 rounded-xl bg-brand-600 px-3 py-2 text-sm font-medium text-white">
            Review
          </span>
        </Link>
      )}

      {isAuthenticated && pendingRecipeShareCount > 0 && (
        <Link
          to="/recipes#pending-shares"
          className="mb-6 flex items-center justify-between gap-3 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-4 text-amber-950 shadow-sm transition hover:border-amber-300 hover:bg-amber-100/80"
        >
          <span className="flex items-center gap-3">
            <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-amber-100 text-amber-800">
              <Bell className="h-5 w-5" aria-hidden />
            </span>
            <span>
              <span className="block font-semibold text-ink">
                {pendingRecipeShareCount === 1
                  ? "1 pending recipe invitation"
                  : `${pendingRecipeShareCount} pending recipe invitations`}
              </span>
              <span className="block text-sm text-amber-900/80">
                Review and accept shared recipes
              </span>
            </span>
          </span>
          <span className="shrink-0 rounded-xl bg-brand-600 px-3 py-2 text-sm font-medium text-white">
            Review
          </span>
        </Link>
      )}

      {isAuthenticated && (        <nav className="mb-10 space-y-3" aria-label="Main">
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
          <HomeNavCard
            to="/friends"
            icon={Users}
            title="Friends"
            description="Add friends by email, phone, or code"
          />
        </nav>
      )}

      {isAuthenticated && (
        <section>
          <div className="mb-4 flex items-center justify-between gap-3">
            <h2 className="text-lg font-semibold text-ink">Shopping Lists</h2>
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

          {authLoading || loading ? (
            <p className="text-center text-sm text-muted">Loading lists…</p>
          ) : recentLists.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-border bg-white/60 px-6 py-10 text-center">
              <p className="text-muted">No active lists.</p>
              <Link
                to="/lists"
                className="mt-3 inline-block text-sm font-medium text-brand-700 hover:text-brand-600"
              >
                Create a new list
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
      )}
    </div>
  );
}
