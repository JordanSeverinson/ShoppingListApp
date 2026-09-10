import { useEffect } from "react";

export function useAutoDismissMessage(
  message: string | null,
  setMessage: (value: string | null) => void,
  timeoutMs = 3000,
) {
  useEffect(() => {
    if (!message) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setMessage(null);
    }, timeoutMs);

    return () => window.clearTimeout(timeoutId);
  }, [message, setMessage, timeoutMs]);
}
