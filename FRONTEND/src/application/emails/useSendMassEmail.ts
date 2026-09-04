import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { emailsQueryKey } from "@/application/emails/useEmails"
import type { SendMassEmailRequest, SendMassEmailResponse } from "@/domain/types/email"

export function useSendMassEmail() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: SendMassEmailRequest) => {
      const { data } = await api.post<SendMassEmailResponse>("/api/emails/mass", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: emailsQueryKey })
    },
  })
}
