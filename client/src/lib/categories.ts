import categoryData from "../data/category-keywords.json";
import { categoryTrie, type Category } from "./categoryTrie";

export const CATEGORIES = categoryData.categories as readonly Category[];

export type { Category };

export function suggestCategory(name: string): Category {
  return categoryTrie.suggest(name);
}
