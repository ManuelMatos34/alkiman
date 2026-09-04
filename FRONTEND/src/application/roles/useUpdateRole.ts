import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { rolesQueryKey } from "@/application/roles/useRoles"
import type { Role, UpdateRoleRequest } from "@/domain/types/role"

export function useUpdateRole() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: UpdateRoleRequest }) => {
      const { data } = await api.put<Role>(`/api/roles/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rolesQueryKey })
    },
  })
}
