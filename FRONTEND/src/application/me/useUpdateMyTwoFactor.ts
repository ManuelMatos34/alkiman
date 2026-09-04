import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { meQueryKey } from "@/application/me/useMe"
import type { Me, UpdateMyTwoFactorRequest } from "@/domain/types/me"

export function useUpdateMyTwoFactor() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: UpdateMyTwoFactorRequest) => {
      const { data } = await api.put<Me>("/api/me/two-factor", request)
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(meQueryKey, data)
    },
  })
}
