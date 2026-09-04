export type RentalRequestType = "Extension" | "Cancellation"
export type RentalRequestStatus = "Pending" | "Approved" | "Rejected"

/** Pedido de prórroga o cancelación que un cliente hace desde su link público `/mi-renta/{token}`. */
export interface RentalRequest {
  id: string
  rentalId: string
  assetId: string
  assetName: string
  customerId: string
  customerName: string
  type: RentalRequestType
  status: RentalRequestStatus
  requestedPeriods: number | null
  proposedEndDate: string | null
  reason: string | null
  staffNote: string | null
  reviewedAt: string | null
  reviewedBy: string | null
  createdAt: string
}

export interface ReviewRentalRequestPayload {
  staffNote?: string | null
}
