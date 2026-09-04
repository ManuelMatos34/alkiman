import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { CarwashPublicTicketStatus, JoinCarwashQueueRequest } from "@/domain/types/carwashPublic"

/**
 * Auto-registro del cliente en la cola desde su link público `/lavado/{slug}`. Usa el cliente
 * HTTP base SIN el interceptor de auth: esta pantalla se renderiza fuera de ProtectedRoute.
 * Devuelve ya el estado del turno, así que el `accessToken` de la respuesta es lo que se usa
 * para navegar a la página de seguimiento sin una segunda llamada.
 */
export function useJoinCarwashQueue(slug: string | undefined) {
  return useMutation({
    mutationFn: async (request: JoinCarwashQueueRequest) => {
      const { data } = await apiClient.post<CarwashPublicTicketStatus>(
        `/api/carwash/public/${slug}/join`,
        request
      )
      return data
    },
  })
}
