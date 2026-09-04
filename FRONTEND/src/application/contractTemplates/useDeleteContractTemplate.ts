import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { contractTemplatesQueryKey } from "@/application/contractTemplates/useContractTemplates"

export function useDeleteContractTemplate() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      await api.delete(`/api/contract-templates/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractTemplatesQueryKey })
    },
  })
}
