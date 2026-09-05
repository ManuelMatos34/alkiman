import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashTicket, CarwashTicketStatus } from "@/domain/types/carwash"

/**
 * Avanza un ticket al siguiente estado del lavado (InProgress -> Drying -> Ready -> Delivered).
 *
 * `tipAmount` sólo aplica al pasar a `Delivered`; el backend lo ignora en el
 * resto de las transiciones.
 */
export function useAdvanceTicketStatus() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      id,
      status,
      tipAmount,
    }: {
      id: string
      status: CarwashTicketStatus
      tipAmount?: number | null
    }) => {
      const { data } = await api.put<CarwashTicket>(`/api/carwash/queue/${id}/status`, {
        status,
        tipAmount: tipAmount ?? null,
      })
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
      // Entregar un vehículo cambia volumen, facturación y propinas del tablero.
      queryClient.invalidateQueries({ queryKey: ["carwash-metrics"] })
    },
  })
}
