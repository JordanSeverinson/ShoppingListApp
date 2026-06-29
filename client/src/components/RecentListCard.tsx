import { Link } from "react-router-dom";
import type { ListSummary } from "../types/list";

export function RecentListCard({ list }: { list: ListSummary }) {
  const progress =
    list.itemCount > 0 ? Math.round((list.checkedCount / list.itemCount) * 100) : 0;

  return (
    <Link
      to={`/lists/${list.id}`}
      className="block rounded-2xl border border-border bg-white p-4 shadow-sm transition hover:border-brand-200 hover:shadow-md"
    >
      <div className="mb-3">
        <p className="font-semibold text-ink">{list.name}</p>
        <p className="text-sm text-muted">
          {list.itemCount} {list.itemCount === 1 ? "item" : "items"}
        </p>
      </div>
      <div className="mb-2 flex items-center gap-3">
        <div className="h-2 flex-1 overflow-hidden rounded-full bg-stone-200">
          <div
            className="h-full rounded-full bg-brand-500 transition-all"
            style={{ width: `${progress}%` }}
          />
        </div>
        <span className="w-10 text-right text-sm font-medium text-muted">{progress}%</span>
      </div>
      <p className="text-sm text-muted">
        {list.checkedCount} of {list.itemCount} items checked
      </p>
    </Link>
  );
}
