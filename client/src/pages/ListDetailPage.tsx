import { ArrowLeft, History, LogOut, ShoppingCart, UserPlus } from "lucide-react";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import * as listsApi from "../api/lists";
import { AddItemInput } from "../components/AddItemInput";
import { ConfirmModal } from "../components/ConfirmModal";
import { ConnectionBadge } from "../components/ConnectionBadge";
import { EditableName } from "../components/EditableName";
import { ListView } from "../components/ListView";
import { RecipeImporter } from "../components/RecipeImporter";
import { ShareWithFriendsModal } from "../components/ShareWithFriendsModal";
import { ShoppingListProvider, useShoppingList } from "../context/ShoppingListContext";
import { useAutoDismissMessage } from "../hooks/useAutoDismissMessage";

function ListDetailContent() {
  const navigate = useNavigate();
  const {
    listId,
    listName,
    isOwner,
    isArchived,
    canEdit,
    connectionStatus,
    renameList,
  } = useShoppingList();
  const [shareOpen, setShareOpen] = useState(false);
  const [shareMessage, setShareMessage] = useState<string | null>(null);
  const [leaveOpen, setLeaveOpen] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [leaveError, setLeaveError] = useState<string | null>(null);

  useAutoDismissMessage(shareMessage, setShareMessage);

  async function confirmLeaveList() {
    setLeaving(true);
    setLeaveError(null);
    try {
      await listsApi.leaveList(listId);
      setLeaveOpen(false);
      navigate("/lists");
    } catch (err) {
      setLeaveError(err instanceof Error ? err.message : "Could not leave list");
    } finally {
      setLeaving(false);
    }
  }

  return (
    <>
      <Link
        to="/lists"
        className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-brand-700"
      >
        <ArrowLeft className="h-4 w-4" aria-hidden />
        All lists
      </Link>

      <header className="mb-8">
        <div className="mb-3 flex flex-wrap items-center gap-2">
          <div className="inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
            <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
            {isArchived ? "Archived list" : "Active list"}
          </div>
          {isArchived ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-stone-100 px-2.5 py-1 text-xs font-medium text-muted">
              <History className="h-3.5 w-3.5" aria-hidden />
              View only
            </span>
          ) : (
            <ConnectionBadge status={connectionStatus} />
          )}
        </div>

        <EditableName name={listName} size="lg" onSave={renameList} />

        {isOwner && !isArchived && (
          <div className="mt-4">
            <button
              type="button"
              onClick={() => setShareOpen(true)}
              className="inline-flex items-center gap-2 rounded-xl border border-brand-200 bg-brand-50 px-4 py-2 text-sm font-medium text-brand-800 transition hover:border-brand-300 hover:bg-brand-100"
            >
              <UserPlus className="h-4 w-4" aria-hidden />
              Share with Friends
            </button>
          </div>
        )}

        {!isOwner && (
          <div className="mt-4">
            <button
              type="button"
              onClick={() => {
                setLeaveError(null);
                setLeaveOpen(true);
              }}
              className="inline-flex items-center gap-2 rounded-xl border border-border px-4 py-2 text-sm font-medium text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-700"
            >
              <LogOut className="h-4 w-4" aria-hidden />
              Leave List
            </button>
          </div>
        )}

        {leaveError && (
          <p className="mt-3 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {leaveError}
          </p>
        )}

        {shareMessage && (
          <p className="mt-3 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
            {shareMessage}
          </p>
        )}

        {isArchived && (
          <p className="mt-3 text-sm text-muted">
            This list is in your history. Items cannot be changed—only the list name.
          </p>
        )}
      </header>

      <div className="space-y-8">
        {canEdit && <RecipeImporter />}
        {canEdit && <AddItemInput />}
        <ListView readOnly={!canEdit} />
      </div>

      <ShareWithFriendsModal
        open={shareOpen}
        itemName={listName}
        itemLabel="list"
        onClose={() => setShareOpen(false)}
        onShared={(message) => setShareMessage(message)}
        onShare={(friendUserIds) => listsApi.shareList(listId, friendUserIds)}
      />

      <ConfirmModal
        open={leaveOpen}
        message="Are you sure you want to remove yourself as a shared user on this list?"
        confirmLabel="Leave List"
        cancelLabel="Cancel"
        busy={leaving}
        busyLabel="Leaving…"
        onConfirm={() => void confirmLeaveList()}
        onCancel={() => {
          if (!leaving) {
            setLeaveOpen(false);
          }
        }}
      />
    </>
  );
}

export function ListDetailPage() {
  const { listId } = useParams<{ listId: string }>();

  if (!listId) {
    return (
      <div className="mx-auto max-w-lg px-4 py-20 text-center text-red-700">
        Missing list id in URL.
      </div>
    );
  }

  return (
    <div className="mx-auto min-h-screen max-w-3xl px-4 py-8 sm:px-6 sm:py-12">
      <ShoppingListProvider listId={listId}>
        <ListDetailContent />
      </ShoppingListProvider>
    </div>
  );
}
