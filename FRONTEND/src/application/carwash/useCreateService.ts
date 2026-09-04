import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashServicesQueryKey } from "@/application/carwash/useCarwashServices"
import type { CarwashServiceItem, CreateCarwashServiceRequest } from "@/domain/types/carwash"

export function useCreateService() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateCarwashServiceRequest) => {
      const { data } = await api.post<CarwashServiceItem>("/api/carwash/services", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashServicesQueryKey })
    },
  })
}
