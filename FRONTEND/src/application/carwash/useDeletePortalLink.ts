import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashPortalLinksQueryKey } from "@/application/carwash/useCarwashPortalLinks"

export function useDeletePortalLink() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/carwash/portal-links/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashPortalLinksQueryKey })
    },
  })
}
