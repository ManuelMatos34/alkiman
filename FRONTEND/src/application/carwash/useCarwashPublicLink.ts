import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { CarwashPublicLink } from "@/domain/types/carwashPublic"

/**
 * Info pública de un link de Carwash. Usa el cliente HTTP base SIN el interceptor de auth
 * (`useApiClient`): esta pantalla se renderiza fuera de ProtectedRoute, no hay JWT ni sesión, y
 * un 401 nunca debería redirigir a /login acá (esto es público).
 */
export function useCarwashPublicLink(slug: string | undefined) {
  return useQuery({
    queryKey: ["carwash-public-link", slug],
    queryFn: async () => {
      const { data } = await apiClient.get<CarwashPublicLink>(`/api/carwash/public/${slug}`)
      return data
    },
    enabled: !!slug,
    retry: false,
  })
}
