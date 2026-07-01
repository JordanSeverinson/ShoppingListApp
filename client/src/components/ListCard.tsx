import { Archive, ChevronRight, History, LogOut, Trash2, UserPlus, Users } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router-dom";
import * as listsApi from "../api/lists";
import { getListOwnershipLabel, type ListSummary } from "../types/list";
import { EditableListName } from "./EditableListName";
import { ShareWithFriendsModal } from "./ShareWithFriendsModal";

export function ListCard({
  list,
  onRename,
  showArchiveAction,
  onArchive,
  onDelete,
  onLeave,
  onShared,
}: {
  list: ListSummary;
  onRename: (listId: string, name: string) => Promise<void>;
  showArchiveAction?: boolean;
  onArchive?: (list: ListSummary) => void;
  onDelete?: (list: ListSummary) => void;
  onLeave?: (list: ListSummary) => void;
  onShared?: (message: string) => void;
}) {
  const [shareOpen, setShareOpen] = useState(false);
  const progress =
    list.itemCount > 0 ? Math.round((list.checkedCount / list.itemCount) * 100) : 0;

  return (
    <>
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
                {getListOwnershipLabel(list)}
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
          <p className="text-sm text-muted">
            {list.checkedCount} of {list.itemCount} items checked ({progress}%)
          </p>
          <div className="flex flex-wrap gap-2">
            {list.isOwner && !list.isArchived && (
              <button
                type="button"
                onClick={() => setShareOpen(true)}
                className="inline-flex items-center gap-2 rounded-lg border border-brand-200 bg-brand-50 px-3 py-1.5 text-sm font-medium text-brand-800 transition hover:border-brand-300 hover:bg-brand-100"
              >
                <UserPlus className="h-4 w-4" aria-hidden />
                Share with Friends
              </button>
            )}
            {showArchiveAction && list.isOwner && onArchive && (
              <button
                type="button"
                onClick={() => onArchive(list)}
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:border-amber-300 hover:bg-amber-50 hover:text-amber-900"
              >
                <Archive className="h-4 w-4" aria-hidden />
                Archive
              </button>
            )}
            {list.isOwner && onDelete && (
              <button
                type="button"
                onClick={() => onDelete(list)}
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-700"
              >
                <Trash2 className="h-4 w-4" aria-hidden />
                Delete list
              </button>
            )}
            {!list.isOwner && onLeave && (
              <button
                type="button"
                onClick={() => onLeave(list)}
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-700"
              >
                <LogOut className="h-4 w-4" aria-hidden />
                Leave List
              </button>
            )}
          </div>
        </div>
      </article>

      <ShareWithFriendsModal
        open={shareOpen}
        itemName={list.name}
        itemLabel="list"
        onClose={() => setShareOpen(false)}
        onShared={onShared}
        onShare={(friendUserIds) => listsApi.shareList(list.id, friendUserIds)}
      />
    </>
  );
}
