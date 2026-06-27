import categoryData from "../data/category-keywords.json";

export type Category = (typeof categoryData.categories)[number];

type TrieNode = {
  children: Map<string, TrieNode>;
  category: Category | null;
};

function createNode(): TrieNode {
  return { children: new Map(), category: null };
}

/** Normalizes item names for dictionary matching. */
export function normalizeItemName(name: string): string {
  return name
    .toLowerCase()
    .trim()
    .replace(/-/g, " ")
    .replace(/[^a-z0-9\s]/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

export class CategoryTrie {
  private readonly root = createNode();

  constructor(keywords: Record<string, string[]>) {
    const entries: Array<{ term: string; category: Category }> = [];

    for (const [category, terms] of Object.entries(keywords)) {
      for (const term of terms) {
        const normalized = normalizeItemName(term);
        if (normalized) {
          entries.push({ term: normalized, category: category as Category });
        }
      }
    }

    entries.sort((a, b) => b.term.length - a.term.length);

    for (const { term, category } of entries) {
      this.insert(term, category);
    }
  }

  insert(term: string, category: Category): void {
    let node = this.root;

    for (const char of term) {
      let next = node.children.get(char);
      if (!next) {
        next = createNode();
        node.children.set(char, next);
      }
      node = next;
    }

    node.category = category;
  }

  lookupToken(token: string): Category | null {
    let node = this.root;

    for (const char of token) {
      const next = node.children.get(char);
      if (!next) {
        return null;
      }
      node = next;
    }

    return node.category;
  }

  findLongestMatchAt(text: string, start: number): { length: number; category: Category } | null {
    let node = this.root;
    let best: { length: number; category: Category } | null = null;

    for (let index = start; index < text.length; index += 1) {
      const next = node.children.get(text[index]!);
      if (!next) {
        break;
      }

      node = next;
      if (node.category) {
        const endsAtWordBoundary =
          index === text.length - 1 || text[index + 1] === " ";
        const startsAtWordBoundary = start === 0 || text[start - 1] === " ";

        if (startsAtWordBoundary && endsAtWordBoundary) {
          best = { length: index - start + 1, category: node.category };
        }
      }
    }

    return best;
  }

  suggest(name: string): Category {
    const text = normalizeItemName(name);
    if (!text) {
      return "Other";
    }

    let best: { length: number; category: Category } | null = null;

    for (let index = 0; index < text.length; index += 1) {
      if (index > 0 && text[index - 1] !== " ") {
        continue;
      }

      const match = this.findLongestMatchAt(text, index);
      if (match && (!best || match.length > best.length)) {
        best = match;
      }
    }

    if (best) {
      return best.category;
    }

    for (const token of text.split(" ")) {
      const category = this.lookupToken(token);
      if (category) {
        return category;
      }
    }

    return "Other";
  }
}

export const categoryTrie = new CategoryTrie(categoryData.keywords);
