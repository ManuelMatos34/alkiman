export type ThemeMode = "light" | "dark"

/**
 * Colores de acento que el negocio puede elegir en Apariencia.
 *
 * El orden es el que ve el usuario en la paleta, y va siguiendo la rueda de
 * color (azules -> verdes -> cálidos -> magentas) para que elegir sea recorrer
 * un degradé y no un montón de puntos sueltos. `slate` va al final aparte: es
 * el neutro, para el negocio que no quiere color de marca.
 *
 * Agregar uno acá NO alcanza. Hay que tocar, en el mismo commit:
 *   1. index.css              -> los bloques [data-accent="..."] claro y oscuro.
 *   2. AppearanceSettingsForm -> accentSwatchClasses (la bolita de la paleta).
 *   3. locales es/en settingsForms.json -> appearance.accentLabels.
 *   4. LandlordService.ValidAccentColors -> si no, el back rechaza el guardado.
 *
 * Si falta (1) el color se guarda pero la app no cambia; si falta (4) el
 * usuario elige y le vuelve "El color de acento es inválido".
 */
export const ACCENT_COLORS = [
  "blue",
  "sky",
  "cyan",
  "teal",
  "green",
  "orange",
  "red",
  "rose",
  "pink",
  "fuchsia",
  "violet",
  "slate",
] as const
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
