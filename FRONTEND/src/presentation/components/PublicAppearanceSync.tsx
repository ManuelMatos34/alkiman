import { useEffect } from "react"
import { useTheme } from "next-themes"
import type { AccentColor, ThemeMode } from "@/domain/types/landlord"

interface PublicAppearanceSyncProps {
  themeMode: ThemeMode
  accentColor: AccentColor
}

/**
 * Igual que `AppearanceSync`, pero para pantallas públicas (Portal de Rentas, `/mi-renta/{token}`)
 * donde no hay sesión: la apariencia llega como parte de la respuesta del propio endpoint público
 * (ya resuelta desde el negocio dueño del link/renta), no desde `/api/landlords/me`. Así cada
 * cliente ve el link con LA marca de su negocio, no la misma apariencia genérica para todos.
 *
 * Al desmontarse restaura los valores por defecto, para no "ensuciar" con la marca de un tercero
 * otras pantallas públicas que se visiten después (ej: /login).
 */
export function PublicAppearanceSync({ themeMode, accentColor }: PublicAppearanceSyncProps) {
  const { setTheme } = useTheme()

  useEffect(() => {
    setTheme(themeMode)
    document.documentElement.setAttribute("data-accent", accentColor)

    return () => {
      setTheme("light")
      document.documentElement.removeAttribute("data-accent")
    }
  }, [themeMode, accentColor, setTheme])

  return null
}
