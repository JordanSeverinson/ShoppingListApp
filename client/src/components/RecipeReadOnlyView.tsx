import { Download, Loader2 } from "lucide-react";
import { useRef, useState } from "react";
import { exportRecipePng } from "../lib/exportRecipePng";
import {
  resolveRecipeDisplayData,
  type RecipeContentDocument,
  type RecipeIngredient,
} from "../types/recipe";

export function RecipeReadOnlyView({
  recipeName,
  content,
  ingredients,
  steps,
}: {
  recipeName: string;
  content: RecipeContentDocument;
  ingredients: RecipeIngredient[];
  steps: string[];
}) {
  const cardRef = useRef<HTMLElement>(null);
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);

  const { subCategories, cookingSteps, hasMultipleSubsections } = resolveRecipeDisplayData(
    content,
    ingredients,
    steps,
  );

  async function handleExport() {
    if (!cardRef.current) {
      return;
    }

    setExporting(true);
    setExportError(null);
    try {
      await exportRecipePng(cardRef.current, recipeName);
    } catch (err) {
      setExportError(err instanceof Error ? err.message : "Could not export recipe");
    } finally {
      setExporting(false);
    }
  }

  return (
    <div className="space-y-4">
      <button
        type="button"
        onClick={() => void handleExport()}
        disabled={exporting}
        className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-brand-600 px-4 py-3 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:opacity-60 sm:w-auto"
      >
        {exporting ? (
          <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
        ) : (
          <Download className="h-4 w-4" aria-hidden />
        )}
        {exporting ? "Generating image…" : "Export Recipe"}
      </button>

      {exportError && (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {exportError}
        </p>
      )}

      <article
        ref={cardRef}
        className="overflow-hidden rounded-2xl border border-border bg-white shadow-sm"
      >
        <header className="border-b border-border bg-stone-50 px-6 py-5">
          <p className="text-xs font-semibold uppercase tracking-widest text-brand-700">
            Cook With Me
          </p>
          <h2 className="mt-1 text-2xl font-bold tracking-tight text-ink sm:text-3xl">
            {recipeName}
          </h2>
        </header>

        <div className="space-y-8 px-6 py-6">
          {subCategories.length === 0 ? (
            <section>
              <h3 className="mb-3 text-lg font-bold text-ink">Ingredients</h3>
              <p className="text-sm text-muted">No ingredients for this recipe yet.</p>
            </section>
          ) : hasMultipleSubsections ? (
            subCategories.map((block, blockIndex) => (
              <section key={`${block.description}-${blockIndex}`}>
                <h3 className="mb-3 text-lg font-bold text-ink">{block.description}</h3>
                <ul className="space-y-2">
                  {block.ingredients.map((line, lineIndex) => (
                    <li
                      key={`${blockIndex}-${lineIndex}`}
                      className="flex gap-3 text-base leading-relaxed text-ink"
                    >
                      <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-brand-600" />
                      <span>{line}</span>
                    </li>
                  ))}
                </ul>
              </section>
            ))
          ) : (
            <section>
              <h3 className="mb-3 text-lg font-bold text-ink">Ingredients</h3>
              <ul className="space-y-2">
                {subCategories[0]?.ingredients.map((line, lineIndex) => (
                  <li
                    key={lineIndex}
                    className="flex gap-3 text-base leading-relaxed text-ink"
                  >
                    <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-brand-600" />
                    <span>{line}</span>
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section className="border-t border-border pt-8">
            <h3 className="mb-4 text-lg font-bold text-ink">Cooking Steps</h3>
            {cookingSteps.length === 0 ? (
              <p className="text-sm text-muted">No cooking steps for this recipe yet.</p>
            ) : (
              <ol className="space-y-4">
                {cookingSteps.map((step, index) => (
                  <li key={index} className="flex gap-4">
                    <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-brand-600 text-sm font-bold text-white">
                      {index + 1}
                    </span>
                    <p className="pt-1 text-base leading-relaxed text-ink">{step}</p>
                  </li>
                ))}
              </ol>
            )}
          </section>
        </div>
      </article>
    </div>
  );
}
