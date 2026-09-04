import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { currentLandlordQueryKey } from "@/application/landlords/useCurrentLandlord"
import type { Landlord, UpdateLandlordProfileRequest } from "@/domain/types/landlord"

export function useUpdateLandlordProfile() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: UpdateLandlordProfileRequest) => {
      const { data } = await api.put<Landlord>("/api/landlords/me", request)
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(currentLandlordQueryKey, data)
    },
  })
}
