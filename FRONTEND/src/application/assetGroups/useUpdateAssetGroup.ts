import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { assetGroupsQueryKey } from "@/application/assetGroups/useAssetGroups"
import type { AssetGroup, UpdateAssetGroupRequest } from "@/domain/types/assetGroup"

export function useUpdateAssetGroup() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: UpdateAssetGroupRequest }) => {
      const { data } = await api.put<AssetGroup>(`/api/asset-groups/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetGroupsQueryKey })
    },
  })
}
