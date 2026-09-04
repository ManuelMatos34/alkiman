import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Contract } from "@/domain/types/contract"

export const contractsQueryKey = ["contracts"] as const

/** Listado de contratos del negocio autenticado (uno por renta, generado automáticamente). */
export function useContracts() {
  const api = useApiClient()

  return useQuery({
    queryKey: contractsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Contract[]>("/api/contracts")
      return data
    },
  })
}
