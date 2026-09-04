/** Body para pedir la previsualización del texto del contrato antes de firmarlo/pagarlo. */
export interface PortalContractPreviewRequest {
  assetId: string
  fullName: string
  identityNumber?: string | null
  email?: string | null
  phone?: string | null
  address?: string | null
  startDate: string
  periods: number
  quantity: number
}

/** Texto ya renderizado del contrato (con saltos de línea `\n`) más el resumen calculado. */
export interface PortalContractPreviewResponse {
  content: string
  endDate: string
  totalPrice: number
}
