import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { WhatsAppStats } from "@/domain/types/whatsapp"

export const whatsAppStatsQueryKey = ["whatsapp-stats"] as const

/** Estadísticas de mensajes de WhatsApp enviados este mes (límite gratuito Meta: 1,000/mes). */
export function useWhatsAppStats() {
  const api = useApiClient()

  return useQuery({
    queryKey: whatsAppStatsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<WhatsAppStats>("/api/whatsapp/stats")
      return data
    },
  })
}
