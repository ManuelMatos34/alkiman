export type ThemeMode = "light" | "dark"

export const ACCENT_COLORS = ["blue", "green", "violet", "orange", "pink", "red"] as const
export type AccentColor = (typeof ACCENT_COLORS)[number]

export interface Landlord {
  id: string
  businessName: string
  appName: string
  themeMode: ThemeMode
  accentColor: AccentColor
  countryId: number | null
  stateId: number | null
  cityId: number | null
  address: string | null
  phone1: string | null
  phone2: string | null
  taxId: string | null
  createdAt: string
  signatureBase64: string | null
}

export interface UpdateLandlordProfileRequest {
  businessName: string
  countryId?: number | null
  stateId?: number | null
  cityId?: number | null
  address?: string | null
  phone1?: string | null
  phone2?: string | null
  taxId?: string | null
}

export interface UpdateAppearanceRequest {
  appName: string
  themeMode: ThemeMode
  accentColor: AccentColor
}
