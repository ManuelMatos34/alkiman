export type ContractStatus = "Pending" | "Signed"

/** Contrato generado automáticamente al crear una renta (manual o vía Portal). */
export interface Contract {
  id: string
  rentalId: string
  customerId: string
  customerName: string
  contractTemplateId: number | null
  templateName: string | null
  status: ContractStatus
  signedAt: string | null
  emailSent: boolean
  createdAt: string
}

export interface SignContractRequest {
  signatureImageBase64: string
}
