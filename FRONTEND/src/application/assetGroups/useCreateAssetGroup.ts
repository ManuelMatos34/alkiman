import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { assetGroupsQueryKey } from "@/application/assetGroups/useAssetGroups"
import type { AssetGroup, CreateAssetGroupRequest } from "@/domain/types/assetGroup"

export function useCreateAssetGroup() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateAssetGroupRequest) => {
      const { data } = await api.post<AssetGroup>("/api/asset-groups", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetGroupsQueryKey })
    },
  })
}
