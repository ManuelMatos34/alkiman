import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashTicket } from "@/domain/types/carwash"

/**
 * Endpoint exclusivo de Caja: marca un ticket como Entregado y registra la propina.
 * Requiere el permiso `carwash.caja`. Separado de `useAdvanceTicketStatus` para que
 * el rol Cajera no necesite `carwash.work` ni `carwash.manage`.
 */
export function useDeliverTicket() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, tipAmount }: { id: string; tipAmount?: number | null }) => {
      const { data } = await api.post<CarwashTicket>(
        `/api/carwash/queue/${id}/deliver`,
        { tipAmount: tipAmount ?? null }
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
      queryClient.invalidateQueries({ queryKey: ["carwash-metrics"] })
    },
  })
}
