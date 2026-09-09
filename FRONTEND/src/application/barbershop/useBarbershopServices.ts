import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { BarbershopService, CreateBarbershopServiceRequest, UpdateBarbershopServiceRequest } from "@/domain/types/barbershop"

export const barbershopServicesKey = ["barbershop-services"] as const

export function useBarbershopServices() {
  const api = useApiClient()
  return useQuery({
    queryKey: barbershopServicesKey,
    queryFn: async () => {
      const { data } = await api.get<BarbershopService[]>("/api/barbershop/services")
      return data
    },
    retry: false,
  })
}

export function useCreateBarbershopService() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: CreateBarbershopServiceRequest) => {
      const { data } = await api.post<BarbershopService>("/api/barbershop/services", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopServicesKey })
    },
  })
}

export function useUpdateBarbershopService() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, request }: { id: number; request: UpdateBarbershopServiceRequest }) => {
      const { data } = await api.put<BarbershopService>(`/api/barbershop/services/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopServicesKey })
    },
  })
}

export function useDeleteBarbershopService() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      await api.delete(`/api/barbershop/services/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopServicesKey })
    },
  })
}
