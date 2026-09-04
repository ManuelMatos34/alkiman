import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Rental } from "@/domain/types/rental"

export const rentalsQueryKey = ["rentals"] as const

/** Listado de rentas (asignaciones activo↔cliente) del negocio autenticado. */
export function useRentals() {
  const api = useApiClient()

  return useQuery({
    queryKey: rentalsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Rental[]>("/api/rentals")
      return data
    },
  })
}
