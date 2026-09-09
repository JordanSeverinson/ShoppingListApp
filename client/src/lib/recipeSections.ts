export const DEFAULT_SECTION_TITLE = "Ingredients";

export function isDefaultSectionTitle(section: string | null | undefined): boolean {
  return !section?.trim() || section.trim().toLowerCase() === DEFAULT_SECTION_TITLE.toLowerCase();
}

/** Persist the UI placeholder as null so it is not saved as a real section. */
export function toPersistedSection(section: string | null | undefined): string | null {
  const trimmed = section?.trim();
  return !trimmed || isDefaultSectionTitle(trimmed) ? null : trimmed;
}

export function sectionLabel(section: string | null | undefined): string {
  return toPersistedSection(section) ?? DEFAULT_SECTION_TITLE;
}

export function sectionLabelFromIngredient(ingredient: {
  section: string | null;
}): string {
  return sectionLabel(ingredient.section);
}
