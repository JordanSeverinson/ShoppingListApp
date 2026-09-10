import { Plus, Trash2 } from "lucide-react";
import { useEffect, useLayoutEffect, useRef, useState } from "react";

function serializeSteps(steps: string[]): string {
  return steps.map((step) => step.trim()).filter((step) => step.length > 0).join("\n");
}

function AutoResizeTextarea({
  value,
  onChange,
  onBlur,
  placeholder,
}: {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  placeholder?: string;
}) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useLayoutEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) {
      return;
    }

    textarea.style.height = "auto";
    textarea.style.height = `${textarea.scrollHeight}px`;
  }, [value]);

  return (
    <textarea
      ref={textareaRef}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      onBlur={onBlur}
      rows={1}
      placeholder={placeholder}
      className="min-h-[2.75rem] flex-1 resize-none overflow-hidden rounded-xl border border-border px-4 py-2.5 text-ink outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
    />
  );
}

export function RecipeStepsEditor({
  steps,
  onSave,
}: {
  steps: string[];
  onSave?: (steps: string[]) => Promise<void>;
}) {
  const [draftSteps, setDraftSteps] = useState<string[]>(steps.length > 0 ? steps : [""]);
  const [error, setError] = useState<string | null>(null);
  const draftRef = useRef(draftSteps);
  const onSaveRef = useRef(onSave);
  const lastSavedKey = useRef(serializeSteps(steps));
  const skipNextSync = useRef(false);
  const saveTimer = useRef<number | null>(null);
  const persistTail = useRef(Promise.resolve());
  const pendingDraft = useRef<string[] | null>(null);
  const saving = useRef(false);
  const persistRef = useRef<(nextDraft: string[]) => Promise<void>>(async () => undefined);
  const isMounted = useRef(true);

  useEffect(() => {
    draftRef.current = draftSteps;
  }, [draftSteps]);

  useEffect(() => {
    onSaveRef.current = onSave;
  }, [onSave]);

  useEffect(() => {
    if (skipNextSync.current || saving.current || pendingDraft.current) {
      return;
    }

    const incomingKey = serializeSteps(steps);
    if (incomingKey === lastSavedKey.current) {
      return;
    }

    setDraftSteps(steps.length > 0 ? steps : [""]);
    lastSavedKey.current = incomingKey;
  }, [steps]);

  useEffect(() => {
    isMounted.current = true;
    return () => {
      isMounted.current = false;
      if (saveTimer.current) {
        window.clearTimeout(saveTimer.current);
      }
      void persistRef.current(draftRef.current);
    };
  }, []);

  async function flushPersist() {
    const nextDraft = pendingDraft.current;
    pendingDraft.current = null;
    if (!nextDraft || !onSaveRef.current) {
      return;
    }

    const trimmedSteps = nextDraft.map((step) => step.trim()).filter((step) => step.length > 0);
    const nextKey = serializeSteps(trimmedSteps);
    if (nextKey === lastSavedKey.current) {
      return;
    }

    if (isMounted.current) {
      setError(null);
    }

    const previousKey = lastSavedKey.current;
    lastSavedKey.current = nextKey;
    skipNextSync.current = true;
    saving.current = true;
    try {
      await onSaveRef.current(trimmedSteps);
    } catch (err: unknown) {
      lastSavedKey.current = previousKey;
      if (isMounted.current) {
        setError(err instanceof Error ? err.message : "Could not save steps");
      }
    } finally {
      skipNextSync.current = false;
      saving.current = false;
    }
  }

  function persist(nextDraft: string[]) {
    pendingDraft.current = nextDraft;
    const run = persistTail.current.then(flushPersist);
    persistTail.current = run.catch(() => undefined);
    return run;
  }

  persistRef.current = persist;

  function schedulePersist(nextDraft: string[]) {
    if (saveTimer.current) {
      window.clearTimeout(saveTimer.current);
    }

    saveTimer.current = window.setTimeout(() => {
      void persist(nextDraft);
    }, 400);
  }

  function updateStep(index: number, value: string) {
    setDraftSteps((current) => {
      const next = current.map((step, i) => (i === index ? value : step));
      schedulePersist(next);
      return next;
    });
  }

  function addStep() {
    setDraftSteps((current) => [...current, ""]);
  }

  function removeStep(index: number) {
    setDraftSteps((current) => {
      const next = current.filter((_, i) => i !== index);
      const withPlaceholder = next.length > 0 ? next : [""];
      if (saveTimer.current) {
        window.clearTimeout(saveTimer.current);
      }
      void persist(withPlaceholder);
      return withPlaceholder;
    });
  }

  return (
    <section className="rounded-2xl border border-border bg-white p-4 shadow-sm">
      <div className="mb-4">
        <h2 className="font-semibold text-ink">Cooking Steps</h2>
        <p className="text-sm text-muted">Add the directions for this recipe. Changes save automatically.</p>
      </div>

      <ol className="space-y-3">
        {draftSteps.map((step, index) => (
          <li key={index} className="flex items-start gap-3">
            <span className="mt-2 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700">
              {index + 1}
            </span>
            <AutoResizeTextarea
              value={step}
              onChange={(value) => updateStep(index, value)}
              onBlur={() => {
                if (saveTimer.current) {
                  window.clearTimeout(saveTimer.current);
                }
                void persist(draftRef.current);
              }}
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

      {error && (
        <p className="mt-3 text-sm text-red-600" role="alert">
          {error}
        </p>
      )}
    </section>
  );
}
