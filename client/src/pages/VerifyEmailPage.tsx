import { CheckCircle2, ShoppingCart, XCircle } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import * as usersApi from "../api/users";
import { APP_NAME } from "../lib/appName";

import { readAuthTokenFromUrl, stripAuthTokenFromHistory } from "../lib/authTokenFromUrl";

export function VerifyEmailPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    const token = readAuthTokenFromUrl();
    if (!token) {
      setStatus("error");
      setMessage("This verification link is invalid or has expired.");
      return;
    }

    stripAuthTokenFromHistory();

    async function verify() {
      try {
        const response = await usersApi.verifyEmail(token!);
        if (response.verified) {
          setStatus("success");
          setMessage(response.message);
          window.setTimeout(() => navigate("/login?verified=1"), 2500);
        } else {
          setStatus("error");
          setMessage(response.message);
        }
      } catch {
        setStatus("error");
        setMessage("This verification link is invalid or has expired.");
      }
    }

    void verify();
  }, [navigate]);

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-8">
      <div className="mb-8 text-center">
        <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <ShoppingCart className="h-3.5 w-3.5" aria-hidden />
          {APP_NAME}
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink">Email verification</h1>
      </div>

      <div className="rounded-2xl border border-border bg-white p-6 text-center shadow-sm">
        {status === "loading" && <p className="text-muted">Verifying your email…</p>}

        {status === "success" && (
          <>
            <CheckCircle2 className="mx-auto mb-4 h-12 w-12 text-brand-600" aria-hidden />
            <p className="text-ink">{message}</p>
            <p className="mt-2 text-sm text-muted">Redirecting you to sign in…</p>
          </>
        )}

        {status === "error" && (
          <>
            <XCircle className="mx-auto mb-4 h-12 w-12 text-red-600" aria-hidden />
            <p className="text-ink">{message}</p>
            <Link
              to="/login"
              className="mt-6 inline-block font-medium text-brand-700 hover:text-brand-600"
            >
              Back to sign in
            </Link>
          </>
        )}
      </div>
    </div>
  );
}
