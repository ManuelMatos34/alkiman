import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { ContractTemplate } from "@/domain/types/contractTemplate"

export const contractTemplatesQueryKey = ["contract-templates"] as const

/** Listado de plantillas de contrato del negocio autenticado (todas las categorías). */
export function useContractTemplates() {
  const api = useApiClient()

  return useQuery({
    queryKey: contractTemplatesQueryKey,
    queryFn: async () => {
      const { data } = await api.get<ContractTemplate[]>("/api/contract-templates")
      return data
    },
  })
}
