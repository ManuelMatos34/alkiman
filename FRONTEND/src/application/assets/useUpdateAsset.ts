import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { assetsQueryKey } from "@/application/assets/useAssets"
import type { Asset, UpdateAssetRequest } from "@/domain/types/asset"

export function useUpdateAsset() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateAssetRequest }) => {
      const { data } = await api.put<Asset>(`/api/assets/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetsQueryKey })
    },
  })
}
