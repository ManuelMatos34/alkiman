import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashWashersQueryKey } from "@/application/carwash/useCarwashWashers"
import { carwashLinkableUsersQueryKey } from "@/application/carwash/useCarwashLinkableUsers"
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
      // Si se vinculó una cuenta, esa cuenta ya no está disponible para otro lavador.
      queryClient.invalidateQueries({ queryKey: carwashLinkableUsersQueryKey })
    },
  })
}
