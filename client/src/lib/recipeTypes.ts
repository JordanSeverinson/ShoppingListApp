export const RECIPE_TYPES = [
  "Main Course",
  "Side Dish",
  "Snack",
  "Dessert",
  "Drink",
] as const;

export type RecipeType = (typeof RECIPE_TYPES)[number];

export function isRecipeType(value: string | null | undefined): value is RecipeType {
  return !!value && (RECIPE_TYPES as readonly string[]).includes(value);
}
