import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { CarwashPublicPaymentConfig } from "@/domain/types/carwashPublic"

/**
 * Clave publicable de Stripe del link público de Carwash. Cliente HTTP base sin
 * el interceptor de auth, por el mismo motivo que `useCarwashPublicLink`: acá no
 * hay sesión y un 401 no debe mandar a /login.
 *
 * `enabled` lo controla el llamador: si el link dice que el negocio no cobra en
 * línea, la pasarela ni siquiera muestra el paso de pago y pedir esto sería una
 * llamada de más.
 */
export function useCarwashPublicPaymentConfig(slug: string | undefined, enabled = true) {
  return useQuery({
    queryKey: ["carwash-public-payment-config", slug],
    queryFn: async () => {
      const { data } = await apiClient.get<CarwashPublicPaymentConfig>(
        `/api/carwash/public/${slug}/payment/config`
      )
      return data
    },
    enabled: !!slug && enabled,
    retry: false,
    staleTime: Infinity,
  })
}
