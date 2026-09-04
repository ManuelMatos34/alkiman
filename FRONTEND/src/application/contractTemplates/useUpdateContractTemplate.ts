import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { contractTemplatesQueryKey } from "@/application/contractTemplates/useContractTemplates"
import type {
  ContractTemplate,
  UpdateContractTemplateRequest,
} from "@/domain/types/contractTemplate"

export function useUpdateContractTemplate() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: UpdateContractTemplateRequest }) => {
      const { data } = await api.put<ContractTemplate>(`/api/contract-templates/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractTemplatesQueryKey })
    },
  })
}
