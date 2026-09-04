import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { rentalsQueryKey } from "@/application/rentals/useRentals"
import { assetsQueryKey } from "@/application/assets/useAssets"
import type { Rental, UpdateRentalStatusRequest } from "@/domain/types/rental"

export function useUpdateRentalStatus() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      id,
      request,
    }: {
      id: string
      request: UpdateRentalStatusRequest
    }) => {
      const { data } = await api.patch<Rental>(
        `/api/rentals/${id}/status`,
        request
      )
      return data
    },
    onSuccess: () => {
      // Completar una renta también libera el activo del lado del servidor.
      queryClient.invalidateQueries({ queryKey: rentalsQueryKey })
      queryClient.invalidateQueries({ queryKey: assetsQueryKey })
    },
  })
}
