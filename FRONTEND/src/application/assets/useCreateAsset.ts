import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { assetsQueryKey } from "@/application/assets/useAssets"
import type { Asset, CreateAssetRequest } from "@/domain/types/asset"

export function useCreateAsset() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateAssetRequest) => {
      const { data } = await api.post<Asset>("/api/assets", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetsQueryKey })
    },
  })
}
