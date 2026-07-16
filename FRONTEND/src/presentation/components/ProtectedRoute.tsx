import { useAuth0 } from "@auth0/auth0-react"
import { Navigate, Outlet } from "react-router-dom"
import { FullScreenLoader } from "@/presentation/components/FullScreenLoader"

export function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth0()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
