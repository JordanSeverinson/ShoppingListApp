export type FriendshipStatus = "Pending" | "Accepted" | "Declined";

export interface FriendSummary {
  userId: string;
  firstName: string;
  lastName: string | null;
  email: string;
  friendCode: string;
}

export interface FriendRequest {
  id: string;
  userId: string;
  firstName: string;
  lastName: string | null;
  email: string;
  friendCode: string;
  status: FriendshipStatus;
  isIncoming: boolean;
}

export interface FriendsResponse {
  friends: FriendSummary[];
  incomingRequests: FriendRequest[];
  outgoingRequests: FriendRequest[];
}

export interface SendFriendRequestResponse {
  requestId: string;
  status: FriendshipStatus;
  message: string;
  friend: FriendSummary | null;
}

export type FriendLookupMethod = "email" | "phone" | "friendCode";
