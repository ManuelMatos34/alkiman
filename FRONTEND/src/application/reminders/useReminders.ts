import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Reminder } from "@/domain/types/reminder"

export const remindersQueryKey = ["reminders"] as const

/** Recordatorios de seguimiento del negocio autenticado. */
export function useReminders() {
  const api = useApiClient()

  return useQuery({
    queryKey: remindersQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Reminder[]>("/api/reminders")
      return data
    },
  })
}
