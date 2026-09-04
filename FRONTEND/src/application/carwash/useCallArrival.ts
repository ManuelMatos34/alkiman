import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashTicket } from "@/domain/types/carwash"

/** Llama el turno de un ticket registrado por portal: pasa a ArrivalPending con una ventana límite para llegar. */
export function useCallArrival() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await api.post<CarwashTicket>(`/api/carwash/queue/${id}/call-arrival`)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
    },
  })
}
