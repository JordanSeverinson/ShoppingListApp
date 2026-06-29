import { ImageUp, Loader2, Upload } from "lucide-react";
import { useCallback, useRef, useState, type DragEvent } from "react";
import * as recipesApi from "../api/recipes";

const ACCEPTED_TYPES = new Set([
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/webp",
  "image/bmp",
  "image/tiff",
]);

function isImageFile(file: File): boolean {
  return ACCEPTED_TYPES.has(file.type) || file.type.startsWith("image/");
}

export function RecipeImageUploader({
  recipeId,
  onIngredientsAdded,
}: {
  recipeId: string;
  onIngredientsAdded: () => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragOver, setDragOver] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const processFile = useCallback(
    async (file: File) => {
      if (!isImageFile(file)) {
        setError("Please upload a JPEG, PNG, or WebP image.");
        setMessage(null);
        return;
      }

      setUploading(true);
      setError(null);
      setMessage(null);

      try {
        const result = await recipesApi.uploadRecipeImage(recipeId, file);
        if (result.ingredients.length > 0) {
          onIngredientsAdded();
        }
        setMessage(
          result.ingredients.length > 0
            ? result.message
            : "No ingredients detected — try a clearer screenshot.",
        );
      } catch (err) {
        setError(err instanceof Error ? err.message : "Upload failed");
      } finally {
        setUploading(false);
      }
    },
    [onIngredientsAdded, recipeId],
  );

  function handleDrop(event: DragEvent) {
    event.preventDefault();
    setDragOver(false);
    const file = event.dataTransfer.files[0];
    if (file) {
      void processFile(file);
    }
  }

  return (
    <section className="rounded-2xl border border-border bg-white p-4 shadow-sm">
      <div className="mb-3 flex items-center gap-2">
        <ImageUp className="h-5 w-5 text-brand-600" aria-hidden />
        <h2 className="font-semibold text-ink">Import from screenshot</h2>
      </div>
      <p className="mb-4 text-sm text-muted">
        Drop a recipe ingredient screenshot. OCR will add ingredients to this saved recipe.
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
            <p className="mt-3 font-medium text-ink">Reading ingredients…</p>
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

      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        className="sr-only"
        disabled={uploading}
        onChange={(event) => {
          const file = event.target.files?.[0];
          if (file) {
            void processFile(file);
          }
          event.target.value = "";
        }}
      />

      {message && (
        <p className="mt-3 text-sm font-medium text-brand-700" role="status">
          {message}
        </p>
      )}
      {error && (
        <p className="mt-3 text-sm text-red-600" role="alert">
          {error}
        </p>
      )}
    </section>
  );
}
