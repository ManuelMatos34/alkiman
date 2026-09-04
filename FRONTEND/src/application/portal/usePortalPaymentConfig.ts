import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { PortalPaymentConfig } from "@/domain/types/portalPayment"

/**
 * Config pública de pagos (clave publicable de Stripe en modo sandbox) de un link
 * del Portal. Se pide una sola vez al montar la página de checkout. Ver nota de
 * `usePortalCatalog` sobre por qué usa el cliente HTTP base sin auth.
 */
export function usePortalPaymentConfig(slug: string | undefined) {
  return useQuery({
    queryKey: ["portal-payment-config", slug],
    queryFn: async () => {
      const { data } = await apiClient.get<PortalPaymentConfig>(
        `/api/portal/${slug}/payment/config`
      )
      return data
    },
    enabled: !!slug,
    retry: false,
    staleTime: Infinity,
  })
}
