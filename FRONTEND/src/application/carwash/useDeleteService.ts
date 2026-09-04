import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashServicesQueryKey } from "@/application/carwash/useCarwashServices"

export function useDeleteService() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      await api.delete(`/api/carwash/services/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashServicesQueryKey })
    },
  })
}
