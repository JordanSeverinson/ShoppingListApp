import { ChevronRight, type LucideIcon } from "lucide-react";
import { Link } from "react-router";

export function HomeNavCard({
  to,
  icon: Icon,
  title,
  description,
}: {
  to: string;
  icon: LucideIcon;
  title: string;
  description: string;
}) {
  return (
    <Link
      to={to}
      className="flex items-center gap-4 rounded-2xl border border-border bg-white p-4 shadow-sm transition hover:border-brand-200 hover:shadow-md"
    >
      <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-stone-100 text-ink">
        <Icon className="h-6 w-6" aria-hidden />
      </div>
      <div className="min-w-0 flex-1">
        <p className="font-semibold text-ink">{title}</p>
        <p className="text-sm text-muted">{description}</p>
      </div>
      <ChevronRight className="h-5 w-5 shrink-0 text-muted" aria-hidden />
    </Link>
  );
}
