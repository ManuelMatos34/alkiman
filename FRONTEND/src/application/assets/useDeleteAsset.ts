import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { assetsQueryKey } from "@/application/assets/useAssets"

export function useDeleteAsset() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/assets/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetsQueryKey })
    },
  })
}
