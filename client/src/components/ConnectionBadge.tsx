import type { ConnectionStatus } from "../lib/listHub";

const LABELS: Record<ConnectionStatus, string> = {
  connected: "Live",
  connecting: "Connecting…",
  reconnecting: "Reconnecting…",
  disconnected: "Offline",
};

const STYLES: Record<ConnectionStatus, string> = {
  connected: "bg-brand-100 text-brand-800 ring-brand-200",
  connecting: "bg-amber-50 text-amber-800 ring-amber-200",
  reconnecting: "bg-amber-50 text-amber-800 ring-amber-200",
  disconnected: "bg-stone-100 text-stone-600 ring-stone-200",
};

const DOT_STYLES: Record<ConnectionStatus, string> = {
  connected: "bg-brand-500",
  connecting: "bg-amber-400 animate-pulse",
  reconnecting: "bg-amber-400 animate-pulse",
  disconnected: "bg-stone-400",
};

export function ConnectionBadge({ status }: { status: ConnectionStatus }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset ${STYLES[status]}`}
      title={`Real-time sync: ${LABELS[status]}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${DOT_STYLES[status]}`} aria-hidden />
      {LABELS[status]}
    </span>
  );
}
