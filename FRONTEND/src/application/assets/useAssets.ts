import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Asset } from "@/domain/types/asset"

export const assetsQueryKey = ["assets"] as const

/** Listado de activos (inventario) del negocio autenticado. */
export function useAssets() {
  const api = useApiClient()

  return useQuery({
    queryKey: assetsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Asset[]>("/api/assets")
      return data
    },
  })
}
