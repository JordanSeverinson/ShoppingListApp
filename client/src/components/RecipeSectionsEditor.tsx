import { ChevronDown, ChevronRight, Plus, Trash2 } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { CATEGORIES, suggestCategory } from "../lib/categories";
import type {
  CreateRecipeIngredientPayload,
  RecipeIngredient,
  UpdateRecipeIngredientPayload,
} from "../types/recipe";

const DEFAULT_SECTION_TITLE = "Ingredients";

type RecipeEditorSection = {
  key: string;
  title: string;
};

function sectionLabel(ingredient: RecipeIngredient): string {
  return ingredient.section?.trim() || DEFAULT_SECTION_TITLE;
}

function buildEditorSections(
  ingredients: RecipeIngredient[],
): RecipeEditorSection[] {
  const sectionTitles: string[] = [];
  for (const ingredient of [...ingredients].sort(
    (a, b) =>
      sectionLabel(a).localeCompare(sectionLabel(b)) || a.sortOrder - b.sortOrder,
  )) {
    const title = sectionLabel(ingredient);
    if (!sectionTitles.includes(title)) {
      sectionTitles.push(title);
    }
  }

  if (sectionTitles.length === 0) {
    return [{ key: crypto.randomUUID(), title: DEFAULT_SECTION_TITLE }];
  }

  return sectionTitles.map((title) => ({ key: crypto.randomUUID(), title }));
}

function groupIngredientsBySection(
  ingredients: RecipeIngredient[],
  sections: RecipeEditorSection[],
): Map<string, RecipeIngredient[]> {
  const groups = new Map<string, RecipeIngredient[]>();
  for (const section of sections) {
    groups.set(section.key, []);
  }

  const titleToKey = new Map(sections.map((section) => [section.title, section.key]));

  for (const ingredient of [...ingredients].sort((a, b) => a.sortOrder - b.sortOrder)) {
    const title = sectionLabel(ingredient);
    const key = titleToKey.get(title);
    if (key) {
      groups.get(key)!.push(ingredient);
    } else if (sections[0]) {
      groups.get(sections[0].key)!.push(ingredient);
    }
  }

  return groups;
}

type Props = {
  ingredients: RecipeIngredient[];
  onAdd: (payload: CreateRecipeIngredientPayload) => Promise<void>;
  onUpdate: (ingredientId: string, payload: UpdateRecipeIngredientPayload) => Promise<void>;
  onRemove: (ingredientId: string) => void;
  onRenameSection: (from: string, to: string) => Promise<void>;
  onDeleteSectionIngredients: (ingredientIds: string[]) => void;
};

export function RecipeSectionsEditor({
  ingredients,
  onAdd,
  onUpdate,
  onRemove,
  onRenameSection,
  onDeleteSectionIngredients,
}: Props) {
  const [sections, setSections] = useState<RecipeEditorSection[]>(() =>
    buildEditorSections(ingredients),
  );
  const [openByKey, setOpenByKey] = useState<Record<string, boolean>>({});
  const syncedFromIngredients = useRef<string>("");

  const ingredientsSignature = useMemo(
    () =>
      ingredients
        .map((item) => `${item.id}:${item.section ?? ""}:${item.sortOrder}`)
        .sort()
        .join("|"),
    [ingredients],
  );

  useEffect(() => {
    if (syncedFromIngredients.current === ingredientsSignature) {
      return;
    }

    const nextSections = buildEditorSections(ingredients);
    setSections((current) => {
      const currentTitles = current.map((section) => section.title);
      const nextTitles = nextSections.map((section) => section.title);
      const emptyExtraSections = current.filter(
        (section) =>
          !nextTitles.includes(section.title) &&
          !ingredients.some((item) => sectionLabel(item) === section.title),
      );

      if (
        currentTitles.length === nextTitles.length &&
        currentTitles.every((title, index) => title === nextTitles[index]) &&
        emptyExtraSections.length === 0
      ) {
        return current;
      }

      const mergedSections = [...nextSections];
      for (const extra of emptyExtraSections) {
        if (!mergedSections.some((section) => section.title === extra.title)) {
          mergedSections.push(extra);
        }
      }
      return mergedSections.length > 0
        ? mergedSections
        : [{ key: crypto.randomUUID(), title: DEFAULT_SECTION_TITLE }];
    });
    syncedFromIngredients.current = ingredientsSignature;
  }, [ingredients, ingredientsSignature]);

  const ingredientsBySection = useMemo(
    () => groupIngredientsBySection(ingredients, sections),
    [ingredients, sections],
  );

  function isSectionOpen(key: string, index: number): boolean {
    return openByKey[key] ?? index === 0;
  }

  function toggleSectionOpen(key: string, index: number) {
    setOpenByKey((current) => ({
      ...current,
      [key]: !isSectionOpen(key, index),
    }));
  }

  async function commitSectionTitle(
    section: RecipeEditorSection,
    previousTitle: string,
    draftTitle: string,
  ) {
    const nextTitle = draftTitle.trim() || DEFAULT_SECTION_TITLE;
    setSections((current) =>
      current.map((item) =>
        item.key === section.key ? { ...item, title: nextTitle } : item,
      ),
    );

    if (previousTitle === nextTitle) {
      return;
    }

    const hasIngredients = ingredients.some(
      (item) => sectionLabel(item) === previousTitle,
    );
    if (hasIngredients) {
      await onRenameSection(previousTitle, nextTitle);
    }
  }

  function handleAddSection() {
    const nextNumber = sections.length + 1;
    const key = crypto.randomUUID();
    setSections((current) => [
      ...current,
      { key, title: `Section ${nextNumber}` },
    ]);
    setOpenByKey((current) => ({ ...current, [key]: true }));
  }

  function handleDeleteOrClearSection(section: RecipeEditorSection) {
    const sectionIngredients = ingredientsBySection.get(section.key) ?? [];
    const ingredientIds = sectionIngredients.map((item) => item.id);

    if (sections.length === 1) {
      if (ingredientIds.length > 0) {
        onDeleteSectionIngredients(ingredientIds);
      }
      setSections([{ key: crypto.randomUUID(), title: DEFAULT_SECTION_TITLE }]);
      return;
    }

    if (ingredientIds.length > 0) {
      onDeleteSectionIngredients(ingredientIds);
    }
    setSections((current) => current.filter((item) => item.key !== section.key));
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
          Ingredients by section
        </h2>
        <p className="text-sm text-muted">
          {ingredients.length} item{ingredients.length === 1 ? "" : "s"}
        </p>
      </div>

      {sections.map((section, index) => {
        const sectionIngredients = ingredientsBySection.get(section.key) ?? [];
        const isOpen = isSectionOpen(section.key, index);
        const isOnlySection = sections.length === 1;

        return (
          <SectionCard
            key={section.key}
            section={section}
            isOpen={isOpen}
            isOnlySection={isOnlySection}
            ingredients={sectionIngredients}
            onToggle={() => toggleSectionOpen(section.key, index)}
            onTitleBlur={(previous, draft) => void commitSectionTitle(section, previous, draft)}
            onDeleteOrClear={() => handleDeleteOrClearSection(section)}
            onAdd={onAdd}
            onUpdate={onUpdate}
            onRemove={onRemove}
          />
        );
      })}

      <button
        type="button"
        onClick={handleAddSection}
        className="inline-flex w-full items-center justify-center gap-2 rounded-xl border border-dashed border-border bg-white px-4 py-3 text-sm font-medium text-brand-700 transition hover:border-brand-300 hover:bg-brand-50"
      >
        <Plus className="h-4 w-4" aria-hidden />
        Add section
      </button>
    </div>
  );
}

function SectionCard({
  section,
  isOpen,
  isOnlySection,
  ingredients,
  onToggle,
  onTitleBlur,
  onDeleteOrClear,
  onAdd,
  onUpdate,
  onRemove,
}: {
  section: RecipeEditorSection;
  isOpen: boolean;
  isOnlySection: boolean;
  ingredients: RecipeIngredient[];
  onToggle: () => void;
  onTitleBlur: (previousTitle: string, draftTitle: string) => void;
  onDeleteOrClear: () => void;
  onAdd: (payload: CreateRecipeIngredientPayload) => Promise<void>;
  onUpdate: (ingredientId: string, payload: UpdateRecipeIngredientPayload) => Promise<void>;
  onRemove: (ingredientId: string) => void;
}) {
  const [draftTitle, setDraftTitle] = useState(section.title);
  const titleBeforeEdit = useRef(section.title);

  useEffect(() => {
    setDraftTitle(section.title);
  }, [section.title]);

  async function handleAdd(payload: CreateRecipeIngredientPayload) {
    const nextTitle = draftTitle.trim() || DEFAULT_SECTION_TITLE;
    if (nextTitle !== section.title) {
      onTitleBlur(section.title, draftTitle);
    }
    await onAdd({
      ...payload,
      section: nextTitle,
    });
  }

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-white shadow-sm">
      <div className="flex items-center gap-2 border-b border-border px-3 py-2.5">
        <button
          type="button"
          onClick={onToggle}
          className="rounded-lg p-1.5 text-muted transition hover:bg-brand-50 hover:text-brand-700"
          aria-expanded={isOpen}
          aria-label={isOpen ? "Collapse section" : "Expand section"}
        >
          {isOpen ? (
            <ChevronDown className="h-4 w-4" aria-hidden />
          ) : (
            <ChevronRight className="h-4 w-4" aria-hidden />
          )}
        </button>
        <input
          type="text"
          value={draftTitle}
          onFocus={() => {
            titleBeforeEdit.current = draftTitle;
          }}
          onChange={(event) => setDraftTitle(event.target.value)}
          onBlur={() => onTitleBlur(titleBeforeEdit.current, draftTitle)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.currentTarget.blur();
            }
          }}
          className="min-w-0 flex-1 rounded-lg border border-transparent bg-transparent px-2 py-1.5 text-sm font-semibold text-ink outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          aria-label="Section name"
        />
        <span className="shrink-0 rounded-full bg-surface px-2.5 py-0.5 text-xs font-medium text-muted">
          {ingredients.length} item{ingredients.length === 1 ? "" : "s"}
        </span>
        <button
          type="button"
          onClick={onDeleteOrClear}
          className="shrink-0 rounded-lg px-2.5 py-1.5 text-xs font-medium text-muted transition hover:bg-red-50 hover:text-red-700"
        >
          {isOnlySection ? "Clear" : "Delete"}
        </button>
      </div>

      {isOpen && (
        <div className="space-y-3 p-3 sm:p-4">
          <div className="hidden gap-2 px-1 text-xs font-medium uppercase tracking-wide text-muted sm:grid sm:grid-cols-[5.5rem_minmax(0,1fr)_8.5rem_4.5rem]">
            <span>Quantity</span>
            <span>Ingredient</span>
            <span>Category</span>
            <span className="text-right">Actions</span>
          </div>

          {ingredients.length === 0 ? (
            <p className="px-1 text-sm text-muted">No ingredients yet. Add some below.</p>
          ) : (
            <ul className="space-y-2">
              {ingredients.map((ingredient) => (
                <EditableIngredientRow
                  key={ingredient.id}
                  ingredient={ingredient}
                  sectionTitle={section.title}
                  onUpdate={onUpdate}
                  onRemove={onRemove}
                />
              ))}
            </ul>
          )}

          <AddIngredientRow
            sectionTitle={draftTitle.trim() || DEFAULT_SECTION_TITLE}
            onAdd={handleAdd}
          />
        </div>
      )}
    </section>
  );
}

function EditableIngredientRow({
  ingredient,
  sectionTitle,
  onUpdate,
  onRemove,
}: {
  ingredient: RecipeIngredient;
  sectionTitle: string;
  onUpdate: (ingredientId: string, payload: UpdateRecipeIngredientPayload) => Promise<void>;
  onRemove: (ingredientId: string) => void;
}) {
  const [quantity, setQuantity] = useState(ingredient.quantity ?? "");
  const [name, setName] = useState(ingredient.name);
  const [category, setCategory] = useState(ingredient.category || "Other");
  const [error, setError] = useState<string | null>(null);
  const saveTimer = useRef<number | null>(null);

  useEffect(() => {
    setQuantity(ingredient.quantity ?? "");
    setName(ingredient.name);
    setCategory(ingredient.category || "Other");
  }, [ingredient.id, ingredient.quantity, ingredient.name, ingredient.category]);

  function scheduleSave(draftFields: {
    quantity: string;
    name: string;
    category: string;
  }) {
    if (saveTimer.current) {
      window.clearTimeout(saveTimer.current);
    }

    saveTimer.current = window.setTimeout(() => {
      void persist(draftFields);
    }, 400);
  }

  async function persist(draftFields: {
    quantity: string;
    name: string;
    category: string;
  }) {
    const trimmedName = draftFields.name.trim();
    if (!trimmedName) {
      setError("Ingredient name is required.");
      return;
    }

    setError(null);
    try {
      await onUpdate(ingredient.id, {
        name: trimmedName,
        quantity: draftFields.quantity.trim() || null,
        category: draftFields.category,
        section: sectionTitle.trim() || DEFAULT_SECTION_TITLE,
        sortOrder: ingredient.sortOrder,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save ingredient");
    }
  }

  return (
    <li className="space-y-1">
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-[5.5rem_minmax(0,1fr)_8.5rem_4.5rem] sm:items-center">
        <input
          type="text"
          value={quantity}
          onChange={(event) => {
            const value = event.target.value;
            setQuantity(value);
            scheduleSave({ quantity: value, name, category });
          }}
          onBlur={() => void persist({ quantity, name, category })}
          placeholder="Qty"
          aria-label="Quantity"
          className="w-full rounded-xl border border-border px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
        />
        <input
          type="text"
          value={name}
          onChange={(event) => {
            const value = event.target.value;
            setName(value);
            scheduleSave({ quantity, name: value, category });
          }}
          onBlur={() => void persist({ quantity, name, category })}
          placeholder="Ingredient name"
          aria-label="Ingredient name"
          className="w-full rounded-xl border border-border px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
        />
        <select
          value={category}
          onChange={(event) => {
            const value = event.target.value;
            setCategory(value);
            void persist({ quantity, name, category: value });
          }}
          aria-label="Category"
          className="w-full rounded-xl border border-border bg-white px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
        >
          {CATEGORIES.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
        <button
          type="button"
          onClick={() => onRemove(ingredient.id)}
          className="inline-flex items-center justify-center rounded-xl p-2 text-muted transition hover:bg-red-50 hover:text-red-600 sm:justify-self-end"
          aria-label={`Remove ${ingredient.name}`}
        >
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
      {error && <p className="text-xs text-red-600">{error}</p>}
    </li>
  );
}

function AddIngredientRow({
  sectionTitle,
  onAdd,
}: {
  sectionTitle: string;
  onAdd: (payload: CreateRecipeIngredientPayload) => Promise<void>;
}) {
  const [quantity, setQuantity] = useState("");
  const [name, setName] = useState("");
  const [category, setCategory] = useState<string>("Other");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (name.trim()) {
      setCategory(suggestCategory(name));
    }
  }, [name]);

  async function handleAdd() {
    const trimmed = name.trim();
    if (!trimmed) {
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await onAdd({
        name: trimmed,
        quantity: quantity.trim() || null,
        category,
        section: sectionTitle.trim() || DEFAULT_SECTION_TITLE,
      });
      setName("");
      setQuantity("");
      setCategory("Other");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not add ingredient");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="space-y-2 border-t border-border pt-3">
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-[5.5rem_minmax(0,1fr)_8.5rem_4.5rem] sm:items-center">
        <input
          type="text"
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          placeholder="Quantity"
          aria-label={`Quantity for new ingredient in ${sectionTitle}`}
          className="w-full rounded-xl border border-border px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
        />
        <input
          type="text"
          value={name}
          onChange={(event) => setName(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              void handleAdd();
            }
          }}
          placeholder="Ingredient name"
          aria-label={`New ingredient in ${sectionTitle}`}
          className="w-full rounded-xl border border-border px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
        />
        <select
          value={category}
          onChange={(event) => setCategory(event.target.value)}
          aria-label="Category"
          className="w-full rounded-xl border border-border bg-white px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
        >
          {CATEGORIES.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
        <button
          type="button"
          disabled={submitting || !name.trim()}
          onClick={() => void handleAdd()}
          className="inline-flex items-center justify-center gap-1 rounded-xl bg-brand-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <Plus className="h-4 w-4" aria-hidden />
          Add
        </button>
      </div>
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  );
}
