import { Outlet } from "react-router-dom"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { ForbiddenPage } from "@/presentation/pages/ForbiddenPage"

interface PermissionRouteProps {
  /** Código de permiso requerido para acceder a las rutas anidadas. */
  permission: string
}

/** Gatea rutas anidadas por permiso; asume que ya se pasó por ProtectedRoute. */
export function PermissionRoute({ permission }: PermissionRouteProps) {
  const { hasPermission } = useAuth()

  if (!hasPermission(permission)) {
    return <ForbiddenPage />
  }

  return <Outlet />
}
