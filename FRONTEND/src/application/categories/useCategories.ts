import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Category } from "@/domain/types/category"

export const categoriesQueryKey = ["categories"] as const

/** Listado de categorías del negocio autenticado. */
export function useCategories() {
  const api = useApiClient()

  return useQuery({
    queryKey: categoriesQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Category[]>("/api/categories")
      return data
    },
  })
}
