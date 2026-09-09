export type BarbershopAppointmentStatus = 'Scheduled' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled'
export type BarbershopAppointmentSource = 'Manual' | 'Portal'

export interface BarbershopService {
  id: number
  name: string
  description: string | null
  price: number
  durationMinutes: number
  isActive: boolean
}

export interface BarbershopStylist {
  id: string
  fullName: string
  phone: string | null
  email: string | null
  isActive: boolean
}

export interface BarbershopPortalLink {
  id: string
  stylistId: string | null
  stylistName: string | null
  title: string
  slug: string
  isActive: boolean
  createdAt: string
}

export interface BarbershopAppointment {
  id: string
  stylistId: string | null
  stylistName: string | null
  serviceId: number | null
  serviceName: string | null
  servicePrice: number | null
  clientName: string
  clientPhone: string | null
  clientEmail: string | null
  notes: string | null
  scheduledAt: string
  source: BarbershopAppointmentSource
  status: BarbershopAppointmentStatus
  isPaid: boolean
  createdAt: string
}

export interface BarbershopMetrics {
  totalAppointments: number
  completedAppointments: number
  cancelledAppointments: number
  totalRevenue: number
  dailyPoints: { date: string; count: number; revenue: number }[]
  topServices: { serviceName: string; count: number }[]
  stylistRanking: { stylistName: string; count: number; revenue: number }[]
}

// Request types
export interface CreateBarbershopServiceRequest { name: string; description?: string | null; price: number; durationMinutes: number }
export interface UpdateBarbershopServiceRequest extends CreateBarbershopServiceRequest { isActive: boolean }
export interface CreateBarbershopStylistRequest { fullName: string; phone?: string | null; email?: string | null }
export interface UpdateBarbershopStylistRequest extends CreateBarbershopStylistRequest { isActive: boolean }
export interface CreateBarbershopPortalLinkRequest { stylistId?: string | null; title: string; slug: string }
export interface CreateBarbershopAppointmentRequest {
  stylistId?: string | null; serviceId?: number | null;
  clientName: string; clientPhone?: string | null; clientEmail?: string | null;
  notes?: string | null; scheduledAt: string
}
export interface BookBarbershopAppointmentRequest {
  clientName: string; clientPhone?: string | null; clientEmail?: string | null;
  notes?: string | null; serviceId?: number | null; scheduledAt: string
}

export interface BarbershopSlot {
  slotTime: string
  isAvailable: boolean
}

// Public response types
export interface BarbershopPublicLink {
  title: string; isActive: boolean; stylistId: string | null; stylistName: string | null;
  services: BarbershopService[]
}
export interface BarbershopBookingResponse { appointmentId: string; trackingToken: string; scheduledAt: string }
export interface BarbershopAppointmentStatusResponse {
  id: string; clientName: string; stylistName: string | null; serviceName: string | null;
  scheduledAt: string; status: BarbershopAppointmentStatus
}
