import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { currentLandlordQueryKey } from "@/application/landlords/useCurrentLandlord"
import type { Landlord } from "@/domain/types/landlord"

export function useUpdateSignature() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (signatureBase64: string | null) => {
      const { data } = await api.put<Landlord>("/api/landlords/me/signature", { signatureBase64 })
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(currentLandlordQueryKey, data)
    },
  })
}
