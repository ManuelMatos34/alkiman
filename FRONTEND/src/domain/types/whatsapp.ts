export interface WhatsAppStats {
  sentThisMonth: number
  limit: number
  warningThreshold: number
  isWarning: boolean
  isAtLimit: boolean
}

export interface WhatsAppConfig {
  isConfigured: boolean
  phoneNumber: string | null
  displayName: string | null
  isActive: boolean
}

export interface WhatsAppConfigRequest {
  phoneNumberId: string
  accessToken: string
  phoneNumber: string | null
  displayName: string | null
}
