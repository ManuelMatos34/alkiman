import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { PortalCatalog } from "@/domain/types/portalPublic"

/**
 * Catálogo público de un link del Portal de Rentas. Usa el cliente HTTP base
 * SIN el interceptor de auth (`useApiClient`): esta pantalla se renderiza
 * fuera de ProtectedRoute, no hay JWT ni sesión, y un 401 nunca debería
 * redirigir a /login acá (esto es público).
 */
export function usePortalCatalog(slug: string | undefined) {
  return useQuery({
    queryKey: ["portal-catalog", slug],
    queryFn: async () => {
      const { data } = await apiClient.get<PortalCatalog>(`/api/portal/${slug}`)
      return data
    },
    enabled: !!slug,
    retry: false,
  })
}
