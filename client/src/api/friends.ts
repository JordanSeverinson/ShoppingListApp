import { apiRequest } from "../lib/apiClient";
import type {
  FriendLookupMethod,
  FriendsResponse,
  SendFriendRequestResponse,
} from "../types/friend";

export function fetchFriends(): Promise<FriendsResponse> {
  return apiRequest<FriendsResponse>("/api/friends");
}

export function sendFriendRequest(
  method: FriendLookupMethod,
  value: string,
): Promise<SendFriendRequestResponse> {
  const body =
    method === "email"
      ? { email: value.trim() }
      : method === "phone"
        ? { phoneNumber: value.trim() }
        : { friendCode: value.trim().toUpperCase() };

  return apiRequest<SendFriendRequestResponse>("/api/friends/requests", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function acceptFriendRequest(requestId: string): Promise<void> {
  return apiRequest<void>(`/api/friends/requests/${requestId}/accept`, { method: "POST" });
}

export function declineFriendRequest(requestId: string): Promise<void> {
  return apiRequest<void>(`/api/friends/requests/${requestId}/decline`, { method: "POST" });
}

export function removeFriend(friendUserId: string): Promise<void> {
  return apiRequest<void>(`/api/friends/${friendUserId}`, { method: "DELETE" });
}
