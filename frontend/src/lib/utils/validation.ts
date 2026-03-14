export interface PasswordValidation {
  isValid: boolean;
  score: number; // 0-5
  checks: {
    minLength: boolean;
    maxLength: boolean;
    hasUppercase: boolean;
    hasLowercase: boolean;
    hasNumber: boolean;
    hasSpecial: boolean;
  };
}

export function validatePassword(password: string): PasswordValidation {
  const checks = {
    minLength: password.length >= 12,
    maxLength: password.length <= 128,
    hasUppercase: /[A-Z]/.test(password),
    hasLowercase: /[a-z]/.test(password),
    hasNumber: /[0-9]/.test(password),
    hasSpecial: /[^A-Za-z0-9]/.test(password),
  };

  const score = Object.values(checks).filter(Boolean).length;
  const isValid = Object.values(checks).every(Boolean);

  return { isValid, score, checks };
}

export function validateEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}
