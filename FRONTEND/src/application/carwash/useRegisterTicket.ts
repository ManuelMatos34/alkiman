import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashTicket, RegisterCarwashTicketRequest } from "@/domain/types/carwash"

/** Registro presencial de un vehículo en la cola (lo hace el Encargado). */
export function useRegisterTicket() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: RegisterCarwashTicketRequest) => {
      const { data } = await api.post<CarwashTicket>("/api/carwash/queue/register", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
    },
  })
}
