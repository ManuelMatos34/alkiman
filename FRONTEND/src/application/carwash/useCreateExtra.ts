import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashExtrasQueryKey } from "@/application/carwash/useCarwashExtras"
import type { CarwashExtraItem, CreateCarwashExtraRequest } from "@/domain/types/carwash"

export function useCreateExtra() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateCarwashExtraRequest) => {
      const { data } = await api.post<CarwashExtraItem>("/api/carwash/extras", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashExtrasQueryKey })
    },
  })
}
