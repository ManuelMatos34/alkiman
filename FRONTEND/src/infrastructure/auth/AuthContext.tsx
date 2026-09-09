import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react"
import { apiClient } from "@/infrastructure/http/apiClient"
import type {
  AuthResponse,
  ForgotPasswordRequest,
  LoginOutcome,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  ResendTwoFactorRequest,
  ResetPasswordRequest,
  VerifyTwoFactorRequest,
} from "@/domain/types/auth"

const SESSION_STORAGE_KEY = "alkiman.session"

interface AuthLandlord {
  id: string
  businessName: string
}

interface AuthUser {
  id: string
  fullName: string
  email: string
  role: string
  isOwner: boolean
  permissions: string[]
  mustChangePassword: boolean
}

interface StoredSession {
  token: string
  expiresAtUtc: string
  landlord: AuthLandlord
  user: AuthUser
}

interface AuthContextValue {
  token: string | null
  landlord: AuthLandlord | null
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  hasPermission: (code: string) => boolean
  /**
   * Verifica las credenciales. Si el usuario tiene doble factor, NO deja sesión
   * iniciada: devuelve el token de desafío para que la pantalla pida el código y
   * cierre con `verifyTwoFactor`.
   */
  login: (email: string, password: string) => Promise<LoginOutcome>
  /** Segundo paso del login con doble factor: canjea el código del correo por la sesión. */
  verifyTwoFactor: (challengeToken: string, code: string) => Promise<void>
  /** Pide otro código para el desafío en curso (invalida el anterior). */
  resendTwoFactorCode: (challengeToken: string) => Promise<void>
  register: (
    businessName: string,
    fullName: string,
    email: string,
    password: string,
    phone?: string
  ) => Promise<void>
  logout: () => void
  /**
   * Reemite el token con el rol y los permisos al día. Los permisos son claims del
   * JWT, así que quedan congelados al emitirlo: hay que llamar a esto después de algo
   * que cambie los permisos del usuario en la base (habilitar un módulo), o la API
   * responde 403 hasta el próximo login.
   */
  refreshSession: () => Promise<void>
  /** Actualiza localmente el flag de "debe cambiar la contraseña" sin requerir un nuevo login (ver ForcedChangePasswordPage). */
  updateMustChangePassword: (value: boolean) => void
  /** Pide el link de recuperación de contraseña. Sin sesión: siempre resuelve, exista o no el email. */
  forgotPassword: (email: string) => Promise<void>
  /** Confirma la recuperación de contraseña con el token recibido por email. */
  resetPassword: (token: string, newPassword: string) => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function loadSession(): StoredSession | null {
  const raw = localStorage.getItem(SESSION_STORAGE_KEY)
  if (!raw) return null

  try {
    const parsed = JSON.parse(raw) as StoredSession
    if (new Date(parsed.expiresAtUtc).getTime() <= Date.now()) {
      localStorage.removeItem(SESSION_STORAGE_KEY)
      return null
    }
    return parsed
  } catch {
    localStorage.removeItem(SESSION_STORAGE_KEY)
    return null
  }
}

function toSession(response: AuthResponse): StoredSession {
  return {
    token: response.token,
    expiresAtUtc: response.expiresAtUtc,
    landlord: {
      id: response.landlordId,
      businessName: response.businessName,
    },
    user: {
      id: response.userId,
      fullName: response.fullName,
      email: response.email,
      role: response.role,
      isOwner: response.isOwner,
      permissions: response.permissions,
      mustChangePassword: response.mustChangePassword,
    },
  }
}

/**
 * Sesión propia (JWT emitido por /api/auth). Persiste el token, el negocio
 * (landlord) y el usuario autenticado (con su rol y permisos) en
 * localStorage e hidrata el estado al montar.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<StoredSession | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    setSession(loadSession())
    setIsLoading(false)
  }, [])

  const persistSession = useCallback((next: StoredSession | null) => {
    setSession(next)
    if (next) {
      localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(next))
    } else {
      localStorage.removeItem(SESSION_STORAGE_KEY)
    }
  }, [])

  const login = useCallback(
    async (email: string, password: string): Promise<LoginOutcome> => {
      const request: LoginRequest = { email, password }
      const { data } = await apiClient.post<LoginResponse>("/api/auth/login", request)

      if (data.requiresTwoFactor && data.challengeToken) {
        return { requiresTwoFactor: true, challengeToken: data.challengeToken }
      }

      if (!data.session) {
        // No debería pasar: o hay desafío o hay sesión. Si el backend manda las dos
        // en null, fallar acá es mejor que dejar la pantalla colgada sin explicación.
        throw new Error("El servidor no devolvió una sesión válida.")
      }

      persistSession(toSession(data.session))
      return { requiresTwoFactor: false }
    },
    [persistSession]
  )

  const verifyTwoFactor = useCallback(
    async (challengeToken: string, code: string) => {
      const request: VerifyTwoFactorRequest = { challengeToken, code }
      const { data } = await apiClient.post<AuthResponse>("/api/auth/two-factor/verify", request)
      persistSession(toSession(data))
    },
    [persistSession]
  )

  const resendTwoFactorCode = useCallback(async (challengeToken: string) => {
    const request: ResendTwoFactorRequest = { challengeToken }
    await apiClient.post("/api/auth/two-factor/resend", request)
  }, [])

  const register = useCallback(
    async (businessName: string, fullName: string, email: string, password: string, phone?: string) => {
      const request: RegisterRequest = { businessName, fullName, email, password, ...(phone ? { phone } : {}) }
      const { data } = await apiClient.post<AuthResponse>("/api/auth/register", request)
      persistSession(toSession(data))
    },
    [persistSession]
  )

  const logout = useCallback(() => {
    persistSession(null)
  }, [persistSession])

  // El header Authorization lo pone el interceptor de useApiClient, que vive en el árbol
  // de React por debajo de este provider. Acá se usa el apiClient pelado, así que el token
  // va explícito para no depender de que ese hook esté montado.
  const refreshSession = useCallback(async () => {
    const current = session
    if (!current) return

    const { data } = await apiClient.post<AuthResponse>("/api/auth/refresh", null, {
      headers: { Authorization: `Bearer ${current.token}` },
    })
    persistSession(toSession(data))
  }, [session, persistSession])

  const forgotPassword = useCallback(async (email: string) => {
    const request: ForgotPasswordRequest = { email }
    await apiClient.post("/api/auth/forgot-password", request)
  }, [])

  const resetPassword = useCallback(async (token: string, newPassword: string) => {
    const request: ResetPasswordRequest = { token, newPassword }
    await apiClient.post("/api/auth/reset-password", request)
  }, [])

  const updateMustChangePassword = useCallback(
    (value: boolean) => {
      setSession((current) => {
        if (!current) return current
        const next: StoredSession = {
          ...current,
          user: { ...current.user, mustChangePassword: value },
        }
        localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(next))
        return next
      })
    },
    []
  )

  const hasPermission = useCallback(
    (code: string) => {
      if (!session) return false
      if (session.user.isOwner) return true
      return session.user.permissions.includes(code)
    },
    [session]
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      token: session?.token ?? null,
      landlord: session?.landlord ?? null,
      user: session?.user ?? null,
      isAuthenticated: !!session,
      isLoading,
      hasPermission,
      login,
      verifyTwoFactor,
      resendTwoFactorCode,
      register,
      logout,
      refreshSession,
      updateMustChangePassword,
      forgotPassword,
      resetPassword,
    }),
    [
      session,
      isLoading,
      hasPermission,
      login,
      verifyTwoFactor,
      resendTwoFactorCode,
      register,
      logout,
      refreshSession,
      updateMustChangePassword,
      forgotPassword,
      resetPassword,
    ]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error("useAuth debe usarse dentro de un AuthProvider")
  }
  return context
}
