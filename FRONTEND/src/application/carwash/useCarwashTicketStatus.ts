import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { CarwashPublicTicketStatus } from "@/domain/types/carwashPublic"

/**
 * Estado del turno tal como lo ve el cliente en `/lavado/turno/{token}`. Usa el cliente HTTP
 * base SIN el interceptor de auth (pantalla pública) y hace polling para reflejar cambios de
 * estado (ej: cuando el Encargado llama el turno) sin depender de WebSockets.
 */
export function useCarwashTicketStatus(token: string | undefined) {
  return useQuery({
    queryKey: ["carwash-ticket-status", token],
    queryFn: async () => {
      const { data } = await apiClient.get<CarwashPublicTicketStatus>(
        `/api/carwash/public/ticket/${token}`
      )
      return data
    },
    enabled: !!token,
    retry: false,
    refetchInterval: 6000,
  })
}
