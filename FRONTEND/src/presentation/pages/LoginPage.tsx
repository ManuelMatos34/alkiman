import { useAuth0 } from "@auth0/auth0-react"
import { Navigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { FullScreenLoader } from "@/presentation/components/FullScreenLoader"

export function LoginPage() {
  const { loginWithRedirect, isAuthenticated, isLoading, error } = useAuth0()

  if (error) {
    console.error("[Auth0] Error de autenticación:", error)
  }

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-sm space-y-8 text-center">
        <div className="space-y-3">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-lg font-semibold text-primary-foreground">
            A
          </div>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Alkiman</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Gestión de alquileres, activos y clientes en un solo lugar.
            </p>
          </div>
        </div>
        <Button
          className="w-full"
          size="lg"
          onClick={() => loginWithRedirect()}
        >
          Iniciar sesión
        </Button>
        {error && (
          <p className="text-sm text-destructive">
            {error.message}
          </p>
        )}
      </div>
    </div>
  )
}
