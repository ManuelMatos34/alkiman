import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashServicesQueryKey } from "@/application/carwash/useCarwashServices"
import type { CarwashServiceItem, UpdateCarwashServiceRequest } from "@/domain/types/carwash"

export function useUpdateService() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: UpdateCarwashServiceRequest }) => {
      const { data } = await api.put<CarwashServiceItem>(`/api/carwash/services/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashServicesQueryKey })
    },
  })
}
