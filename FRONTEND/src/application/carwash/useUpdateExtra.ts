import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashExtrasQueryKey } from "@/application/carwash/useCarwashExtras"
import type { CarwashExtraItem, UpdateCarwashExtraRequest } from "@/domain/types/carwash"

export function useUpdateExtra() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: UpdateCarwashExtraRequest }) => {
      const { data } = await api.put<CarwashExtraItem>(`/api/carwash/extras/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashExtrasQueryKey })
    },
  })
}
