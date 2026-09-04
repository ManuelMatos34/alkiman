import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { EmailMessage } from "@/domain/types/email"

export const emailsQueryKey = ["emails"] as const

/** Historial de correos individuales y masivos enviados por el negocio autenticado. */
export function useEmails() {
  const api = useApiClient()

  return useQuery({
    queryKey: emailsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<EmailMessage[]>("/api/emails")
      return data
    },
  })
}
