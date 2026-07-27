import { Check, Pencil } from "lucide-react";
import { useEffect, useState } from "react";

export function EditableName({
  name,
  onSave,
  size = "lg",
  readOnly = false,
}: {
  name: string;
  onSave: (name: string) => Promise<void>;
  size?: "lg" | "sm";
  readOnly?: boolean;
}) {
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(name);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setValue(name);
  }, [name]);

  async function commit() {
    const trimmed = value.trim();
    if (!trimmed || trimmed === name) {
      setEditing(false);
      setValue(name);
      return;
    }

    setSaving(true);
    try {
      await onSave(trimmed);
      setEditing(false);
    } finally {
      setSaving(false);
    }
  }

  if (readOnly) {
    return (
      <h1
        className={
          size === "lg"
            ? "text-2xl font-bold tracking-tight text-ink sm:text-3xl"
            : "text-lg font-semibold text-ink"
        }
      >
        {name}
      </h1>
    );
  }

  if (editing) {
    return (
      <div className="flex items-center gap-2">
        <input
          value={value}
          onChange={(event) => setValue(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              void commit();
            }
            if (event.key === "Escape") {
              setEditing(false);
              setValue(name);
            }
          }}
          className={`rounded-lg border border-brand-300 px-3 py-1.5 font-semibold text-ink outline-none ring-2 ring-brand-500/30 ${
            size === "lg" ? "text-2xl sm:text-3xl" : "text-base"
          }`}
          autoFocus
          disabled={saving}
        />
        <button
          type="button"
          onClick={() => void commit()}
          disabled={saving}
          className="rounded-lg bg-brand-600 p-2 text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <Check className="h-4 w-4" />
        </button>
      </div>
    );
  }

  return (
    <div className="group flex items-center gap-2">
      <h1
        className={
          size === "lg"
            ? "text-2xl font-bold tracking-tight text-ink sm:text-3xl"
            : "text-lg font-semibold text-ink"
        }
      >
        {name}
      </h1>
      <button
        type="button"
        onClick={() => setEditing(true)}
        className="rounded-lg p-1.5 text-muted opacity-100 transition hover:bg-stone-100 hover:text-ink sm:opacity-0 sm:group-hover:opacity-100"
        aria-label="Rename"
      >
        <Pencil className="h-4 w-4" />
      </button>
    </div>
  );
}
