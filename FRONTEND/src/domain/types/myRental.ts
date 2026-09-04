import type { RentalRequestType } from "@/domain/types/rentalRequest"
import type { AccentColor, ThemeMode } from "@/domain/types/landlord"

/** Pedido pendiente resumido, tal como lo ve el cliente en su propia renta. */
export interface MyRentalPendingRequest {
  id: string
  type: RentalRequestType
  createdAt: string
}

/** Detalle de la renta tal como lo ve el cliente en su link público `/mi-renta/{token}`. */
export interface MyRentalDetails {
  rentalId: string
  assetName: string
  assetDescription: string | null
  startDate: string
  endDate: string
  totalPrice: number
  status: string
  contractPdfUrl: string | null
  canRequestExtension: boolean
  canRequestCancellation: boolean
  pendingRequest: MyRentalPendingRequest | null
  appName: string
  themeMode: ThemeMode
  accentColor: AccentColor
}

export interface VerifyMyRentalPayload {
  identifier: string
}

export interface CreateExtensionRequestPayload {
  identifier: string
  requestedPeriods: number
}

export interface CreateCancellationRequestPayload {
  identifier: string
  reason: string
}
