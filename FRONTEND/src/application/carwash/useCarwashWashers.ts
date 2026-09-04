import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashWasher } from "@/domain/types/carwash"

export const carwashWashersQueryKey = ["carwash-washers"] as const

/**
 * Directorio de lavadores del negocio (activos e inactivos).
 *
 * Son entidades del módulo, no usuarios del sistema: ver `CarwashWasher`. Quien
 * necesite sólo los asignables debe filtrar por `isActive`.
 */
export function useCarwashWashers(enabled = true) {
  const api = useApiClient()

  return useQuery({
    queryKey: carwashWashersQueryKey,
    queryFn: async () => {
      const { data } = await api.get<CarwashWasher[]>("/api/carwash/washers")
      return data
    },
    enabled,
    retry: false,
  })
}
