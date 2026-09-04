import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Country } from "@/domain/types/location"

export const countriesQueryKey = ["locations", "countries"] as const

/** Catálogo completo de países. */
export function useCountries() {
  const api = useApiClient()

  return useQuery({
    queryKey: countriesQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Country[]>("/api/locations/countries")
      return data
    },
  })
}
