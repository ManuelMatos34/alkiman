import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type {
  CreateStripeIntentResponse,
  PortalPaymentQuoteRequest,
} from "@/domain/types/portalPayment"

/** Crea el PaymentIntent de Stripe (sandbox) para el detalle de renta ya elegido en el paso 1. */
export function useCreateStripeIntent(slug: string | undefined) {
  return useMutation({
    mutationFn: async (request: PortalPaymentQuoteRequest) => {
      const { data } = await apiClient.post<CreateStripeIntentResponse>(
        `/api/portal/${slug}/payment/stripe/intent`,
        request
      )
      return data
    },
  })
}
