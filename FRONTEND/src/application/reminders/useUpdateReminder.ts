import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { remindersQueryKey } from "@/application/reminders/useReminders"
import type { Reminder, UpdateReminderRequest } from "@/domain/types/reminder"

export function useUpdateReminder() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateReminderRequest }) => {
      const { data } = await api.put<Reminder>(`/api/reminders/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: remindersQueryKey })
    },
  })
}
