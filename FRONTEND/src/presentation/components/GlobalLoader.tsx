import { useEffect, useState } from "react"
import { useIsFetching, useIsMutating } from "@tanstack/react-query"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"

/** Espera antes de mostrar el overlay, para no parpadear en cargas casi instantáneas. */
const SHOW_DELAY_MS = 200

/**
 * Loader global: pantalla completa en blanco con el "logo" (avatar de la
 * inicial del negocio) animado con un latido, mientras haya cualquier
 * consulta o mutación de React Query en curso en toda la app.
 *
 * Se monta una única vez en la raíz (`main.tsx`), por fuera de las rutas,
 * para cubrir *todo* tipo de carga sin tener que instrumentar cada página.
 */
export function GlobalLoader() {
  const { isAuthenticated } = useAuth()
  const fetchingCount = useIsFetching()
  const mutatingCount = useIsMutating()
  const isBusy = fetchingCount > 0 || mutatingCount > 0

  // Solo pedimos el perfil del negocio si hay sesión; en pantallas públicas
  // (login, portal, mi-renta) evitamos disparar un 401 innecesario.
  const { data: landlord } = useCurrentLandlord({ enabled: isAuthenticated })

  const [visible, setVisible] = useState(false)

  useEffect(() => {
    if (!isBusy) {
      setVisible(false)
      return
    }

    const timer = setTimeout(() => setVisible(true), SHOW_DELAY_MS)
    return () => clearTimeout(timer)
  }, [isBusy])

  if (!visible) return null

  const appName = (isAuthenticated ? landlord?.appName : undefined) ?? "Alkiman"

  return (
    <div
      role="status"
      aria-live="polite"
      aria-label="Cargando"
      className="fixed inset-0 z-[100] flex items-center justify-center bg-background"
    >
      <div className="flex h-20 w-20 animate-heartbeat items-center justify-center rounded-2xl bg-primary text-3xl font-semibold text-primary-foreground shadow-lg">
        {appName.charAt(0).toUpperCase()}
      </div>
    </div>
  )
}
