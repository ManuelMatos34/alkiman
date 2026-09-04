import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { rentalsQueryKey } from "@/application/rentals/useRentals"
import { assetsQueryKey } from "@/application/assets/useAssets"
import type { Rental, CreateRentalRequest } from "@/domain/types/rental"

export function useCreateRental() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateRentalRequest) => {
      const { data } = await api.post<Rental>("/api/rentals", request)
      return data
    },
    onSuccess: () => {
      // Crear una renta también cambia el activo a "Rentado" del lado del servidor.
      queryClient.invalidateQueries({ queryKey: rentalsQueryKey })
      queryClient.invalidateQueries({ queryKey: assetsQueryKey })
    },
  })
}
