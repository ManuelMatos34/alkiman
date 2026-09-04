/** Fila cruda tal como viene del CSV subido por el usuario (todo texto; el backend valida/parsea). */
export interface RentalImportRow {
  customerFullName: string
  customerIdentityNumber: string
  customerPhone?: string | null
  customerEmail?: string | null
  assetName: string
  assetCategoryName?: string | null
  assetBasePrice?: string | null
  assetStock?: string | null
  assetRentalType?: string | null
  startDate: string
  endDate: string
  totalPrice: string
  status?: string | null
}

export interface ImportRentalsRequest {
  rows: RentalImportRow[]
}

export interface RentalImportRowResult {
  rowNumber: number
  success: boolean
  errorMessage: string | null
  rentalId: string | null
  customerCreated: boolean
  assetCreated: boolean
  categoryCreated: boolean
}

export interface ImportRentalsResponse {
  total: number
  succeeded: number
  failed: number
  results: RentalImportRowResult[]
}

/** Encabezados esperados en el CSV de importación, en el mismo orden que se ofrece en la plantilla. */
export const rentalImportCsvHeaders = [
  "customerFullName",
  "customerIdentityNumber",
  "customerPhone",
  "customerEmail",
  "assetName",
  "assetCategoryName",
  "assetBasePrice",
  "assetStock",
  "assetRentalType",
  "startDate",
  "endDate",
  "totalPrice",
  "status",
] as const
