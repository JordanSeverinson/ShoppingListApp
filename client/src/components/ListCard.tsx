import { Archive, ChevronRight, History, Users } from "lucide-react";
import { Link } from "react-router-dom";
import type { ListSummary } from "../types/list";
import { EditableListName } from "./EditableListName";
import { ShareCodeCopy } from "./ShareCodeCopy";

export function ListCard({
  list,
  onRename,
  showArchiveAction,
  onArchive,
}: {
  list: ListSummary;
  onRename: (listId: string, name: string) => Promise<void>;
  showArchiveAction?: boolean;
  onArchive?: (listId: string) => Promise<void>;
}) {
  const progress =
    list.itemCount > 0 ? Math.round((list.checkedCount / list.itemCount) * 100) : 0;

  return (
    <article className="rounded-2xl border border-border bg-white p-4 shadow-sm transition hover:border-brand-200 hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          {list.isArchived ? (
            <div className="mb-1 flex items-center gap-1.5 text-xs font-medium text-muted">
              <History className="h-3.5 w-3.5" aria-hidden />
              Archived · view only
            </div>
          ) : (
            <div className="mb-1 flex items-center gap-1.5 text-xs font-medium text-brand-700">
              <Users className="h-3.5 w-3.5" aria-hidden />
              Shared list
            </div>
          )}
          <EditableListName
            name={list.name}
            size="sm"
            onSave={(name) => onRename(list.id, name)}
          />
        </div>
        <Link
          to={`/lists/${list.id}`}
          className="shrink-0 rounded-lg p-2 text-muted hover:bg-brand-50 hover:text-brand-700"
          aria-label={`Open ${list.name}`}
        >
          <ChevronRight className="h-5 w-5" />
        </Link>
      </div>

      <div className="mt-3 space-y-3">
        <ShareCodeCopy shareCode={list.shareCode} />
        <p className="text-sm text-muted">
          {list.checkedCount} of {list.itemCount} items checked ({progress}%)
        </p>
        {showArchiveAction && onArchive && (
          <button
            type="button"
            onClick={() => void onArchive(list.id)}
            className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:border-amber-300 hover:bg-amber-50 hover:text-amber-900"
          >
            <Archive className="h-4 w-4" aria-hidden />
            Archive
          </button>
        )}
      </div>
    </article>
  );
}
