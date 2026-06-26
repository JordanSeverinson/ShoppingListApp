import { Check, Copy } from "lucide-react";
import { useState } from "react";

export function ShareCodeCopy({
  shareCode,
  label = "Share code",
}: {
  shareCode: string;
  label?: string;
}) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(shareCode);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      /* clipboard unavailable */
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <span className="text-xs font-medium uppercase tracking-wide text-muted">
        {label}
      </span>
      <code className="rounded-lg bg-stone-100 px-2.5 py-1 font-mono text-sm font-semibold tracking-wider text-brand-800 ring-1 ring-border">
        {shareCode}
      </code>
      <button
        type="button"
        onClick={() => void handleCopy()}
        className="inline-flex items-center gap-1.5 rounded-lg border border-border bg-white px-2.5 py-1.5 text-xs font-medium text-ink transition hover:border-brand-300 hover:bg-brand-50"
        aria-label={copied ? "Copied" : "Copy share code"}
      >
        {copied ? (
          <>
            <Check className="h-3.5 w-3.5 text-brand-600" aria-hidden />
            Copied
          </>
        ) : (
          <>
            <Copy className="h-3.5 w-3.5" aria-hidden />
            Copy
          </>
        )}
      </button>
    </div>
  );
}
