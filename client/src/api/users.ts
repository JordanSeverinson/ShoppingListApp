import { apiRequest } from "../lib/apiClient";
import type {
  ChangePasswordResponse,
  ForgotPasswordResponse,
  LoginResponse,
  RegisterPayload,
  RegisterResponse,
  ResetPasswordResponse,
  UpdateUserProfilePayload,
  UserProfile,
  VerifyEmailResponse,
} from "../types/user";

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export function logout(): Promise<void> {
  return apiRequest<void>("/api/auth/logout", {
    method: "POST",
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
  return apiRequest<VerifyEmailResponse>("/api/auth/verify-email", {
    method: "POST",
    body: JSON.stringify({ token }),
  });
}

export function requestPasswordReset(email: string): Promise<ForgotPasswordResponse> {
  return apiRequest<ForgotPasswordResponse>("/api/auth/forgot-password", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function resetPassword(token: string, password: string): Promise<ResetPasswordResponse> {
  return apiRequest<ResetPasswordResponse>("/api/auth/reset-password", {
    method: "POST",
    body: JSON.stringify({ token, password }),
  });
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

export function changePassword(
  currentPassword: string,
  newPassword: string,
): Promise<ChangePasswordResponse> {
  return apiRequest<ChangePasswordResponse>("/api/users/me/change-password", {
    method: "POST",
    body: JSON.stringify({ currentPassword, newPassword }),
  });
}
