import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { User } from "@/domain/types/user"

export const usersQueryKey = ["users"] as const

/** Listado de usuarios del negocio autenticado. */
export function useUsers() {
  const api = useApiClient()

  return useQuery({
    queryKey: usersQueryKey,
    queryFn: async () => {
      const { data } = await api.get<User[]>("/api/users")
      return data
    },
  })
}
