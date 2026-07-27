import { KeyRound, ShoppingCart } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router";
import * as usersApi from "../api/users";
import { readAuthTokenFromUrl, stripAuthTokenFromHistory } from "../lib/authTokenFromUrl";
import { APP_NAME } from "../lib/appName";
import { validatePassword } from "../lib/registrationValidation";

export function ResetPasswordPage() {
  const navigate = useNavigate();
  // Read the hash token immediately so a later history strip / auth redirect cannot lose it.
  const [token] = useState(() => readAuthTokenFromUrl());
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(
    token ? null : "This reset link is invalid or has expired.",
  );

  useEffect(() => {
    if (token) {
      stripAuthTokenFromHistory();
    }
  }, [token]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) {
      return;
    }

    setBusy(true);
    setError(null);

    const passwordError = validatePassword(password);
    if (passwordError) {
      setError(passwordError);
      setBusy(false);
      return;
    }

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      setBusy(false);
      return;
    }

    try {
      const response = await usersApi.resetPassword(token, password);
      if (response.success) {
        navigate("/login?reset=1");
      } else {
        setError(response.message);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not reset password");
    } finally {
      setBusy(false);
    }
  }

  if (!token && error) {
    return (
      <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-8">
        <div className="mb-8 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
            <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
            {APP_NAME}
          </div>
          <h1 className="text-3xl font-bold tracking-tight text-ink">Reset password</h1>
        </div>

        <div className="rounded-2xl border border-border bg-white p-6 text-center shadow-sm">
          <p className="text-ink">{error}</p>
          <Link
            to="/forgot-password"
            className="mt-6 inline-block font-medium text-brand-700 hover:text-brand-600"
          >
            Request a new reset link
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-8">
      <div className="mb-8 text-center">
        <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
          {APP_NAME}
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink">Reset password</h1>
        <p className="mt-2 text-sm text-muted">Choose a new password for your account.</p>
      </div>

      <form
        onSubmit={(event) => void handleSubmit(event)}
        className="rounded-2xl border border-border bg-white p-6 shadow-sm"
      >
        <label className="mb-4 block">
          <span className="mb-2 block text-sm font-medium text-ink">New password</span>
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="new-password"
            required
            minLength={12}
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="mb-6 block">
          <span className="mb-2 block text-sm font-medium text-ink">Confirm password</span>
          <input
            type="password"
            value={confirmPassword}
            onChange={(event) => setConfirmPassword(event.target.value)}
            autoComplete="new-password"
            required
            minLength={12}
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        {error && (
          <p className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </p>
        )}

        <button
          type="submit"
          disabled={busy || !token}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <KeyRound className="h-4 w-4" aria-hidden />
          {busy ? "Saving…" : "Reset password"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-muted">
        <Link to="/login" className="font-medium text-brand-700 hover:text-brand-600">
          Back to sign in
        </Link>
      </p>
    </div>
  );
}
