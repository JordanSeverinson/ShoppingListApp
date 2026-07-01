import { apiRequest } from "../lib/apiClient";
import type { LoginResponse, RegisterPayload, RegisterResponse, UpdateUserProfilePayload, UserProfile, VerifyEmailResponse } from "../types/user";

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export function register(payload: RegisterPayload): Promise<RegisterResponse> {
  return apiRequest<RegisterResponse>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify({
      email: payload.email.trim(),
      password: payload.password,
      preferredName: payload.preferredName.trim(),
      gender: payload.gender || null,
      phoneNumber: payload.phoneNumber?.trim() || null,
    }),
  });
}

export function verifyEmail(token: string): Promise<VerifyEmailResponse> {
  const query = new URLSearchParams({ token });
  return apiRequest<VerifyEmailResponse>(`/api/auth/verify-email?${query.toString()}`);
}

export function fetchCurrentUser(): Promise<UserProfile> {
  return apiRequest<UserProfile>("/api/users/me");
}

export function updateCurrentUser(payload: UpdateUserProfilePayload): Promise<UserProfile> {
  return apiRequest<UserProfile>("/api/users/me", {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}
