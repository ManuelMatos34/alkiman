import { Navigate, Outlet, useLocation } from "react-router-dom"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { FullScreenLoader } from "@/presentation/components/FullScreenLoader"

const FORCED_CHANGE_PASSWORD_PATH = "/cambiar-password"

export function ProtectedRoute() {
  const { isAuthenticated, isLoading, user } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  // Si el usuario tiene una contraseña generada/temporal pendiente de cambiar, no puede
  // usar el resto de la app hasta que la cambie -- lo mandamos siempre a esa pantalla.
  if (user?.mustChangePassword && location.pathname !== FORCED_CHANGE_PASSWORD_PATH) {
    return <Navigate to={FORCED_CHANGE_PASSWORD_PATH} replace />
  }

  return <Outlet />
}
