export function readAuthTokenFromUrl(): string | null {
  const hash = window.location.hash.startsWith("#")
    ? window.location.hash.slice(1)
    : window.location.hash;
  if (!hash) {
    return null;
  }

  return new URLSearchParams(hash).get("token");
}

export function stripAuthTokenFromHistory(): void {
  if (window.location.hash) {
    window.history.replaceState(null, "", window.location.pathname + window.location.search);
    return;
  }

  if (window.location.search.includes("token=")) {
    window.history.replaceState(null, "", window.location.pathname);
  }
}
