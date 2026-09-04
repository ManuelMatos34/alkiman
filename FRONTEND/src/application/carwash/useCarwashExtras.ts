import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashExtraItem } from "@/domain/types/carwash"

export const carwashExtrasQueryKey = ["carwash-extras"] as const

/** Catálogo de agregados (encerado, ozono, ...) del negocio autenticado. */
export function useCarwashExtras() {
  const api = useApiClient()

  return useQuery({
    queryKey: carwashExtrasQueryKey,
    queryFn: async () => {
      const { data } = await api.get<CarwashExtraItem[]>("/api/carwash/extras")
      return data
    },
    retry: false,
  })
}
