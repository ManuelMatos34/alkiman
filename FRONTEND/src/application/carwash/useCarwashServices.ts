import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashServiceItem } from "@/domain/types/carwash"

export const carwashServicesQueryKey = ["carwash-services"] as const

/** Catálogo de servicios de Carwash del negocio autenticado. */
export function useCarwashServices() {
  const api = useApiClient()

  return useQuery({
    queryKey: carwashServicesQueryKey,
    queryFn: async () => {
      const { data } = await api.get<CarwashServiceItem[]>("/api/carwash/services")
      return data
    },
    retry: false,
  })
}
