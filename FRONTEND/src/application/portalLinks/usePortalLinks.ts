import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { PortalLink } from "@/domain/types/portal"

export const portalLinksQueryKey = ["portal-links"] as const

/** Listado de links del Portal de Rentas del negocio autenticado. */
export function usePortalLinks() {
  const api = useApiClient()

  return useQuery({
    queryKey: portalLinksQueryKey,
    queryFn: async () => {
      const { data } = await api.get<PortalLink[]>("/api/portal-links")
      return data
    },
  })
}
