import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashPortalLinksQueryKey } from "@/application/carwash/useCarwashPortalLinks"
import type { CarwashPortalLink } from "@/domain/types/carwash"

export function useSetPortalLinkActive() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) => {
      const { data } = await api.put<CarwashPortalLink>(
        `/api/carwash/portal-links/${id}/active`,
        { isActive }
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashPortalLinksQueryKey })
    },
  })
}
