import { Link2, Plus, ShoppingCart } from "lucide-react";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import * as listsApi from "../api/lists";
import { ListCard } from "../components/ListCard";
import type { ListSummary } from "../types/list";

export function LandingPage() {
  const navigate = useNavigate();
  const [sharedLists, setSharedLists] = useState<ListSummary[]>([]);
  const [archivedLists, setArchivedLists] = useState<ListSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newListName, setNewListName] = useState("");
  const [joinCode, setJoinCode] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listsApi.fetchMyLists();
      setSharedLists(data.sharedLists);
      setArchivedLists(data.archivedLists);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load lists");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!newListName.trim()) {
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const created = await listsApi.createList(newListName.trim());
      if (!created.id) {
        throw new Error("Server did not return a list id.");
      }
      setNewListName("");
      navigate(`/lists/${created.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create list");
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
      const joined = await listsApi.joinList(code);
      setJoinCode("");
      navigate(`/lists/${joined.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not join list");
    } finally {
      setBusy(false);
    }
  }

  async function handleRename(listId: string, name: string) {
    await listsApi.renameList(listId, name);
    await load();
  }

  async function handleArchive(listId: string) {
    setError(null);
    try {
      await listsApi.archiveList(listId);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not archive list");
    }
  }

  return (
    <div className="mx-auto min-h-screen max-w-3xl px-4 py-8 sm:px-6 sm:py-12">
      <header className="mb-10">
        <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
          Collaborative grocery lists
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl">
          Your lists
        </h1>
        <p className="mt-2 max-w-xl text-muted">
          Manage active shared lists with your household.
        </p>
      </header>

      <section className="mb-10 grid gap-4 sm:grid-cols-2">
        <form
          onSubmit={(event) => void handleCreate(event)}
          className="rounded-2xl border border-border bg-white p-4 shadow-sm"
        >
          <h2 className="mb-3 flex items-center gap-2 font-semibold text-ink">
            <Plus className="h-5 w-5 text-brand-600" aria-hidden />
            New shared list
          </h2>
          <input
            value={newListName}
            onChange={(event) => setNewListName(event.target.value)}
            placeholder="e.g. Weekend groceries"
            className="mb-3 w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <button
            type="submit"
            disabled={busy || !newListName.trim()}
            className="w-full rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
          >
            {busy ? "Creating…" : "Create list"}
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
            Join list
          </button>
        </form>
      </section>

      {error && (
        <p className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {loading ? (
        <p className="text-center text-muted">Loading lists…</p>
      ) : (
        <>
          <section className="mb-10">
            <h2 className="mb-4 text-lg font-semibold text-ink">Shared grocery lists</h2>
            {sharedLists.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
                No active lists yet. Create one or join with a share code.
              </p>
            ) : (
              <div className="space-y-4">
                {sharedLists.map((list) => (
                  <ListCard
                    key={list.id}
                    list={list}
                    onRename={handleRename}
                    showArchiveAction
                    onArchive={handleArchive}
                  />
                ))}
              </div>
            )}
          </section>

          <section>
            <h2 className="mb-4 text-lg font-semibold text-ink">Archived grocery lists</h2>
            <p className="mb-4 text-sm text-muted">
              View past lists or rename them for your records.
            </p>
            {archivedLists.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
                No archived lists yet. Archive a shared list when you are done shopping.
              </p>
            ) : (
              <div className="space-y-4">
                {archivedLists.map((list) => (
                  <ListCard key={list.id} list={list} onRename={handleRename} />
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
