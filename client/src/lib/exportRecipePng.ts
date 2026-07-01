import { toPng } from "html-to-image";

function sanitizeFilename(name: string): string {
  const cleaned = name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return cleaned.length > 0 ? `${cleaned}.png` : "recipe.png";
}

export async function exportRecipePng(
  element: HTMLElement,
  recipeName: string,
): Promise<void> {
  const dataUrl = await toPng(element, {
    cacheBust: true,
    pixelRatio: 2,
    backgroundColor: "#fafaf9",
  });

  const link = document.createElement("a");
  link.download = sanitizeFilename(recipeName);
  link.href = dataUrl;
  link.click();
}
