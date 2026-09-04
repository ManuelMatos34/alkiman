import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { assetsQueryKey } from "@/application/assets/useAssets"
import type { Asset, AssetStatus } from "@/domain/types/asset"

export function useUpdateAssetStatus() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: AssetStatus }) => {
      const { data } = await api.patch<Asset>(`/api/assets/${id}/status`, { status })
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetsQueryKey })
    },
  })
}
