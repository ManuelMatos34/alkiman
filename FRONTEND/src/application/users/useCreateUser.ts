import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { usersQueryKey } from "@/application/users/useUsers"
import type { CreateUserRequest, CreateUserResponse } from "@/domain/types/user"

export function useCreateUser() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateUserRequest) => {
      const { data } = await api.post<CreateUserResponse>("/api/users", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usersQueryKey })
    },
  })
}
