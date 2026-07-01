import { ImageUp, Loader2, Upload } from "lucide-react";
import { useCallback, useRef, useState, type DragEvent } from "react";
import * as recipesApi from "../api/recipes";
import type { RecipeImageImportMode } from "../types/recipe";
import { Toast } from "./Toast";

const ACCEPTED_TYPES = new Set([
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/webp",
  "image/bmp",
  "image/tiff",
]);

const IMPORT_MODE_OPTIONS: { value: RecipeImageImportMode; label: string }[] = [
  { value: "FullRecipeWithSteps", label: "Full Recipe With Steps" },
  { value: "IngredientsOnly", label: "Just Ingredients" },
  { value: "CookingStepsOnly", label: "Just Cooking Steps" },
];

const DEFAULT_IMPORT_MODE: RecipeImageImportMode = "FullRecipeWithSteps";

function isImageFile(file: File): boolean {
  return ACCEPTED_TYPES.has(file.type) || file.type.startsWith("image/");
}

function resetFileInput(input: HTMLInputElement | null) {
  if (input) {
    input.value = "";
  }
}

export function RecipeImageUploader({
  recipeId,
  onImported,
}: {
  recipeId: string;
  onImported: () => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragOver, setDragOver] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [importMode, setImportMode] = useState<RecipeImageImportMode>(DEFAULT_IMPORT_MODE);
  const [error, setError] = useState<string | null>(null);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const resetToDefault = useCallback(() => {
    setSelectedFile(null);
    setImportMode(DEFAULT_IMPORT_MODE);
    setError(null);
    resetFileInput(inputRef.current);
  }, []);

  const selectFile = useCallback((file: File) => {
    if (!isImageFile(file)) {
      setError("Please upload a JPEG, PNG, or WebP image.");
      setSelectedFile(null);
      resetFileInput(inputRef.current);
      return;
    }

    setError(null);
    setSelectedFile(file);
  }, []);

  const handleImport = useCallback(async () => {
    if (!selectedFile || uploading) {
      return;
    }

    setUploading(true);
    setError(null);

    try {
      const result = await recipesApi.uploadRecipeImage(recipeId, selectedFile, importMode);
      if (result.ingredients.length > 0 || result.steps.length > 0) {
        onImported();
      }
      setToastMessage(result.message);
      resetToDefault();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed");
    } finally {
      setUploading(false);
    }
  }, [importMode, onImported, recipeId, resetToDefault, selectedFile, uploading]);

  function handleDrop(event: DragEvent) {
    event.preventDefault();
    setDragOver(false);
    const file = event.dataTransfer.files[0];
    if (file) {
      selectFile(file);
    }
  }

  const canImport = Boolean(selectedFile) && !uploading;

  return (
    <>
      <section className="rounded-2xl border border-border bg-white p-4 shadow-sm">
        <div className="mb-3 flex items-center gap-2">
          <ImageUp className="h-5 w-5 text-brand-600" aria-hidden />
          <h2 className="font-semibold text-ink">Import from screenshot</h2>
        </div>
        <p className="mb-4 text-sm text-muted">
          Drop a recipe screenshot with ingredients and directions. OCR imports all ingredients
          into one list (section labels like Cake or Glaze are ignored) and adds cooking steps
          when it can read them.
        </p>

        <div
          role="button"
          tabIndex={0}
          onKeyDown={(event) => {
            if (event.key === "Enter" || event.key === " ") {
              inputRef.current?.click();
            }
          }}
          onClick={() => !uploading && inputRef.current?.click()}
          onDragOver={(event) => {
            event.preventDefault();
            setDragOver(true);
          }}
          onDragLeave={() => setDragOver(false)}
          onDrop={handleDrop}
          className={`flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed px-6 py-10 text-center transition ${
            uploading
              ? "cursor-wait border-brand-300 bg-brand-50/50"
              : dragOver
                ? "border-brand-500 bg-brand-50"
                : "border-border bg-stone-50/80 hover:border-brand-400 hover:bg-brand-50/40"
          }`}
        >
          {uploading ? (
            <>
              <Loader2 className="h-10 w-10 animate-spin text-brand-600" aria-hidden />
              <p className="mt-3 font-medium text-ink">Reading recipe…</p>
              <p className="mt-1 text-sm text-muted">This may take a few seconds</p>
            </>
          ) : (
            <>
              <Upload className="h-10 w-10 text-muted" aria-hidden />
              <p className="mt-3 font-medium text-ink">Drag & drop an image here</p>
              <p className="mt-1 text-sm text-muted">or click to choose a file</p>
            </>
          )}
        </div>

        {selectedFile && !uploading && (
          <p className="mt-3 truncate text-sm font-medium text-ink" title={selectedFile.name}>
            {selectedFile.name}
          </p>
        )}

        <input
          ref={inputRef}
          type="file"
          accept="image/*"
          className="sr-only"
          disabled={uploading}
          onChange={(event) => {
            const file = event.target.files?.[0];
            if (file) {
              selectFile(file);
            } else {
              resetFileInput(event.target);
            }
          }}
        />

        <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <label className="block min-w-0 flex-1">
            <span className="sr-only">Import type</span>
            <select
              value={importMode}
              onChange={(event) => setImportMode(event.target.value as RecipeImageImportMode)}
              disabled={uploading}
              className="w-full rounded-xl border border-border bg-white px-3 py-2.5 text-sm text-ink outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {IMPORT_MODE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <button
            type="button"
            onClick={() => void handleImport()}
            disabled={!canImport}
            className="shrink-0 rounded-xl border border-border bg-white px-6 py-2.5 text-sm font-medium text-ink transition hover:border-brand-300 hover:bg-brand-50/40 disabled:cursor-not-allowed disabled:border-border disabled:bg-stone-100 disabled:text-muted"
          >
            Import
          </button>
        </div>

        {error && (
          <p className="mt-3 text-sm text-red-600" role="alert">
            {error}
          </p>
        )}
      </section>

      {toastMessage && (
        <Toast message={toastMessage} onDismiss={() => setToastMessage(null)} />
      )}
    </>
  );
}
