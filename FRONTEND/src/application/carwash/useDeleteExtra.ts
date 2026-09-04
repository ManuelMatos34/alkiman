import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashExtrasQueryKey } from "@/application/carwash/useCarwashExtras"

export function useDeleteExtra() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      await api.delete(`/api/carwash/extras/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashExtrasQueryKey })
    },
  })
}
