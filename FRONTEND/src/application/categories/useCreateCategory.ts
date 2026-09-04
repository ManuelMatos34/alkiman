import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { categoriesQueryKey } from "@/application/categories/useCategories"
import type { Category, CreateCategoryRequest } from "@/domain/types/category"

export function useCreateCategory() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateCategoryRequest) => {
      const { data } = await api.post<Category>("/api/categories", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoriesQueryKey })
    },
  })
}
