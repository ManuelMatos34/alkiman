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

/**
 * Respuesta de POST /api/auth/login. La contraseña correcta no siempre alcanza:
 * si el usuario tiene doble factor activo llega `requiresTwoFactor: true` con un
 * `challengeToken` y `session` en null, y el JWT recién sale al verificar el código.
 */
export interface LoginResponse {
  requiresTwoFactor: boolean
  challengeToken: string | null
  session: AuthResponse | null
}

export interface VerifyTwoFactorRequest {
  challengeToken: string
  code: string
}

export interface ResendTwoFactorRequest {
  challengeToken: string
}

/**
 * Lo que `login()` le informa a la pantalla: o quedó la sesión iniciada, o falta el
 * código. Unión discriminada para que el `challengeToken` sólo exista (y sólo compile)
 * en la rama donde de verdad hay uno.
 */
export type LoginOutcome =
  | { requiresTwoFactor: false }
  | { requiresTwoFactor: true; challengeToken: string }

export interface RegisterRequest {
  businessName: string
  fullName: string
  email: string
  password: string
  phone?: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}
