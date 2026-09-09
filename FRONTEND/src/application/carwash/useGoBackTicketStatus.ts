import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashTicket } from "@/domain/types/carwash"

/** Retrocede un ticket al estado anterior. Requiere carwash.work. */
export function useGoBackTicketStatus() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await api.post<CarwashTicket>(`/api/carwash/queue/${id}/go-back`, null)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
    },
  })
}
