import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { BarbershopStylist, CreateBarbershopStylistRequest, UpdateBarbershopStylistRequest } from "@/domain/types/barbershop"

export const barbershopStylistsKey = ["barbershop-stylists"] as const

export function useBarbershopStylists(enabled = true) {
  const api = useApiClient()
  return useQuery({
    queryKey: barbershopStylistsKey,
    queryFn: async () => {
      const { data } = await api.get<BarbershopStylist[]>("/api/barbershop/stylists")
      return data
    },
    enabled,
    retry: false,
  })
}

export function useCreateBarbershopStylist() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: CreateBarbershopStylistRequest) => {
      const { data } = await api.post<BarbershopStylist>("/api/barbershop/stylists", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopStylistsKey })
    },
  })
}

export function useUpdateBarbershopStylist() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateBarbershopStylistRequest }) => {
      const { data } = await api.put<BarbershopStylist>(`/api/barbershop/stylists/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopStylistsKey })
    },
  })
}

export function useDeleteBarbershopStylist() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/barbershop/stylists/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopStylistsKey })
    },
  })
}
