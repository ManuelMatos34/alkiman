import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { contractsQueryKey } from "@/application/contracts/useContracts"
import type { Contract } from "@/domain/types/contract"

/** Reenvía por correo el PDF ya generado de un contrato, a demanda (no solo automáticamente al crearse). */
export function useResendContractEmail() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await api.post<Contract>(`/api/contracts/${id}/resend-email`)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractsQueryKey })
    },
  })
}
