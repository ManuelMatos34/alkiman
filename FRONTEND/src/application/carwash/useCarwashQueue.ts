import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashTicket } from "@/domain/types/carwash"

export const carwashQueueQueryKey = ["carwash-queue"] as const

/**
 * Cola de vehículos del negocio autenticado. Se refresca cada pocos segundos para simular
 * tiempo real sin depender de WebSockets (aceptable para el MVP).
 */
export function useCarwashQueue() {
  const api = useApiClient()

  return useQuery({
    queryKey: carwashQueueQueryKey,
    queryFn: async () => {
      const { data } = await api.get<CarwashTicket[]>("/api/carwash/queue")
      return data
    },
    retry: false,
    refetchInterval: 6000,
  })
}
