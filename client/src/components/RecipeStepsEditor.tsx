import { Plus, Save, Trash2 } from "lucide-react";
import { useEffect, useLayoutEffect, useRef, useState } from "react";

function AutoResizeTextarea({
  value,
  onChange,
  placeholder,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  const ref = useRef<HTMLTextAreaElement>(null);

  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) {
      return;
    }

    el.style.height = "auto";
    el.style.height = `${el.scrollHeight}px`;
  }, [value]);

  return (
    <textarea
      ref={ref}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      rows={1}
      placeholder={placeholder}
      className="min-h-[2.75rem] flex-1 resize-none overflow-hidden rounded-xl border border-border px-4 py-2.5 text-ink outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
    />
  );
}

export function RecipeStepsEditor({
  steps,
  onSave,
  readOnly = false,
}: {
  steps: string[];
  onSave?: (steps: string[]) => Promise<void>;
  readOnly?: boolean;
}) {
  const [draft, setDraft] = useState<string[]>(steps.length > 0 ? steps : [""]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setDraft(steps.length > 0 ? steps : [""]);
  }, [steps]);

  if (readOnly) {
    return (
      <section className="rounded-2xl border border-border bg-white p-4 shadow-sm">
        <h2 className="font-semibold text-ink">Cooking Steps</h2>
        {steps.length === 0 ? (
          <p className="mt-3 text-sm text-muted">No cooking steps for this recipe yet.</p>
        ) : (
          <ol className="mt-4 space-y-4">
            {steps.map((step, index) => (
              <li key={index} className="flex items-start gap-3">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700">
                  {index + 1}
                </span>
                <p className="pt-0.5 text-ink">{step}</p>
              </li>
            ))}
          </ol>
        )}
      </section>
    );
  }

  function updateStep(index: number, value: string) {
    setDraft((current) => current.map((step, i) => (i === index ? value : step)));
    setSaved(false);
  }

  function addStep() {
    setDraft((current) => [...current, ""]);
    setSaved(false);
  }

  function removeStep(index: number) {
    setDraft((current) => current.filter((_, i) => i !== index));
    setSaved(false);
  }

  async function handleSave() {
    const cleaned = draft.map((step) => step.trim()).filter((step) => step.length > 0);
    setSaving(true);
    setError(null);
    try {
      await onSave?.(cleaned);
      setDraft(cleaned.length > 0 ? cleaned : [""]);
      setSaved(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save steps");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="rounded-2xl border border-border bg-white p-4 shadow-sm">
      <div className="mb-4 flex items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold text-ink">Cooking Steps</h2>
          <p className="text-sm text-muted">Add the directions for this recipe.</p>
        </div>
        <button
          type="button"
          onClick={() => void handleSave()}
          disabled={saving}
          className="inline-flex items-center gap-2 rounded-xl bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <Save className="h-4 w-4" aria-hidden />
          {saving ? "Saving…" : "Save steps"}
        </button>
      </div>

      <ol className="space-y-3">
        {draft.map((step, index) => (
          <li key={index} className="flex items-start gap-3">
            <span className="mt-2 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700">
              {index + 1}
            </span>
            <AutoResizeTextarea
              value={step}
              onChange={(value) => updateStep(index, value)}
              placeholder={`Step ${index + 1}`}
            />
            <button
              type="button"
              onClick={() => removeStep(index)}
              className="mt-1 rounded-lg p-2 text-muted hover:bg-red-50 hover:text-red-600"
              aria-label={`Remove step ${index + 1}`}
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </li>
        ))}
      </ol>

      <button
        type="button"
        onClick={addStep}
        className="mt-4 inline-flex items-center gap-2 rounded-xl border border-dashed border-border px-4 py-2 text-sm font-medium text-brand-700 hover:border-brand-300 hover:bg-brand-50"
      >
        <Plus className="h-4 w-4" aria-hidden />
        Add step
      </button>

      {saved && <p className="mt-3 text-sm text-brand-700">Steps saved.</p>}
      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}
    </section>
  );
}
