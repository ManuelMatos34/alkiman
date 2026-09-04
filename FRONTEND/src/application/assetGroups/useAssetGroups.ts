import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { AssetGroup } from "@/domain/types/assetGroup"

export const assetGroupsQueryKey = ["asset-groups"] as const

/** Listado de grupos de activos del negocio autenticado. */
export function useAssetGroups() {
  const api = useApiClient()

  return useQuery({
    queryKey: assetGroupsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<AssetGroup[]>("/api/asset-groups")
      return data
    },
  })
}
