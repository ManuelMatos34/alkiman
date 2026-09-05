import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashWashersQueryKey } from "@/application/carwash/useCarwashWashers"

/** Sólo funciona si el lavador nunca lavó nada; si tiene turnos, el backend responde 400 y hay que desactivarlo. */
export function useDeleteWasher() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/carwash/washers/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashWashersQueryKey })
    },
  })
}
