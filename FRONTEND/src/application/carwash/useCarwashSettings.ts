import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashSettings } from "@/domain/types/carwash"

export const carwashSettingsQueryKey = ["carwash-settings"] as const

/**
 * Configuración del módulo. Un `operationMode` en `null` significa que el negocio todavía no
 * eligió modo, y es lo que dispara el diálogo de configuración inicial.
 */
export function useCarwashSettings() {
  const api = useApiClient()

  return useQuery({
    queryKey: carwashSettingsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<CarwashSettings>("/api/carwash/settings")
      return data
    },
    retry: false,
  })
}
