import { LogIn, ShoppingCart } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link, Navigate, useNavigate, useSearchParams } from "react-router";
import { useAuth } from "../context/AuthContext";
import { APP_NAME } from "../lib/appName";

export function LoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { login, isAuthenticated } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const showVerificationBanner =
    searchParams.get("registered") === "1" || searchParams.get("verify") === "1";
  const emailVerified = searchParams.get("verified") === "1";
  const passwordReset = searchParams.get("reset") === "1";

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      await login(email.trim(), password);
      navigate("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not sign in");
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
        <h1 className="text-3xl font-bold tracking-tight text-ink">Sign in</h1>
        <p className="mt-2 text-sm text-muted">Use your email and password to continue.</p>
      </div>

      {passwordReset && (
        <p className="mb-4 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
          Your password has been reset. You can sign in with your new password.
        </p>
      )}

      {emailVerified && (
        <p className="mb-4 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
          Your email has been verified. You can sign in now.
        </p>
      )}

      {showVerificationBanner && !emailVerified && (
        <p className="mb-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Please verify your email address before signing in. Check your inbox for a verification
          link.
        </p>
      )}

      <form
        onSubmit={(event) => void handleSubmit(event)}
        className="rounded-2xl border border-border bg-white p-6 shadow-sm"
      >
        <label className="mb-4 block">
          <span className="mb-2 block text-sm font-medium text-ink">Email</span>
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            autoComplete="email"
            required
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="mb-6 block">
          <span className="mb-2 block text-sm font-medium text-ink">Password</span>
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
            required
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <div className="mt-2 text-right">
            <Link
              to="/forgot-password"
              className="text-sm font-medium text-brand-700 hover:text-brand-600"
            >
              Forgot password?
            </Link>
          </div>
        </label>

        {error && (
          <p className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </p>
        )}

        <button
          type="submit"
          disabled={busy}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <LogIn className="h-4 w-4" aria-hidden />
          {busy ? "Signing in…" : "Sign in"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-muted">
        New to {APP_NAME}? Register{" "}
        <Link to="/register" className="font-medium text-brand-700 hover:text-brand-600">
          Here
        </Link>
      </p>

      <Link
        to="/"
        className="mt-4 text-center text-sm font-medium text-brand-700 hover:text-brand-600"
      >
        Back to home
      </Link>
    </div>
  );
}
