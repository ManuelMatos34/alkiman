import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { RentalRequest } from "@/domain/types/rentalRequest"

export const rentalRequestsQueryKey = ["rental-requests"] as const

/** Listado de pedidos de prórroga/cancelación que los clientes hicieron desde su link público. */
export function useRentalRequests() {
  const api = useApiClient()

  return useQuery({
    queryKey: rentalRequestsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<RentalRequest[]>("/api/rental-requests")
      return data
    },
  })
}
