import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Landlord } from "@/domain/types/landlord"

export const currentLandlordQueryKey = ["landlord", "me"] as const

/** Perfil de negocio del usuario autenticado, obtenido desde el servidor. */
export function useCurrentLandlord(options?: { enabled?: boolean }) {
  const api = useApiClient()

  return useQuery({
    queryKey: currentLandlordQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Landlord>("/api/landlords/me")
      return data
    },
    enabled: options?.enabled ?? true,
  })
}
