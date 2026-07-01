export interface RegisterPayload {
  email: string;
  password: string;
  preferredName: string;
  gender?: string | null;
  phoneNumber?: string | null;
}

const PREFERRED_NAME_PATTERN = /^[a-zA-Z0-9 ]+$/;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validatePreferredName(preferredName: string): string | null {
  const trimmed = preferredName.trim();
  if (trimmed.length < 2) {
    return "Preferred name must be at least 2 characters.";
  }

  if (!PREFERRED_NAME_PATTERN.test(trimmed)) {
    return "Preferred name cannot contain special characters.";
  }

  return null;
}

export function validateEmail(email: string): string | null {
  const trimmed = email.trim();
  if (!trimmed) {
    return "Email is required.";
  }

  if (!EMAIL_PATTERN.test(trimmed)) {
    return "Enter a valid email address.";
  }

  return null;
}

export function validatePhone(phone: string): string | null {
  const digits = phone.replace(/\D/g, "");
  if (digits.length === 0) {
    return null;
  }

  if (digits.length !== 10) {
    return "Phone number must be a 10-digit number.";
  }

  return null;
}

export function formatPhoneInput(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 10);
  if (digits.length <= 3) {
    return digits;
  }

  if (digits.length <= 6) {
    return `${digits.slice(0, 3)}-${digits.slice(3)}`;
  }

  return `${digits.slice(0, 3)}-${digits.slice(3, 6)}-${digits.slice(6)}`;
}
