import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { contractTemplatesQueryKey } from "@/application/contractTemplates/useContractTemplates"
import type {
  ContractTemplate,
  CreateContractTemplateRequest,
} from "@/domain/types/contractTemplate"

export function useCreateContractTemplate() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateContractTemplateRequest) => {
      const { data } = await api.post<ContractTemplate>("/api/contract-templates", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractTemplatesQueryKey })
    },
  })
}
