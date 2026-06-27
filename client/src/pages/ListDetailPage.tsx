import { ArrowLeft, History, ShoppingCart } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { AddItemInput } from "../components/AddItemInput";
import { ConnectionBadge } from "../components/ConnectionBadge";
import { EditableListName } from "../components/EditableListName";
import { ImageUploader } from "../components/ImageUploader";
import { ListView } from "../components/ListView";
import { ShareCodeCopy } from "../components/ShareCodeCopy";
import { ShoppingListProvider, useShoppingList } from "../context/ShoppingListContext";

function ListDetailContent() {
  const {
    listName,
    shareCode,
    isArchived,
    canEdit,
    connectionStatus,
    renameList,
  } = useShoppingList();

  return (
    <>
      <Link
        to="/"
        className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-brand-700"
      >
        <ArrowLeft className="h-4 w-4" aria-hidden />
        All lists
      </Link>

      <header className="mb-8">
        <div className="mb-3 flex flex-wrap items-center gap-2">
          <div className="inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
            <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
            {isArchived ? "Archived list" : "Shared list"}
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

        <EditableListName name={listName} size="lg" onSave={renameList} />

        <div className="mt-4">
          <ShareCodeCopy shareCode={shareCode} />
        </div>

        {isArchived && (
          <p className="mt-3 text-sm text-muted">
            This list is in your history. Items cannot be changed—only the list name.
          </p>
        )}
      </header>

      <div className="space-y-8">
        {canEdit && <ImageUploader />}
        {canEdit && <AddItemInput />}
        <ListView readOnly={!canEdit} />
      </div>
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
    <ShoppingListProvider listId={listId}>
      <div className="mx-auto min-h-screen max-w-2xl px-4 py-8 sm:px-6 sm:py-12">
        <ListDetailContent />
      </div>
    </ShoppingListProvider>
  );
}
