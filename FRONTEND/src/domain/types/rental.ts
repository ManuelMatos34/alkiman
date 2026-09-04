export type RentalStatus = "Active" | "Completed" | "Overdue" | "Cancelled"

export interface Rental {
  id: string
  assetId: string
  customerId: string
  startDate: string
  endDate: string
  contractPdfUrl: string | null
  totalPrice: number
  status: RentalStatus
  /** Token de acceso al link público de autogestión del cliente (`/mi-renta/{accessToken}`). */
  accessToken: string
  createdAt: string
}

export interface CreateRentalRequest {
  assetId: string
  customerId: string
  startDate: string
  endDate: string
  totalPrice: number
}

export interface UpdateRentalStatusRequest {
  status: RentalStatus
}
