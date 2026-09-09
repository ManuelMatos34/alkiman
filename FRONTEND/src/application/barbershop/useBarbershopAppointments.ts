import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { BarbershopAppointment, BarbershopSlot, CreateBarbershopAppointmentRequest } from "@/domain/types/barbershop"

export const barbershopAppointmentsKey = (date: string) =>
  ["barbershop-appointments", date] as const

export const barbershopAppointmentsRangeKey = (from: string, to: string) =>
  ["barbershop-appointments-range", from, to] as const

export function useBarbershopAppointments(date: string) {
  const api = useApiClient()
  return useQuery({
    queryKey: barbershopAppointmentsKey(date),
    queryFn: async () => {
      const { data } = await api.get<BarbershopAppointment[]>("/api/barbershop/appointments", {
        params: { date },
      })
      return data
    },
    retry: false,
    refetchInterval: 30_000,
  })
}

export function useBarbershopAppointmentsRange(from: string, to: string) {
  const api = useApiClient()
  return useQuery({
    queryKey: barbershopAppointmentsRangeKey(from, to),
    queryFn: async () => {
      const { data } = await api.get<BarbershopAppointment[]>("/api/barbershop/appointments", {
        params: { from, to },
      })
      return data
    },
    enabled: !!from && !!to,
    retry: false,
    refetchInterval: 60_000,
  })
}

export function useCreateBarbershopAppointment() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: CreateBarbershopAppointmentRequest) => {
      const { data } = await api.post<BarbershopAppointment>("/api/barbershop/appointments", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["barbershop-appointments"] })
    },
  })
}

export function useAdvanceBarbershopAppointment() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, isPaid }: { id: string; isPaid?: boolean }) => {
      const { data } = await api.put<BarbershopAppointment>(
        `/api/barbershop/appointments/${id}/status`,
        { isPaid: isPaid ?? false }
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["barbershop-appointments"] })
    },
  })
}

export function useMarkBarbershopAppointmentPaid() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await api.put<BarbershopAppointment>(`/api/barbershop/appointments/${id}/pay`)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["barbershop-appointments"] })
    },
  })
}

export function useCancelBarbershopAppointment() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/api/barbershop/appointments/${id}/cancel`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["barbershop-appointments"] })
    },
  })
}

export function useBarbershopSlots(date: string, serviceId?: number | null) {
  const api = useApiClient()
  return useQuery({
    queryKey: ["barbershop-slots", date, serviceId ?? null],
    queryFn: async () => {
      const params: Record<string, unknown> = { date }
      if (serviceId != null) params.serviceId = serviceId
      const { data } = await api.get<BarbershopSlot[]>("/api/barbershop/slots", { params })
      return data
    },
    enabled: !!date,
    retry: false,
  })
}

export function usePublicBarbershopSlots(slug: string, date: string, serviceId?: number | null) {
  return useQuery({
    queryKey: ["barbershop-public-slots", slug, date, serviceId ?? null],
    queryFn: async () => {
      const params: Record<string, unknown> = { date }
      if (serviceId != null) params.serviceId = serviceId
      const { data } = await apiClient.get<BarbershopSlot[]>(
        `/api/barbershop/public/${slug}/slots`,
        { params }
      )
      return data
    },
    enabled: !!slug && !!date,
    retry: false,
  })
}
