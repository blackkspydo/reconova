// User & Auth State
export interface User {
  id: string;
  email: string;
  tenantId: string;
  role: 'TENANT_OWNER' | 'SUPER_ADMIN';
  twoFactorEnabled: boolean;
  status: 'PENDING_2FA' | 'ACTIVE' | 'LOCKED' | 'PASSWORD_EXPIRED' | 'DEACTIVATED';
  lastLoginAt: string | null;
  passwordChangedAt: string | null;
  createdAt: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface TempTokenState {
  tempToken: string | null;
  purpose: 'password_change' | '2fa_setup' | '2fa_verify' | null;
}

// Request types
export interface RegisterRequest {
  email: string;
  password: string;
  tenantName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  totpCode?: string;
}

export interface TwoFactorVerifyRequest {
  totpCode: string;
}

export interface ChangePasswordRequest {
  currentPassword?: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface GetUsersParams {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
}

// Response types
export interface RegisterResponse {
  userId: string;
  tenantId: string;
  requires2faSetup: true;
  tempToken: string;
}

export interface LoginResponse {
  requiresTwoFactor?: boolean;
  requires2faSetup?: boolean;
  requiresPasswordChange?: boolean;
  tempToken?: string;
}

export interface TwoFactorSetupResponse {
  secret: string;
  qrUri: string;
}

export interface AuthTokenResponse {
  success: true;
}

export interface SuccessResponse {
  success: true;
}

export interface AdminUserDetail {
  id: string;
  email: string;
  role: 'TENANT_OWNER' | 'SUPER_ADMIN';
  status: 'PENDING_2FA' | 'ACTIVE' | 'LOCKED' | 'PASSWORD_EXPIRED' | 'DEACTIVATED';
  twoFactorEnabled: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  lastLoginIp: string | null;
  tenantId: string;
  tenantName: string;
}

export interface PaginatedUsers {
  users: AdminUserDetail[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiError {
  code: string;
  status: number;
  message: string;
  requestId: string;
  timestamp: string;
  details?: Record<string, string>;
}
