export type EmailType = "Individual" | "Mass"
export type EmailStatus = "Sent" | "Failed"

export interface EmailMessage {
  id: string
  type: EmailType
  customerId: string | null
  recipientName: string
  recipientEmail: string
  subject: string
  body: string
  status: EmailStatus
  errorMessage: string | null
  sentAt: string | null
  createdAt: string
}

export interface SendIndividualEmailRequest {
  customerId?: string | null
  recipientName: string
  recipientEmail: string
  subject: string
  body: string
}

export interface SendMassEmailRequest {
  customerIds: string[]
  subject: string
  body: string
}

export interface SendMassEmailResponse {
  sent: number
  failed: number
  messages: EmailMessage[]
}
