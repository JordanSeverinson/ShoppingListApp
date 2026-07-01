export function readAuthTokenFromUrl(): string | null {
  const hash = window.location.hash.startsWith("#")
    ? window.location.hash.slice(1)
    : window.location.hash;
  const hashParams = new URLSearchParams(hash);
  const hashToken = hashParams.get("token");
  if (hashToken) {
    return hashToken;
  }

  return new URLSearchParams(window.location.search).get("token");
}

export function stripAuthTokenFromHistory(): void {
  if (window.location.hash) {
    window.history.replaceState(null, "", window.location.pathname);
  }
}
