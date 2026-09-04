import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { PortalCheckoutRequest, PortalCheckoutResponse } from "@/domain/types/portalPublic"

/** Submit final de la pasarela pública. Ver nota de `usePortalCatalog` sobre por qué no usa `useApiClient`. */
export function usePortalCheckout(slug: string | undefined) {
  return useMutation({
    mutationFn: async (request: PortalCheckoutRequest) => {
      const { data } = await apiClient.post<PortalCheckoutResponse>(
        `/api/portal/${slug}/checkout`,
        request
      )
      return data
    },
  })
}
