export const RECIPE_TYPES = [
  "Main Course",
  "Side Dish",
  "Snack",
  "Dessert",
  "Drink",
] as const;

export type RecipeType = (typeof RECIPE_TYPES)[number];
