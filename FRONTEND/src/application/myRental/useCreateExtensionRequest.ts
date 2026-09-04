import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { CreateExtensionRequestPayload } from "@/domain/types/myRental"

/** Pedido público de prórroga. Ver nota de `useVerifyMyRental` sobre por qué no usa `useApiClient`. */
export function useCreateExtensionRequest(token: string | undefined) {
  return useMutation({
    mutationFn: async (payload: CreateExtensionRequestPayload) => {
      await apiClient.post<void>(`/api/my-rental/${token}/extension-requests`, payload)
    },
  })
}
