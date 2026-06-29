import { DEMO_USER_ID } from "../lib/userId";
import type {
  CreateRecipeIngredientPayload,
  DeleteRecipeIngredientsResponse,
  ImportRecipeResponse,
  RecipeDetail,
  RecipeSummary,
  RecipeSummaryResponse,
  UploadRecipeImageResponse,
} from "../types/recipe";

const API_BASE = import.meta.env.VITE_API_URL ?? "";

function apiHeaders(): HeadersInit {
  return {
    "Content-Type": "application/json",
    "X-User-Id": DEMO_USER_ID,
  };
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      ...apiHeaders(),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string } | null;
    throw new Error(body?.error ?? `Request failed (${response.status})`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function fetchMyRecipes(): Promise<RecipeSummaryResponse> {
  return request<RecipeSummaryResponse>("/api/recipes");
}

export function createRecipe(name: string): Promise<RecipeSummary> {
  return request<RecipeSummary>("/api/recipes", {
    method: "POST",
    body: JSON.stringify({ name }),
  });
}

export function joinRecipe(shareCode: string): Promise<RecipeSummary> {
  return request<RecipeSummary>("/api/recipes/join", {
    method: "POST",
    body: JSON.stringify({ shareCode: shareCode.trim().toUpperCase() }),
  });
}

export function fetchRecipe(recipeId: string): Promise<RecipeDetail> {
  return request<RecipeDetail>(`/api/recipes/${recipeId}`);
}

export function renameRecipe(recipeId: string, name: string): Promise<RecipeSummary> {
  return request<RecipeSummary>(`/api/recipes/${recipeId}`, {
    method: "PATCH",
    body: JSON.stringify({ name }),
  });
}

export function createRecipeIngredient(
  recipeId: string,
  payload: CreateRecipeIngredientPayload,
): Promise<import("../types/recipe").RecipeIngredient> {
  return request(`/api/recipes/${recipeId}/ingredients`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

async function keepaliveRequest<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    keepalive: true,
    headers: {
      ...apiHeaders(),
      ...init.headers,
    },
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string } | null;
    throw new Error(body?.error ?? `Request failed (${response.status})`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
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
    return keepaliveRequest<DeleteRecipeIngredientsResponse>(path, init);
  }

  return request<DeleteRecipeIngredientsResponse>(path, init);
}

export async function uploadRecipeImage(
  recipeId: string,
  file: File,
): Promise<UploadRecipeImageResponse> {
  const formData = new FormData();
  formData.append("image", file);

  const response = await fetch(`${API_BASE}/api/recipes/${recipeId}/upload-image`, {
    method: "POST",
    headers: { "X-User-Id": DEMO_USER_ID },
    body: formData,
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string } | null;
    throw new Error(body?.error ?? `Upload failed (${response.status})`);
  }

  return (await response.json()) as UploadRecipeImageResponse;
}

export function importRecipeToList(
  listId: string,
  recipeId: string,
): Promise<ImportRecipeResponse> {
  return request<ImportRecipeResponse>(`/api/lists/${listId}/import-recipe/${recipeId}`, {
    method: "POST",
  });
}
