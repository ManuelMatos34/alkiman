import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type {
  CarwashPaymentIntentRequest,
  CarwashStripeIntent,
} from "@/domain/types/carwashPublic"

/**
 * Crea el PaymentIntent del turno con lo elegido en los pasos anteriores
 * (servicio, agregados y propina). El total NO se manda: lo calcula el servidor
 * con los precios del catálogo y vuelve en `amount`, que es el que se muestra.
 */
export function useCreateCarwashStripeIntent(slug: string | undefined) {
  return useMutation({
    mutationFn: async (request: CarwashPaymentIntentRequest) => {
      const { data } = await apiClient.post<CarwashStripeIntent>(
        `/api/carwash/public/${slug}/payment/stripe/intent`,
        request
      )
      return data
    },
  })
}
