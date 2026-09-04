import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashPortalLinksQueryKey } from "@/application/carwash/useCarwashPortalLinks"
import type { CarwashPortalLink, CreateCarwashPortalLinkRequest } from "@/domain/types/carwash"

export function useCreatePortalLink() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateCarwashPortalLinkRequest) => {
      const { data } = await api.post<CarwashPortalLink>("/api/carwash/portal-links", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashPortalLinksQueryKey })
    },
  })
}
