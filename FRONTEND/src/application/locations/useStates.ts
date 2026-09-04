import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { State } from "@/domain/types/location"

export const statesQueryKey = (countryId: number | null | undefined) =>
  ["locations", "states", countryId] as const

/** Provincias/estados de un país. Se deshabilita hasta que se elija un país. */
export function useStates(countryId: number | null | undefined) {
  const api = useApiClient()

  return useQuery({
    queryKey: statesQueryKey(countryId),
    queryFn: async () => {
      const { data } = await api.get<State[]>(`/api/locations/countries/${countryId}/states`)
      return data
    },
    enabled: countryId != null,
  })
}
