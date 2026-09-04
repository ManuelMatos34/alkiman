import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { rolesQueryKey } from "@/application/roles/useRoles"
import type { CreateRoleRequest, Role } from "@/domain/types/role"

export function useCreateRole() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateRoleRequest) => {
      const { data } = await api.post<Role>("/api/roles", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rolesQueryKey })
    },
  })
}
