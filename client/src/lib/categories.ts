export const CATEGORIES = [
  "Produce",
  "Dairy",
  "Meat",
  "Bakery",
  "Frozen",
  "Pantry",
  "Other",
] as const;

type Category = (typeof CATEGORIES)[number];

const KEYWORD_CATEGORY: Array<{ pattern: RegExp; category: Category }> = [
  { pattern: /\b(lettuce|tomato|onion|garlic|apple|banana|carrot|spinach|broccoli|avocado|lemon|berry|fruit|vegetable|salad)\b/i, category: "Produce" },
  { pattern: /\b(milk|cheese|butter|cream|yogurt|egg)\b/i, category: "Dairy" },
  { pattern: /\b(chicken|beef|pork|turkey|bacon|sausage|fish|salmon|shrimp)\b/i, category: "Meat" },
  { pattern: /\b(bread|bun|roll|tortilla|bagel)\b/i, category: "Bakery" },
  { pattern: /\b(frozen|ice cream)\b/i, category: "Frozen" },
  { pattern: /\b(flour|sugar|rice|pasta|beans|oil|vinegar|sauce|spice|oat)\b/i, category: "Pantry" },
];

export function suggestCategory(name: string): Category {
  const trimmed = name.trim();
  if (!trimmed) return "Other";

  for (const { pattern, category } of KEYWORD_CATEGORY) {
    if (pattern.test(trimmed)) {
      return category;
    }
  }

  return "Other";
}
