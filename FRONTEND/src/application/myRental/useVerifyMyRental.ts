import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { MyRentalDetails, VerifyMyRentalPayload } from "@/domain/types/myRental"

/**
 * Verificación de identidad del cliente en su link público `/mi-renta/{token}`. Usa el cliente
 * HTTP base SIN el interceptor de auth (`useApiClient`): esta pantalla se renderiza fuera de
 * ProtectedRoute, no hay JWT ni sesión, y un 401 nunca debería redirigir a /login acá.
 */
export function useVerifyMyRental(token: string | undefined) {
  return useMutation({
    mutationFn: async (payload: VerifyMyRentalPayload) => {
      const { data } = await apiClient.post<MyRentalDetails>(
        `/api/my-rental/${token}/verify`,
        payload
      )
      return data
    },
  })
}
