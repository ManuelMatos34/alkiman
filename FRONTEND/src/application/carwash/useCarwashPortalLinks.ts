import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashPortalLink } from "@/domain/types/carwash"

export const carwashPortalLinksQueryKey = ["carwash-portal-links"] as const

/** Listado de links del portal de Carwash del negocio autenticado. */
export function useCarwashPortalLinks() {
  const api = useApiClient()

  return useQuery({
    queryKey: carwashPortalLinksQueryKey,
    queryFn: async () => {
      const { data } = await api.get<CarwashPortalLink[]>("/api/carwash/portal-links")
      return data
    },
    retry: false,
  })
}
