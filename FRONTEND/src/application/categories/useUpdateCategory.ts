import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { categoriesQueryKey } from "@/application/categories/useCategories"
import type { Category, UpdateCategoryRequest } from "@/domain/types/category"

export function useUpdateCategory() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: UpdateCategoryRequest }) => {
      const { data } = await api.put<Category>(`/api/categories/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoriesQueryKey })
    },
  })
}
