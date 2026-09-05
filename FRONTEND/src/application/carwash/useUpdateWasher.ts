import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashWashersQueryKey } from "@/application/carwash/useCarwashWashers"
import { carwashQueueQueryKey } from "@/application/carwash/useCarwashQueue"
import type { CarwashWasher, UpdateCarwashWasherRequest } from "@/domain/types/carwash"

export function useUpdateWasher() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateCarwashWasherRequest }) => {
      const { data } = await api.put<CarwashWasher>(`/api/carwash/washers/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashWashersQueryKey })
      // El tablero muestra el nombre del lavador en cada tarjeta: si se renombró,
      // la cola quedó desactualizada.
      queryClient.invalidateQueries({ queryKey: carwashQueueQueryKey })
    },
  })
}
