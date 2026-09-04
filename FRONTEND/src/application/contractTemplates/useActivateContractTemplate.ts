import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { contractTemplatesQueryKey } from "@/application/contractTemplates/useContractTemplates"
import type { ContractTemplate } from "@/domain/types/contractTemplate"

/** Marca la plantilla como la activa de su categoría (desactiva cualquier otra activa de esa categoría). */
export function useActivateContractTemplate() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      const { data } = await api.post<ContractTemplate>(`/api/contract-templates/${id}/activate`)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractTemplatesQueryKey })
    },
  })
}
