import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { assetGroupsQueryKey } from "@/application/assetGroups/useAssetGroups"

export function useDeleteAssetGroup() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      await api.delete(`/api/asset-groups/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetGroupsQueryKey })
    },
  })
}
