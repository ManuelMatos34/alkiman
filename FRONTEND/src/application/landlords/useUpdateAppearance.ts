import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { currentLandlordQueryKey } from "@/application/landlords/useCurrentLandlord"
import type { Landlord, UpdateAppearanceRequest } from "@/domain/types/landlord"

export function useUpdateAppearance() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: UpdateAppearanceRequest) => {
      const { data } = await api.put<Landlord>("/api/landlords/me/appearance", request)
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(currentLandlordQueryKey, data)
    },
  })
}
