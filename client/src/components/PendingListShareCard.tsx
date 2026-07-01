import { Check, ChevronRight, X } from "lucide-react";
import type { PendingListShare } from "../types/list";

export function PendingListShareCard({
  share,
  busy,
  onAccept,
  onDecline,
}: {
  share: PendingListShare;
  busy?: boolean;
  onAccept: (share: PendingListShare) => void;
  onDecline: (share: PendingListShare) => void;
}) {
  const progress =
    share.itemCount > 0 ? Math.round((share.checkedCount / share.itemCount) * 100) : 0;

  return (
    <article className="rounded-2xl border border-amber-200 bg-amber-50/60 p-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <p className="mb-1 text-xs font-medium uppercase tracking-wide text-amber-800">
            Pending share
          </p>
          <h3 className="text-lg font-semibold text-ink">{share.listName}</h3>
          <p className="mt-1 text-sm text-muted">
            {share.invitedByName} invited you to collaborate
          </p>
          <p className="mt-2 text-sm text-muted">
            {share.checkedCount} of {share.itemCount} items checked ({progress}%)
          </p>
        </div>
        <ChevronRight className="mt-1 h-5 w-5 shrink-0 text-amber-700/50" aria-hidden />
      </div>

      <div className="mt-4 flex flex-wrap gap-2">
        <button
          type="button"
          disabled={busy}
          onClick={() => onAccept(share)}
          className="inline-flex flex-1 items-center justify-center gap-2 rounded-xl bg-brand-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-brand-700 disabled:opacity-50 sm:flex-none"
        >
          <Check className="h-4 w-4" aria-hidden />
          Accept invitation
        </button>
        <button
          type="button"
          disabled={busy}
          onClick={() => onDecline(share)}
          className="inline-flex items-center gap-2 rounded-lg border border-border bg-white px-3 py-1.5 text-sm font-medium text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-700 disabled:opacity-50"
        >
          <X className="h-4 w-4" aria-hidden />
          Decline
        </button>
      </div>
    </article>
  );
}
