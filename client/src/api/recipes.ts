import { apiFormRequest, apiKeepaliveRequest, apiRequest } from "../lib/apiClient";
import type {
  CreateRecipeIngredientPayload,
  DeleteRecipeIngredientsResponse,
  ImportRecipeResponse,
  RecipeDetail,
  RecipeImageImportMode,
  RecipeSummary,
  RecipeSummaryResponse,
  UploadRecipeImageResponse,
} from "../types/recipe";

export function fetchMyRecipes(): Promise<RecipeSummaryResponse> {
  return apiRequest<RecipeSummaryResponse>("/api/recipes");
}

export function createRecipe(
  name: string,
  recipeType?: string | null,
): Promise<RecipeSummary> {
  return apiRequest<RecipeSummary>("/api/recipes", {
    method: "POST",
    body: JSON.stringify({ name, recipeType: recipeType ?? null }),
  });
}

export function shareRecipe(
  recipeId: string,
  friendUserIds: string[],
): Promise<{ invitedCount: number; skippedCount: number; message: string }> {
  return apiRequest(`/api/recipes/${recipeId}/shares`, {
    method: "POST",
    body: JSON.stringify({ friendUserIds }),
  });
}

export function acceptRecipeShare(permissionId: string): Promise<RecipeSummary> {
  return apiRequest<RecipeSummary>(`/api/recipes/shares/${permissionId}/accept`, {
    method: "POST",
  });
}

export function declineRecipeShare(permissionId: string): Promise<void> {
  return apiRequest<void>(`/api/recipes/shares/${permissionId}/decline`, {
    method: "POST",
  });
}

export function fetchRecipe(recipeId: string): Promise<RecipeDetail> {
  return apiRequest<RecipeDetail>(`/api/recipes/${recipeId}`);
}

export function updateRecipe(
  recipeId: string,
  payload: { name?: string; recipeType?: string | null },
): Promise<RecipeSummary> {
  return apiRequest<RecipeSummary>(`/api/recipes/${recipeId}`, {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}

export function renameRecipe(recipeId: string, name: string): Promise<RecipeSummary> {
  return updateRecipe(recipeId, { name });
}

export function deleteRecipe(recipeId: string): Promise<void> {
  return apiRequest<void>(`/api/recipes/${recipeId}`, { method: "DELETE" });
}

export function createRecipeIngredient(
  recipeId: string,
  payload: CreateRecipeIngredientPayload,
): Promise<import("../types/recipe").RecipeIngredient> {
  return apiRequest(`/api/recipes/${recipeId}/ingredients`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function updateRecipeIngredient(
  recipeId: string,
  ingredientId: string,
  payload: import("../types/recipe").UpdateRecipeIngredientPayload,
): Promise<import("../types/recipe").RecipeIngredient> {
  return apiRequest(`/api/recipes/${recipeId}/ingredients/${ingredientId}`, {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}

export function renameRecipeSection(
  recipeId: string,
  from: string,
  to: string,
): Promise<void> {
  return apiRequest(`/api/recipes/${recipeId}/ingredients/rename-section`, {
    method: "POST",
    body: JSON.stringify({ from, to }),
  });
}

export function deleteRecipeIngredients(
  recipeId: string,
  ingredientIds: string[],
  options?: { keepalive?: boolean },
): Promise<DeleteRecipeIngredientsResponse> {
  const path = `/api/recipes/${recipeId}/ingredients/delete-many`;
  const init: RequestInit = {
    method: "POST",
    body: JSON.stringify({ ingredientIds }),
  };

  if (options?.keepalive) {
    return apiKeepaliveRequest<DeleteRecipeIngredientsResponse>(path, init);
  }

  return apiRequest<DeleteRecipeIngredientsResponse>(path, init);
}

export function replaceRecipeSteps(
  recipeId: string,
  steps: string[],
): Promise<import("../types/recipe").RecipeStep[]> {
  return apiRequest(`/api/recipes/${recipeId}/steps`, {
    method: "PUT",
    body: JSON.stringify({ steps }),
  });
}

export function saveRecipeContent(
  recipeId: string,
  content: import("../types/recipe").RecipeContentDocument,
): Promise<import("../types/recipe").RecipeContentDocument> {
  return apiRequest(`/api/recipes/${recipeId}/content`, {
    method: "PUT",
    body: JSON.stringify({ content }),
  });
}

export function uploadRecipeImage(
  recipeId: string,
  file: File,
  importMode: RecipeImageImportMode = "FullRecipeWithSteps",
): Promise<UploadRecipeImageResponse> {
  const formData = new FormData();
  formData.append("image", file);
  formData.append("importMode", importMode);

  return apiFormRequest<UploadRecipeImageResponse>(
    `/api/recipes/${recipeId}/upload-image`,
    formData,
  );
}

export function importRecipeToList(
  listId: string,
  recipeId: string,
): Promise<ImportRecipeResponse> {
  return apiRequest<ImportRecipeResponse>(`/api/lists/${listId}/import-recipe/${recipeId}`, {
    method: "POST",
  });
}
