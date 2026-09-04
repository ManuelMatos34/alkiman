import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { meQueryKey } from "@/application/me/useMe"
import type { Me, UpdateMeRequest } from "@/domain/types/me"

export function useUpdateMe() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: UpdateMeRequest) => {
      const { data } = await api.put<Me>("/api/me", request)
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(meQueryKey, data)
    },
  })
}
