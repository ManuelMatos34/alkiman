import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Landlord } from "@/domain/types/landlord"

export const currentLandlordQueryKey = ["landlord", "me"] as const

/**
 * Perfil de negocio del usuario autenticado.
 * Un 404 es un estado esperado (usuario logueado en Auth0 pero
 * todavía no completó el registro de su negocio) por eso no reintenta.
 */
export function useCurrentLandlord() {
  const api = useApiClient()

  return useQuery({
    queryKey: currentLandlordQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Landlord>("/api/landlords/me")
      return data
    },
    retry: false,
  })
}
