import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashTicket, CarwashTicketStatus } from "@/domain/types/carwash"

/** Avanza un ticket al siguiente estado del lavado (InProgress -> Drying -> Ready -> Delivered). */
export function useAdvanceTicketStatus() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: CarwashTicketStatus }) => {
      const { data } = await api.put<CarwashTicket>(`/api/carwash/queue/${id}/status`, { status })
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
    },
  })
}
