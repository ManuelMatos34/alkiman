import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { WhatsAppConfig, WhatsAppConfigRequest } from "@/domain/types/whatsapp"

export const whatsAppConfigQueryKey = ["whatsapp-config"] as const

export function useWhatsAppConfig() {
  const api = useApiClient()

  return useQuery({
    queryKey: whatsAppConfigQueryKey,
    queryFn: async () => {
      const { data } = await api.get<WhatsAppConfig>("/api/whatsapp/config")
      return data
    },
  })
}

export function useSaveWhatsAppConfig() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: WhatsAppConfigRequest) => {
      const { data } = await api.put<WhatsAppConfig>("/api/whatsapp/config", request)
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(whatsAppConfigQueryKey, data)
    },
  })
}
