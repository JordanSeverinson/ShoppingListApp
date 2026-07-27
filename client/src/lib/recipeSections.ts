export const DEFAULT_SECTION_TITLE = "Ingredients";

export function sectionLabel(section: string | null | undefined): string {
  return section?.trim() || DEFAULT_SECTION_TITLE;
}

export function sectionLabelFromIngredient(ingredient: {
  section: string | null;
}): string {
  return sectionLabel(ingredient.section);
}
