import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { portalLinksQueryKey } from "@/application/portalLinks/usePortalLinks"
import type { PortalLink, UpdatePortalLinkRequest } from "@/domain/types/portal"

export function useUpdatePortalLink() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdatePortalLinkRequest }) => {
      const { data } = await api.put<PortalLink>(`/api/portal-links/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: portalLinksQueryKey })
    },
  })
}
