export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string | null;
  preferredName: string | null;
  gender: string | null;
  phoneNumber: string | null;
  friendCode: string;
  displayName: string;
}

export interface UpdateUserProfilePayload {
  email?: string;
  phoneNumber?: string | null;
  preferredName?: string | null;
  gender?: string | null;
}

export interface LoginResponse {
  user: UserProfile;
}

export interface RegisterPayload {
  email: string;
  password: string;
  preferredName: string;
  gender?: string | null;
  phoneNumber?: string | null;
}

export interface RegisterResponse {
  message: string;
}

export interface VerifyEmailResponse {
  verified: boolean;
  message: string;
}

export interface ForgotPasswordResponse {
  message: string;
}

export interface ResetPasswordResponse {
  success: boolean;
  message: string;
}

export const GENDER_OPTIONS = [
  { value: "Male", label: "Male" },
  { value: "Female", label: "Female" },
  { value: "Non-binary", label: "Non-binary" },
  { value: "", label: "Prefer not to say" },
] as const;
