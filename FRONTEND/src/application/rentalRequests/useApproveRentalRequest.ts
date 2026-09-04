import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { rentalRequestsQueryKey } from "@/application/rentalRequests/useRentalRequests"
import { rentalsQueryKey } from "@/application/rentals/useRentals"
import type { ReviewRentalRequestPayload, RentalRequest } from "@/domain/types/rentalRequest"

export function useApproveRentalRequest() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      id,
      payload,
    }: {
      id: string
      payload: ReviewRentalRequestPayload
    }) => {
      const { data } = await api.post<RentalRequest>(
        `/api/rental-requests/${id}/approve`,
        payload
      )
      return data
    },
    onSuccess: () => {
      // Aprobar un pedido también mutó la renta subyacente (extendió fecha o la canceló).
      queryClient.invalidateQueries({ queryKey: rentalRequestsQueryKey })
      queryClient.invalidateQueries({ queryKey: rentalsQueryKey })
    },
  })
}
