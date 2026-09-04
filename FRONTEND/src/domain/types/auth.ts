export interface AuthResponse {
  token: string
  expiresAtUtc: string
  userId: string
  fullName: string
  landlordId: string
  businessName: string
  email: string
  role: string
  isOwner: boolean
  permissions: string[]
  mustChangePassword: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  businessName: string
  fullName: string
  email: string
  password: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}
