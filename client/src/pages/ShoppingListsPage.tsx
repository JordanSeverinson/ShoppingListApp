import { ArrowLeft, Plus, ShoppingCart } from "lucide-react";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import * as listsApi from "../api/lists";
import { ConfirmModal } from "../components/ConfirmModal";
import { ListCard } from "../components/ListCard";
import { PendingListShareCard } from "../components/PendingListShareCard";
import { APP_NAME } from "../lib/appName";
import { clearDeleteQueue } from "../lib/deleteQueue";
import type { ListSummary, PendingListShare } from "../types/list";

export function ShoppingListsPage() {
  const navigate = useNavigate();
  const [activeLists, setActiveLists] = useState<ListSummary[]>([]);
  const [archivedLists, setArchivedLists] = useState<ListSummary[]>([]);
  const [pendingShares, setPendingShares] = useState<PendingListShare[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [newListName, setNewListName] = useState("");
  const [busy, setBusy] = useState(false);
  const [shareBusy, setShareBusy] = useState(false);
  const [listToDelete, setListToDelete] = useState<ListSummary | null>(null);
  const [listToArchive, setListToArchive] = useState<ListSummary | null>(null);
  const [listToLeave, setListToLeave] = useState<ListSummary | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [archiving, setArchiving] = useState(false);
  const [leaving, setLeaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listsApi.fetchMyLists();
      setActiveLists(data.activeLists);
      setArchivedLists(data.archivedLists);
      setPendingShares(data.pendingShares);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load lists");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!loading && window.location.hash === "#pending-shares") {
      document.getElementById("pending-shares")?.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }, [loading, pendingShares.length]);

  useEffect(() => {
    if (!message) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setMessage(null);
    }, 3000);

    return () => window.clearTimeout(timeoutId);
  }, [message]);

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

  async function handleRename(listId: string, name: string) {
    await listsApi.renameList(listId, name);
    await load();
  }

  function requestArchive(list: ListSummary) {
    setListToArchive(list);
  }

  async function confirmArchive() {
    if (!listToArchive) {
      return;
    }

    setArchiving(true);
    setError(null);
    try {
      await listsApi.archiveList(listToArchive.id);
      setListToArchive(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not archive list");
    } finally {
      setArchiving(false);
    }
  }

  function requestDelete(list: ListSummary) {
    setListToDelete(list);
  }

  function requestLeave(list: ListSummary) {
    setListToLeave(list);
  }

  async function confirmLeave() {
    if (!listToLeave) {
      return;
    }

    setLeaving(true);
    setError(null);
    try {
      await listsApi.leaveList(listToLeave.id);
      clearDeleteQueue(`list:${listToLeave.id}`);
      setActiveLists((current) => current.filter((list) => list.id !== listToLeave.id));
      setArchivedLists((current) => current.filter((list) => list.id !== listToLeave.id));
      setListToLeave(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not leave list");
    } finally {
      setLeaving(false);
    }
  }

  async function confirmDelete() {
    if (!listToDelete) {
      return;
    }

    setDeleting(true);
    setError(null);
    try {
      await listsApi.deleteList(listToDelete.id);
      clearDeleteQueue(`list:${listToDelete.id}`);
      setActiveLists((current) => current.filter((list) => list.id !== listToDelete.id));
      setArchivedLists((current) => current.filter((list) => list.id !== listToDelete.id));
      setListToDelete(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not delete list");
    } finally {
      setDeleting(false);
    }
  }

  async function handleAcceptShare(share: PendingListShare) {
    setShareBusy(true);
    setError(null);
    try {
      await listsApi.acceptListShare(share.id);
      setPendingShares((current) => current.filter((item) => item.id !== share.id));
      setMessage(`You joined ${share.listName}.`);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not accept share");
    } finally {
      setShareBusy(false);
    }
  }

  async function handleDeclineShare(share: PendingListShare) {
    setShareBusy(true);
    setError(null);
    try {
      await listsApi.declineListShare(share.id);
      setPendingShares((current) => current.filter((item) => item.id !== share.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not decline share");
    } finally {
      setShareBusy(false);
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
          <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
          {APP_NAME}
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl">
          Your Lists
        </h1>
        <p className="mt-2 max-w-xl text-muted">
          Manage active lists with your household.
        </p>
      </header>

      <section className="mb-10">
        <form
          onSubmit={(event) => void handleCreate(event)}
          className="rounded-2xl border border-border bg-white p-4 shadow-sm"
        >
          <h2 className="mb-3 flex items-center gap-2 font-semibold text-ink">
            <Plus className="h-5 w-5 text-brand-600" aria-hidden />
            New Active List
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
      </section>

      {message && (
        <p className="mb-6 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
          {message}
        </p>
      )}

      {error && (
        <p className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {loading ? (
        <p className="text-center text-muted">Loading lists…</p>
      ) : (
        <>
          {pendingShares.length > 0 && (
            <section id="pending-shares" className="mb-10 scroll-mt-8">
              <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold text-ink">Pending shared lists</h2>
                  <p className="mt-1 text-sm text-muted">
                    Accept an invitation before you can collaborate on a friend&apos;s list.
                  </p>
                </div>
                <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-900">
                  {pendingShares.length} waiting
                </span>
              </div>

              <div className="space-y-4">
                {pendingShares.map((share) => (
                  <PendingListShareCard
                    key={share.id}
                    share={share}
                    busy={shareBusy}
                    onAccept={(item) => void handleAcceptShare(item)}
                    onDecline={(item) => void handleDeclineShare(item)}
                  />
                ))}
              </div>
            </section>
          )}

          <section className="mb-10">
            <h2 className="mb-4 text-lg font-semibold text-ink">Active Grocery Lists</h2>
            {activeLists.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
                No active lists. Create one to get started.
              </p>
            ) : (
              <div className="space-y-4">
                {activeLists.map((list) => (
                  <ListCard
                    key={list.id}
                    list={list}
                    onRename={handleRename}
                    showArchiveAction
                    onArchive={requestArchive}
                    onDelete={requestDelete}
                    onLeave={requestLeave}
                    onShared={(text) => setMessage(text)}
                  />
                ))}
              </div>
            )}
          </section>

          <section>
            <h2 className="mb-4 text-lg font-semibold text-ink">Archived Grocery Lists</h2>
            <p className="mb-4 text-sm text-muted">
              View past lists or rename them for your records.
            </p>
            {archivedLists.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
                No archived lists yet. Archive an active list when you are done shopping.
              </p>
            ) : (
              <div className="space-y-4">
                {archivedLists.map((list) => (
                  <ListCard
                    key={list.id}
                    list={list}
                    onRename={handleRename}
                    onDelete={requestDelete}
                    onLeave={requestLeave}
                  />
                ))}
              </div>
            )}
          </section>
        </>
      )}

      <ConfirmModal
        open={listToLeave !== null}
        message="Are you sure you want to remove yourself as a shared user on this list?"
        confirmLabel="Leave List"
        cancelLabel="Cancel"
        busy={leaving}
        busyLabel="Leaving…"
        onConfirm={() => void confirmLeave()}
        onCancel={() => {
          if (!leaving) {
            setListToLeave(null);
          }
        }}
      />

      <ConfirmModal
        open={listToArchive !== null}
        message={
          listToArchive
            ? `Archive ${listToArchive.name}? It will move to your archived lists and can no longer be edited.`
            : ""
        }
        confirmLabel="Archive"
        cancelLabel="Cancel"
        busy={archiving}
        busyLabel="Archiving…"
        confirmClassName="rounded-xl bg-amber-600 px-4 py-2.5 font-medium text-white transition hover:bg-amber-700 disabled:opacity-50"
        onConfirm={() => void confirmArchive()}
        onCancel={() => {
          if (!archiving) {
            setListToArchive(null);
          }
        }}
      />

      <ConfirmModal
        open={listToDelete !== null}
        message={
          listToDelete
            ? `Are you sure you want to delete ${listToDelete.name}? This cannot be undone.`
            : ""
        }
        confirmLabel="Yes"
        cancelLabel="No"
        busy={deleting}
        onConfirm={() => void confirmDelete()}
        onCancel={() => {
          if (!deleting) {
            setListToDelete(null);
          }
        }}
      />
    </div>
  );
}
