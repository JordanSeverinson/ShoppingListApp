import { UserPlus } from "lucide-react";
import { useEffect, useId, useState } from "react";
import * as friendsApi from "../api/friends";
import type { FriendSummary } from "../types/friend";

export function ShareWithFriendsModal({
  open,
  itemName,
  itemLabel = "list",
  onClose,
  onShared,
  onShare,
}: {
  open: boolean;
  itemName: string;
  itemLabel?: "list" | "recipe";
  onClose: () => void;
  onShared?: (message: string) => void;
  onShare: (friendUserIds: string[]) => Promise<{ message: string }>;
}) {
  const titleId = useId();
  const [friends, setFriends] = useState<FriendSummary[]>([]);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setSelectedIds(new Set());
      setError(null);
      return;
    }

    async function loadFriends() {
      setLoading(true);
      setError(null);
      try {
        const data = await friendsApi.fetchFriends();
        setFriends(data.friends);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Could not load friends");
      } finally {
        setLoading(false);
      }
    }

    void loadFriends();
  }, [open]);

  function toggleFriend(userId: string) {
    setSelectedIds((current) => {
      const next = new Set(current);
      if (next.has(userId)) {
        next.delete(userId);
      } else {
        next.add(userId);
      }
      return next;
    });
  }

  async function handleShare() {
    if (selectedIds.size === 0) {
      setError("Select at least one friend.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const result = await onShare([...selectedIds]);
      onShared?.(result.message);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : `Could not share ${itemLabel}`);
    } finally {
      setBusy(false);
    }
  }

  if (!open) {
    return null;
  }

  const shareButtonLabel = itemLabel === "recipe" ? "Share recipe" : "Share list";

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-ink/40 p-4"
      role="presentation"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="flex max-h-[85vh] w-full max-w-md flex-col rounded-2xl border border-border bg-white shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="border-b border-border px-6 py-4">
          <h2 id={titleId} className="text-lg font-semibold text-ink">
            Share with friends
          </h2>
          <p className="mt-1 text-sm text-muted">
            Invite friends to collaborate on <span className="font-medium text-ink">{itemName}</span>.
            They must accept before joining.
          </p>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-4">
          {loading ? (
            <p className="text-sm text-muted">Loading friends…</p>
          ) : friends.length === 0 ? (
            <p className="text-sm text-muted">
              You do not have any friends yet. Add friends first, then share your {itemLabel}.
            </p>
          ) : (
            <ul className="space-y-2">
              {friends.map((friend) => {
                const checked = selectedIds.has(friend.userId);
                return (
                  <li key={friend.userId}>
                    <label className="flex cursor-pointer items-center gap-3 rounded-xl border border-border px-4 py-3 transition hover:border-brand-300 hover:bg-brand-50/50">
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() => toggleFriend(friend.userId)}
                        className="h-4 w-4 rounded border-border text-brand-600 focus:ring-brand-500"
                      />
                      <span className="min-w-0 flex-1">
                        <span className="block font-medium text-ink">{friend.firstName}</span>
                        <span className="block truncate text-sm text-muted">{friend.email}</span>
                      </span>
                    </label>
                  </li>
                );
              })}
            </ul>
          )}

          {error && (
            <p className="mt-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {error}
            </p>
          )}
        </div>

        <div className="flex flex-col-reverse gap-3 border-t border-border px-6 py-4 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onClose}
            disabled={busy}
            className="rounded-xl border border-border px-4 py-2.5 font-medium text-ink transition hover:bg-stone-50 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => void handleShare()}
            disabled={busy || loading || friends.length === 0}
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-brand-600 px-4 py-2.5 font-medium text-white transition hover:bg-brand-700 disabled:opacity-50"
          >
            <UserPlus className="h-4 w-4" aria-hidden />
            {busy ? "Sharing…" : shareButtonLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
