import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { usersQueryKey } from "@/application/users/useUsers"
import type { UpdateUserRequest, User } from "@/domain/types/user"

export function useUpdateUser() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateUserRequest }) => {
      const { data } = await api.put<User>(`/api/users/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usersQueryKey })
    },
  })
}
