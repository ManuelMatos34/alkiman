import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { City } from "@/domain/types/location"

export const citiesQueryKey = (stateId: number | null | undefined) =>
  ["locations", "cities", stateId] as const

/** Ciudades principales de una provincia/estado. Se deshabilita hasta que se elija una provincia. */
export function useCities(stateId: number | null | undefined) {
  const api = useApiClient()

  return useQuery({
    queryKey: citiesQueryKey(stateId),
    queryFn: async () => {
      const { data } = await api.get<City[]>(`/api/locations/states/${stateId}/cities`)
      return data
    },
    enabled: stateId != null,
  })
}
