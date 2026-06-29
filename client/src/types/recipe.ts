export interface RecipeIngredient {
  id: string;
  recipeId: string;
  name: string;
  quantity: string | null;
  category: string;
  sortOrder: number;
}

export interface RecipeSummary {
  id: string;
  name: string;
  shareCode: string;
  isOwner: boolean;
  ingredientCount: number;
}

export interface RecipeSummaryResponse {
  recipes: RecipeSummary[];
}

export interface RecipeDetail {
  id: string;
  name: string;
  shareCode: string;
  ingredients: RecipeIngredient[];
}

export interface CreateRecipeIngredientPayload {
  name: string;
  quantity?: string | null;
  category: string;
}

export interface UploadRecipeImageResponse {
  recipeId: string;
  ingredients: RecipeIngredient[];
  message: string;
}

export interface DeleteRecipeIngredientsResponse {
  deletedCount: number;
  ingredientIds: string[];
}

export interface ImportRecipeResponse {
  listId: string;
  items: import("./list").ListItem[];
  message: string;
}
