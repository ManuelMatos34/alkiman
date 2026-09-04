import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { remindersQueryKey } from "@/application/reminders/useReminders"
import type { CreateReminderRequest, Reminder } from "@/domain/types/reminder"

export function useCreateReminder() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateReminderRequest) => {
      const { data } = await api.post<Reminder>("/api/reminders", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: remindersQueryKey })
    },
  })
}
