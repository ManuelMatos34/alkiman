import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashWashersQueryKey } from "@/application/carwash/useCarwashWashers"
import type { CarwashWasher, CreateCarwashWasherRequest } from "@/domain/types/carwash"

export function useCreateWasher() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateCarwashWasherRequest) => {
      const { data } = await api.post<CarwashWasher>("/api/carwash/washers", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashWashersQueryKey })
    },
  })
}
