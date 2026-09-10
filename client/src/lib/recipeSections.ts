export const DEFAULT_SECTION_TITLE = "Ingredients";

export function isDefaultSectionTitle(section: string | null | undefined): boolean {
  return !section?.trim() || section.trim().toLowerCase() === DEFAULT_SECTION_TITLE.toLowerCase();
}

export function isUsableSectionTitle(section: string | null | undefined): boolean {
  const trimmed = section?.trim();
  if (!trimmed || isDefaultSectionTitle(trimmed)) {
    return false;
  }

  const letters = [...trimmed].filter((char) => /\p{L}/u.test(char)).length;
  if (letters < 3) {
    return false;
  }

  const significant = [...trimmed].filter((char) => !/\s/u.test(char)).length;
  return letters >= significant * 0.6;
}

/** Persist the UI placeholder as null so it is not saved as a real section. */
export function toPersistedSection(section: string | null | undefined): string | null {
  const trimmed = section?.trim();
  return isUsableSectionTitle(trimmed) ? trimmed! : null;
}

export function sectionLabel(section: string | null | undefined): string {
  return toPersistedSection(section) ?? DEFAULT_SECTION_TITLE;
}

export function sectionLabelFromIngredient(ingredient: {
  section: string | null;
}): string {
  return sectionLabel(ingredient.section);
}
