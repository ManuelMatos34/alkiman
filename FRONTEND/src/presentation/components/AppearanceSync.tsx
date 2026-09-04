import { useEffect } from "react"
import { useTheme } from "next-themes"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"

/**
 * Sincroniza el tema (claro/oscuro) y el color de acento guardados en el
 * perfil del landlord con el documento (clase `dark` + atributo `data-accent`).
 * Se monta una vez dentro del layout autenticado.
 */
export function AppearanceSync() {
  const { data: landlord } = useCurrentLandlord()
  const { setTheme } = useTheme()

  useEffect(() => {
    if (!landlord) return

    setTheme(landlord.themeMode)
    document.documentElement.setAttribute("data-accent", landlord.accentColor)
  }, [landlord, setTheme])

  return null
}
