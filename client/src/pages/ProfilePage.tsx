import { ArrowLeft, KeyRound, Save, UserCircle } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import * as usersApi from "../api/users";
import { useAuth } from "../context/AuthContext";
import { validatePassword, validatePreferredName } from "../lib/registrationValidation";
import { ShareCodeCopy } from "../components/ShareCodeCopy";
import { APP_NAME } from "../lib/appName";
import { GENDER_OPTIONS } from "../types/user";

export function ProfilePage() {
  const { user, isAuthenticated, loading, refreshUser } = useAuth();
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [preferredName, setPreferredName] = useState("");
  const [gender, setGender] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [passwordBusy, setPasswordBusy] = useState(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordMessage, setPasswordMessage] = useState<string | null>(null);

  useEffect(() => {
    if (user) {
      setEmail(user.email);
      setPhoneNumber(user.phoneNumber ?? "");
      setPreferredName(user.preferredName ?? "");
      setGender(user.gender ?? "");
    }
  }, [user]);

  if (!loading && !isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (loading || !user) {
    return (
      <div className="mx-auto min-h-screen max-w-lg px-4 py-12 text-center text-muted">
        Loading profile…
      </div>
    );
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);

    const preferredNameError = validatePreferredName(preferredName);
    if (preferredNameError) {
      setError(preferredNameError);
      setBusy(false);
      return;
    }

    try {
      await usersApi.updateCurrentUser({
        email: email.trim(),
        phoneNumber: phoneNumber.trim() || null,
        preferredName: preferredName.trim() || null,
        gender: gender || null,
      });
      await refreshUser();
      setMessage("Profile saved.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save profile");
    } finally {
      setBusy(false);
    }
  }

  async function handlePasswordSubmit(event: FormEvent) {
    event.preventDefault();
    setPasswordBusy(true);
    setPasswordError(null);
    setPasswordMessage(null);

    if (!currentPassword) {
      setPasswordError("Current password is required.");
      setPasswordBusy(false);
      return;
    }

    const newPasswordError = validatePassword(newPassword);
    if (newPasswordError) {
      setPasswordError(newPasswordError);
      setPasswordBusy(false);
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordError("Passwords do not match.");
      setPasswordBusy(false);
      return;
    }

    try {
      const response = await usersApi.changePassword(currentPassword, newPassword);
      setPasswordMessage(response.message);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      setPasswordError(err instanceof Error ? err.message : "Could not change password");
    } finally {
      setPasswordBusy(false);
    }
  }

  return (
    <div className="mx-auto min-h-screen max-w-lg px-4 py-8 sm:px-6 sm:py-12">
      <Link
        to="/"
        className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-brand-700"
      >
        <ArrowLeft className="h-4 w-4" aria-hidden />
        Home
      </Link>

      <header className="mb-8">
        <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <UserCircle className="h-3.5 w-3.5" aria-hidden />
          {APP_NAME}
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink">Your profile</h1>
        <p className="mt-2 text-muted">Update how you appear and how friends can reach you.</p>
      </header>

      <section className="mb-6 rounded-2xl border border-border bg-white p-4 shadow-sm">
        <ShareCodeCopy shareCode={user.friendCode} label="Your friend code" />
      </section>

      <form
        onSubmit={(event) => void handleSubmit(event)}
        className="space-y-5 rounded-2xl border border-border bg-white p-6 shadow-sm"
      >
        <label className="block">
          <span className="mb-2 block text-sm font-medium text-ink">Preferred name</span>
          <input
            value={preferredName}
            onChange={(event) => setPreferredName(event.target.value)}
            placeholder={user.firstName}
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <span className="mt-1 block text-xs text-muted">
            Shown in greetings like &quot;Welcome back, …&quot;
          </span>
        </label>

        <label className="block">
          <span className="mb-2 block text-sm font-medium text-ink">Email</span>
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
            autoComplete="email"
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="block">
          <span className="mb-2 block text-sm font-medium text-ink">Phone number</span>
          <input
            type="tel"
            value={phoneNumber}
            onChange={(event) => setPhoneNumber(event.target.value)}
            placeholder="+1 555 123 4567"
            autoComplete="tel"
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="block">
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

        {error && (
          <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </p>
        )}

        {message && (
          <p className="rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
            {message}
          </p>
        )}

        <button
          type="submit"
          disabled={busy}
          className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <Save className="h-4 w-4" aria-hidden />
          {busy ? "Saving…" : "Save profile"}
        </button>
      </form>

      <form
        onSubmit={(event) => void handlePasswordSubmit(event)}
        className="mt-6 space-y-5 rounded-2xl border border-border bg-white p-6 shadow-sm"
      >
        <div>
          <h2 className="text-lg font-semibold text-ink">Change password</h2>
          <p className="mt-1 text-sm text-muted">Use a strong password with at least 12 characters.</p>
        </div>

        <label className="block">
          <span className="mb-2 block text-sm font-medium text-ink">Current password</span>
          <input
            type="password"
            value={currentPassword}
            onChange={(event) => setCurrentPassword(event.target.value)}
            autoComplete="current-password"
            required
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="block">
          <span className="mb-2 block text-sm font-medium text-ink">New password</span>
          <input
            type="password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            autoComplete="new-password"
            required
            minLength={12}
            className="w-full rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
        </label>

        <label className="block">
          <span className="mb-2 block text-sm font-medium text-ink">Confirm new password</span>
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

        {passwordError && (
          <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {passwordError}
          </p>
        )}

        {passwordMessage && (
          <p className="rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
            {passwordMessage}
          </p>
        )}

        <button
          type="submit"
          disabled={passwordBusy}
          className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-brand-600 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
        >
          <KeyRound className="h-4 w-4" aria-hidden />
          {passwordBusy ? "Updating…" : "Update password"}
        </button>
      </form>
    </div>
  );
}
