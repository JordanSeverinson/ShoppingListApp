import { KeyRound, ShoppingCart } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import * as usersApi from "../api/users";
import { APP_NAME } from "../lib/appName";
import { validateEmail } from "../lib/registrationValidation";

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);

    const emailError = validateEmail(email);
    if (emailError) {
      setError(emailError);
      setBusy(false);
      return;
    }

    try {
      const response = await usersApi.requestPasswordReset(email.trim());
      setMessage(response.message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not send reset link");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-8">
      <div className="mb-8 text-center">
        <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
          {APP_NAME}
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink">Forgot password</h1>
        <p className="mt-2 text-sm text-muted">
          Enter your email and we&apos;ll send you a link to reset your password.
        </p>
      </div>

      {message && (
        <p className="mb-4 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
          {message}
        </p>
      )}

      <form
        onSubmit={(event) => void handleSubmit(event)}
        className="rounded-2xl border border-border bg-white p-6 shadow-sm"
      >
        <label className="mb-6 block">
          <span className="mb-2 block text-sm font-medium text-ink">Email</span>
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            autoComplete="email"
            required
            disabled={!!message}
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30 disabled:opacity-60"
          />
        </label>

        {error && (
          <p className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </p>
        )}

        <button
          type="submit"
          disabled={busy || !!message}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <KeyRound className="h-4 w-4" aria-hidden />
          {busy ? "Sending…" : "Send reset link"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-muted">
        Remember your password?{" "}
        <Link to="/login" className="font-medium text-brand-700 hover:text-brand-600">
          Sign in
        </Link>
      </p>
    </div>
  );
}
