import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { portalLinksQueryKey } from "@/application/portalLinks/usePortalLinks"

export function useDeletePortalLink() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/portal-links/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: portalLinksQueryKey })
    },
  })
}
