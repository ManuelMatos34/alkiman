import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { portalLinksQueryKey } from "@/application/portalLinks/usePortalLinks"
import type { CreatePortalLinkRequest, PortalLink } from "@/domain/types/portal"

export function useCreatePortalLink() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreatePortalLinkRequest) => {
      const { data } = await api.post<PortalLink>("/api/portal-links", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: portalLinksQueryKey })
    },
  })
}
