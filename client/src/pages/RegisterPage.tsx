import { ShoppingCart, UserPlus } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import * as usersApi from "../api/users";
import { APP_NAME } from "../lib/appName";
import {
  formatPhoneInput,
  validateEmail,
  validatePhone,
  validatePreferredName,
} from "../lib/registrationValidation";
import { GENDER_OPTIONS } from "../types/user";

export function RegisterPage() {
  const navigate = useNavigate();
  const [preferredName, setPreferredName] = useState("");
  const [gender, setGender] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const preferredNameError = validatePreferredName(preferredName);
    if (preferredNameError) {
      setError(preferredNameError);
      setBusy(false);
      return;
    }

    const emailError = validateEmail(email);
    if (emailError) {
      setError(emailError);
      setBusy(false);
      return;
    }

    const phoneError = validatePhone(phoneNumber);
    if (phoneError) {
      setError(phoneError);
      setBusy(false);
      return;
    }

    if (!password) {
      setError("Password is required.");
      setBusy(false);
      return;
    }

    if (password.length < 6) {
      setError("Password must be at least 6 characters.");
      setBusy(false);
      return;
    }

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      setBusy(false);
      return;
    }

    try {
      await usersApi.register({
        email: email.trim(),
        password,
        preferredName: preferredName.trim(),
        gender: gender || null,
        phoneNumber: phoneNumber.trim() || null,
      });
      navigate("/login?registered=1");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create account");
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
        <h1 className="text-3xl font-bold tracking-tight text-ink">Create account</h1>
        <p className="mt-2 text-sm text-muted">Join {APP_NAME} to share lists and recipes with friends.</p>
      </div>

      <form
        onSubmit={(event) => void handleSubmit(event)}
        className="rounded-2xl border border-border bg-white p-6 shadow-sm"
      >
        <label className="mb-4 block">
          <span className="mb-2 block text-sm font-medium text-ink">
            Preferred name <span className="text-red-600">*</span>
          </span>
          <input
            value={preferredName}
            onChange={(event) => setPreferredName(event.target.value)}
            autoComplete="nickname"
            required
            minLength={2}
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <span className="mt-1 block text-xs text-muted">
            At least 2 characters, letters and numbers only
          </span>
        </label>

        <label className="mb-4 block">
          <span className="mb-2 block text-sm font-medium text-ink">Gender</span>
          <select
            value={gender}
            onChange={(event) => setGender(event.target.value)}
            className="w-full rounded-xl border border-border bg-white px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          >
            {GENDER_OPTIONS.map((option) => (
              <option key={option.label} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label className="mb-4 block">
          <span className="mb-2 block text-sm font-medium text-ink">
            Email <span className="text-red-600">*</span>
          </span>
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            autoComplete="email"
            required
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="mb-4 block">
          <span className="mb-2 block text-sm font-medium text-ink">Phone number</span>
          <input
            type="tel"
            value={phoneNumber}
            onChange={(event) => setPhoneNumber(formatPhoneInput(event.target.value))}
            placeholder="555-123-4567"
            autoComplete="tel"
            inputMode="numeric"
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="mb-4 block">
          <span className="mb-2 block text-sm font-medium text-ink">Password</span>
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="new-password"
            required
            minLength={6}
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
            minLength={6}
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
          disabled={busy}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <UserPlus className="h-4 w-4" aria-hidden />
          {busy ? "Creating account…" : "Create account"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-muted">
        Already have an account?{" "}
        <Link to="/login" className="font-medium text-brand-700 hover:text-brand-600">
          Sign in
        </Link>
      </p>
    </div>
  );
}
