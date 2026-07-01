import { ArrowLeft, Mail, Phone, UserPlus, Users } from "lucide-react";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import * as friendsApi from "../api/friends";
import { ConfirmModal } from "../components/ConfirmModal";
import { ShareCodeCopy } from "../components/ShareCodeCopy";
import { APP_NAME } from "../lib/appName";
import type { FriendLookupMethod, FriendSummary, FriendsResponse } from "../types/friend";

const LOOKUP_OPTIONS: {
  method: FriendLookupMethod;
  label: string;
  placeholder: string;
  icon: typeof Mail;
}[] = [
  {
    method: "email",
    label: "Email",
    placeholder: "friend@example.com",
    icon: Mail,
  },
  {
    method: "phone",
    label: "Phone",
    placeholder: "+1 555 123 4567",
    icon: Phone,
  },
  {
    method: "friendCode",
    label: "Friend code",
    placeholder: "8-character code",
    icon: Users,
  },
];

function displayName(friend: { firstName: string; lastName: string | null }) {
  return friend.lastName ? `${friend.firstName} ${friend.lastName}` : friend.firstName;
}

export function FriendsPage() {
  const { user, isAuthenticated, loading: authLoading } = useAuth();
  const [friendsData, setFriendsData] = useState<FriendsResponse | null>(null);
  const [lookupMethod, setLookupMethod] = useState<FriendLookupMethod>("email");
  const [lookupValue, setLookupValue] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [friendToRemove, setFriendToRemove] = useState<FriendSummary | null>(null);
  const [removing, setRemoving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const friends = await friendsApi.fetchFriends();
      setFriendsData(friends);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load friends");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!isAuthenticated || authLoading) {
      return;
    }

    void load();
  }, [load, isAuthenticated, authLoading]);

  const selectedLookup = LOOKUP_OPTIONS.find((option) => option.method === lookupMethod)!;

  async function handleAddFriend(event: FormEvent) {
    event.preventDefault();
    if (!lookupValue.trim()) {
      return;
    }

    setBusy(true);
    setError(null);
    setMessage(null);

    try {
      const result = await friendsApi.sendFriendRequest(lookupMethod, lookupValue);
      setMessage(result.message);
      setLookupValue("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not send friend request");
    } finally {
      setBusy(false);
    }
  }

  async function handleAccept(requestId: string) {
    setError(null);
    try {
      await friendsApi.acceptFriendRequest(requestId);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not accept request");
    }
  }

  async function handleDecline(requestId: string) {
    setError(null);
    try {
      await friendsApi.declineFriendRequest(requestId);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not decline request");
    }
  }

  async function confirmRemoveFriend() {
    if (!friendToRemove) {
      return;
    }

    setRemoving(true);
    setError(null);
    try {
      await friendsApi.removeFriend(friendToRemove.userId);
      setFriendToRemove(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not remove friend");
    } finally {
      setRemoving(false);
    }
  }

  if (!authLoading && !isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="mx-auto min-h-screen max-w-3xl px-4 py-8 sm:px-6 sm:py-12">
      <Link
        to="/"
        className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-brand-700"
      >
        <ArrowLeft className="h-4 w-4" aria-hidden />
        Home
      </Link>

      <header className="mb-10">
        <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-xs font-medium text-brand-700">
          <Users className="h-3.5 w-3.5" aria-hidden />
          {APP_NAME}
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-ink sm:text-4xl">Friends</h1>
        <p className="mt-2 max-w-xl text-muted">
          Connect with people you cook and shop with.
        </p>
      </header>

      {user && (
        <section className="mb-8 rounded-2xl border border-border bg-white p-4 shadow-sm">
          <ShareCodeCopy shareCode={user.friendCode} label="Your friend code" />
          <p className="mt-3 text-sm text-muted">
            Share this code so others can add you as a friend.
          </p>
        </section>
      )}

      <section className="mb-10 rounded-2xl border border-border bg-white p-4 shadow-sm">
        <h2 className="mb-4 flex items-center gap-2 font-semibold text-ink">
          <UserPlus className="h-5 w-5 text-brand-600" aria-hidden />
          Add a friend
        </h2>

        <div className="mb-4 flex flex-wrap gap-2">
          {LOOKUP_OPTIONS.map((option) => {
            const Icon = option.icon;
            const selected = lookupMethod === option.method;
            return (
              <button
                key={option.method}
                type="button"
                onClick={() => {
                  setLookupMethod(option.method);
                  setLookupValue("");
                }}
                className={`inline-flex items-center gap-2 rounded-xl border px-3 py-2 text-sm font-medium transition ${
                  selected
                    ? "border-brand-500 bg-brand-50 text-brand-800"
                    : "border-border bg-white text-muted hover:border-brand-300 hover:text-ink"
                }`}
              >
                <Icon className="h-4 w-4" aria-hidden />
                {option.label}
              </button>
            );
          })}
        </div>

        <form onSubmit={(event) => void handleAddFriend(event)} className="flex flex-col gap-3 sm:flex-row">
          <input
            value={lookupValue}
            onChange={(event) =>
              setLookupValue(
                lookupMethod === "friendCode"
                  ? event.target.value.toUpperCase()
                  : event.target.value,
              )
            }
            placeholder={selectedLookup.placeholder}
            maxLength={lookupMethod === "friendCode" ? 8 : undefined}
            className="min-w-0 flex-1 rounded-xl border border-border px-4 py-2.5 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30"
          />
          <button
            type="submit"
            disabled={busy || !lookupValue.trim()}
            className="rounded-xl bg-brand-600 px-6 py-2.5 font-medium text-white hover:bg-brand-700 disabled:opacity-50"
          >
            {busy ? "Sending…" : "Add friend"}
          </button>
        </form>
      </section>

      {error && (
        <p className="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {message && (
        <p className="mb-6 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
          {message}
        </p>
      )}

      {loading ? (
        <p className="text-center text-muted">Loading friends…</p>
      ) : (
        <>
          {friendsData && friendsData.incomingRequests.length > 0 && (
            <section className="mb-10">
              <h2 className="mb-4 text-lg font-semibold text-ink">Friend requests</h2>
              <div className="space-y-3">
                {friendsData.incomingRequests.map((request) => (
                  <article
                    key={request.id}
                    className="flex flex-col gap-3 rounded-2xl border border-border bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div>
                      <p className="font-semibold text-ink">{displayName(request)}</p>
                      <p className="text-sm text-muted">{request.email}</p>
                    </div>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => void handleAccept(request.id)}
                        className="rounded-xl bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700"
                      >
                        Accept
                      </button>
                      <button
                        type="button"
                        onClick={() => void handleDecline(request.id)}
                        className="rounded-xl border border-border px-4 py-2 text-sm font-medium text-muted hover:bg-stone-50"
                      >
                        Decline
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            </section>
          )}

          <section className="mb-10">
            <h2 className="mb-4 text-lg font-semibold text-ink">Your friends</h2>
            {!friendsData || friendsData.friends.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-border px-6 py-10 text-center text-muted">
                No friends yet. Add someone by email, phone, or friend code.
              </p>
            ) : (
              <div className="space-y-3">
                {friendsData.friends.map((friend) => (
                  <article
                    key={friend.userId}
                    className="flex flex-col gap-3 rounded-2xl border border-border bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div>
                      <p className="font-semibold text-ink">{displayName(friend)}</p>
                      <p className="text-sm text-muted">{friend.email}</p>
                    </div>
                    <button
                      type="button"
                      onClick={() => setFriendToRemove(friend)}
                      className="self-start rounded-xl border border-border px-4 py-2 text-sm font-medium text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-700 sm:self-center"
                    >
                      Remove
                    </button>
                  </article>
                ))}
              </div>
            )}
          </section>

          {friendsData && friendsData.outgoingRequests.length > 0 && (
            <section>
              <h2 className="mb-4 text-lg font-semibold text-ink">Pending requests</h2>
              <div className="space-y-3">
                {friendsData.outgoingRequests.map((request) => (
                  <article
                    key={request.id}
                    className="rounded-2xl border border-dashed border-border bg-white/70 p-4"
                  >
                    <p className="font-medium text-ink">{displayName(request)}</p>
                    <p className="text-sm text-muted">Waiting for them to accept your request.</p>
                  </article>
                ))}
              </div>
            </section>
          )}
        </>
      )}

      <ConfirmModal
        open={friendToRemove !== null}
        message={
          friendToRemove
            ? `Remove ${displayName(friendToRemove)} from your friends?`
            : ""
        }
        confirmLabel="Remove"
        cancelLabel="Cancel"
        busy={removing}
        onConfirm={() => void confirmRemoveFriend()}
        onCancel={() => {
          if (!removing) {
            setFriendToRemove(null);
          }
        }}
      />
    </div>
  );
}
