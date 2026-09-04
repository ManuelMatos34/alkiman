import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { CreateCancellationRequestPayload } from "@/domain/types/myRental"

/** Pedido público de cancelación anticipada. Ver nota de `useVerifyMyRental` sobre por qué no usa `useApiClient`. */
export function useCreateCancellationRequest(token: string | undefined) {
  return useMutation({
    mutationFn: async (payload: CreateCancellationRequestPayload) => {
      await apiClient.post<void>(`/api/my-rental/${token}/cancellation-requests`, payload)
    },
  })
}
