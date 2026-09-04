import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { contractsQueryKey } from "@/application/contracts/useContracts"
import type { Contract, SignContractRequest } from "@/domain/types/contract"

/** Registra la firma manual de un contrato pendiente (re-renderiza el PDF y lo marca como Firmado). */
export function useSignContract() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: SignContractRequest }) => {
      const { data } = await api.post<Contract>(`/api/contracts/${id}/sign`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractsQueryKey })
    },
  })
}
