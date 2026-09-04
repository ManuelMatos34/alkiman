import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashTicket } from "@/domain/types/carwash"

/** Asigna (o desasigna, con `washerId` en `null`) el lavador responsable de un vehículo. */
export function useAssignWasher() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, washerId }: { id: string; washerId: string | null }) => {
      const { data } = await api.put<CarwashTicket>(`/api/carwash/queue/${id}/assign`, { washerId })
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
    },
  })
}
