import { DEFAULT_SECTION_TITLE, isDefaultSectionTitle, toPersistedSection } from "../lib/recipeSections";

export interface RecipeSubCategoryBlock {
  description: string;
  ingredients: string[];
}

export interface RecipeContentRoot {
  cookingSteps: string[];
  subCategories?: RecipeSubCategoryBlock[];
  [key: string]: RecipeSubCategoryBlock | RecipeSubCategoryBlock[] | string[] | undefined;
}

export interface RecipeContentDocument {
  recipe: RecipeContentRoot;
}

function getRecipeSubCategories(
  content: RecipeContentDocument | null | undefined,
): RecipeSubCategoryBlock[] {
  if (!content?.recipe) {
    return [];
  }

  const recipe = content.recipe as RecipeContentRoot & {
    subCategories?: RecipeSubCategoryBlock[];
  };

  if (Array.isArray(recipe.subCategories) && recipe.subCategories.length > 0) {
    return recipe.subCategories;
  }

  return Object.entries(recipe)
    .filter(([key, value]) => key.startsWith("subCategory") && isSubCategoryBlock(value))
    .sort(([left], [right]) => subCategorySortKey(left) - subCategorySortKey(right))
    .map(([, value]) => value as RecipeSubCategoryBlock);
}

function subCategorySortKey(key: string): number {
  const match = /subCategory(\d+)/i.exec(key);
  return match ? Number.parseInt(match[1], 10) : Number.MAX_SAFE_INTEGER;
}

function normalizeSubCategoriesForDisplay(
  blocks: RecipeSubCategoryBlock[],
): RecipeSubCategoryBlock[] {
  if (blocks.length <= 1) {
    if (blocks.length === 0) {
      return [];
    }

    return [
      {
        description: DEFAULT_SECTION_TITLE,
        ingredients: blocks[0].ingredients,
      },
    ];
  }

  return blocks;
}

function foldPlaceholderSiblingBlocks(
  blocks: RecipeSubCategoryBlock[],
): RecipeSubCategoryBlock[] {
  const named = blocks.filter((block) => !isDefaultSectionTitle(block.description));
  if (named.length === 0) {
    return blocks;
  }

  const leftover = blocks.filter((block) => isDefaultSectionTitle(block.description));
  if (leftover.length === 0) {
    return named;
  }

  const [first, ...rest] = named;
  return [
    {
      ...first,
      ingredients: [...leftover.flatMap((block) => block.ingredients), ...first.ingredients],
    },
    ...rest,
  ];
}

function isSubCategoryBlock(value: unknown): value is RecipeSubCategoryBlock {
  return (
    typeof value === "object" &&
    value !== null &&
    "description" in value &&
    "ingredients" in value &&
    Array.isArray((value as RecipeSubCategoryBlock).ingredients)
  );
}

function formatIngredientLine(ingredient: RecipeIngredient): string {
  const name = ingredient.name.trim();
  const quantity = ingredient.quantity?.trim();
  return quantity ? `${quantity} ${name}` : name;
}

function buildSubCategoriesFromIngredients(
  ingredients: RecipeIngredient[],
): RecipeSubCategoryBlock[] {
  if (ingredients.length === 0) {
    return [];
  }

  const groups = new Map<string, string[]>();
  const sectionOrder: string[] = [];

  for (const ingredient of [...ingredients].sort(
    (a, b) => a.sortOrder - b.sortOrder,
  )) {
    const label = toPersistedSection(ingredient.section) ?? DEFAULT_SECTION_TITLE;
    if (!groups.has(label)) {
      groups.set(label, []);
      sectionOrder.push(label);
    }
    groups.get(label)!.push(formatIngredientLine(ingredient));
  }

  return sectionOrder.map((description) => ({
    description,
    ingredients: groups.get(description)!,
  }));
}

export function buildRecipeContentDocument(
  ingredients: RecipeIngredient[],
  cookingSteps: string[],
): RecipeContentDocument {
  const subCategories = foldPlaceholderSiblingBlocks(
    buildSubCategoriesFromIngredients(ingredients),
  );
  return {
    recipe: {
      cookingSteps,
      ...(subCategories.length > 0 ? { subCategories } : {}),
    },
  };
}

function inferSectionGroupsFromIngredients(
  ingredients: RecipeIngredient[],
): RecipeIngredient[] {
  if (ingredients.some((ingredient) => toPersistedSection(ingredient.section))) {
    return ingredients;
  }

  const glazeStart = ingredients.findIndex((ingredient) => {
    const text = formatIngredientLine(ingredient).toLowerCase();
    return (
      text.includes("confectioners") ||
      text.includes("confectioner's") ||
      text.includes("powdered sugar") ||
      text.includes("icing sugar")
    );
  });

  if (glazeStart <= 0 || glazeStart >= ingredients.length - 1) {
    return ingredients;
  }

  return ingredients.map((ingredient, index) => ({
    ...ingredient,
    section: index < glazeStart ? "Cake" : "Glaze",
  }));
}

export function resolveRecipeDisplayData(
  content: RecipeContentDocument | null | undefined,
  ingredients: RecipeIngredient[],
  steps: string[],
): { subCategories: RecipeSubCategoryBlock[]; cookingSteps: string[]; hasMultipleSubsections: boolean } {
  const cookingSteps =
    steps.length > 0 ? steps : (content?.recipe.cookingSteps ?? []);

  const fromContent = getRecipeSubCategories(content);
  const fromIngredients = buildSubCategoriesFromIngredients(
    inferSectionGroupsFromIngredients(ingredients),
  );

  let rawBlocks: RecipeSubCategoryBlock[];
  if (fromContent.length > 1) {
    rawBlocks = fromContent;
  } else if (fromIngredients.length > 1) {
    rawBlocks = fromIngredients;
  } else if (fromContent.length === 1) {
    rawBlocks = fromContent;
  } else {
    rawBlocks = fromIngredients;
  }

  const namedUsable = rawBlocks.filter((block) => toPersistedSection(block.description));
  if (namedUsable.length <= 1) {
    rawBlocks = [
      {
        description: DEFAULT_SECTION_TITLE,
        ingredients: rawBlocks.flatMap((block) => block.ingredients),
      },
    ].filter((block) => block.ingredients.length > 0);
  } else {
    rawBlocks = foldPlaceholderSiblingBlocks(rawBlocks);
  }

  return {
    subCategories:
      rawBlocks.length <= 1
        ? normalizeSubCategoriesForDisplay(rawBlocks)
        : rawBlocks,
    cookingSteps,
    hasMultipleSubsections: namedUsable.length > 1,
  };
}

export interface RecipeIngredient {
  id: string;
  recipeId: string;
  name: string;
  quantity: string | null;
  category: string;
  section: string | null;
  sortOrder: number;
}

export interface RecipeStep {
  id: string;
  recipeId: string;
  text: string;
  sortOrder: number;
}

export interface RecipeSummary {
  id: string;
  name: string;
  recipeType: string | null;
  isOwner: boolean;
  ingredientCount: number;
  updatedAt?: string;
}

export interface PendingRecipeShare {
  id: string;
  recipeId: string;
  recipeName: string;
  invitedByName: string;
  invitedByUserId: string;
  ingredientCount: number;
}

export interface RecipeSummaryResponse {
  recipes: RecipeSummary[];
  pendingShares: PendingRecipeShare[];
}

export interface RecipeDetail {
  id: string;
  name: string;
  recipeType: string | null;
  isOwner: boolean;
  content: RecipeContentDocument;
  ingredients: RecipeIngredient[];
  steps: RecipeStep[];
}

export interface CreateRecipeIngredientPayload {
  name: string;
  quantity?: string | null;
  category: string;
  section?: string | null;
}

export interface UpdateRecipeIngredientPayload {
  name: string;
  quantity?: string | null;
  category: string;
  section?: string | null;
  sortOrder?: number;
}

export type RecipeImageImportMode =
  | "FullRecipeWithSteps"
  | "IngredientsOnly"
  | "CookingStepsOnly";

export interface UploadRecipeImageResponse {
  recipeId: string;
  content: RecipeContentDocument;
  ingredients: RecipeIngredient[];
  steps: RecipeStep[];
  message: string;
}

export interface DeleteRecipeIngredientsResponse {
  deletedCount: number;
  ingredientIds: string[];
}

export interface ShareRecipeResponse {
  invitedCount: number;
  skippedCount: number;
  message: string;
}

export interface ImportRecipeResponse {
  listId: string;
  items: import("./list").ListItem[];
  message: string;
}
