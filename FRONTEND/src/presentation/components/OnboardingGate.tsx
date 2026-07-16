import { isAxiosError } from "axios"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"
import { FullScreenLoader } from "@/presentation/components/FullScreenLoader"
import { OnboardingForm } from "@/presentation/components/OnboardingForm"

/**
 * Se ubica dentro del layout autenticado. Resuelve el perfil de negocio
 * del usuario logueado en Auth0:
 * - 404 -> todavía no completó el registro -> muestra el formulario de alta.
 * - otro error -> muestra un estado de error genérico.
 * - ok -> renderiza el resto del dashboard (children).
 */
export function OnboardingGate({ children }: { children: React.ReactNode }) {
  const { isLoading, isError, error } = useCurrentLandlord()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (isError) {
    const status = isAxiosError(error) ? error.response?.status : undefined

    if (status === 404) {
      return <OnboardingForm />
    }

    return (
      <div className="flex h-full min-h-[60vh] items-center justify-center">
        <p className="text-sm text-muted-foreground">
          No pudimos cargar tu perfil de negocio. Intentá recargar la página.
        </p>
      </div>
    )
  }

  return <>{children}</>
}
