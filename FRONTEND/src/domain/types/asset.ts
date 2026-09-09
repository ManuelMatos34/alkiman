export type AssetStatus = "Available" | "Rented" | "Maintenance"

/** Unidad de tiempo en la que se renta el activo: determina el mínimo que puede rentar
 *  un cliente en el Portal (no puede rentar menos de 1 período de esta unidad) y cómo
 *  se calcula el precio total (BasePrice x Periods x Quantity). */
export type RentalTypeOption = "Daily" | "Weekly" | "Biweekly" | "Monthly" | "Annual"

/** Etiqueta legible en español de cada unidad de renta. Fuente única para toda la UI
 *  (formulario de activos, catálogo público y checkout del Portal). */
export const rentalTypeLabels: Record<RentalTypeOption, string> = {
  Daily: "Diario",
  Weekly: "Semanal",
  Biweekly: "Quincenal",
  Monthly: "Mensual",
  Annual: "Anual",
}

/** Nombre (singular) del período de cada unidad, para armar textos como "1 mes". */
export const rentalPeriodUnitLabels: Record<RentalTypeOption, string> = {
  Daily: "día",
  Weekly: "semana",
  Biweekly: "quincena",
  Monthly: "mes",
  Annual: "año",
}

/** Nombre (plural) del período de cada unidad, para armar textos como "Cantidad de meses". */
export const rentalPeriodUnitPluralLabels: Record<RentalTypeOption, string> = {
  Daily: "días",
  Weekly: "semanas",
  Biweekly: "quincenas",
  Monthly: "meses",
  Annual: "años",
}

/** Suma `periods` unidades de `rentalType` a `startDate` (mismo criterio que
 *  Alkiman.Domain.Common.RentalPeriodCalculator en el backend). Se usa solo para la
 *  vista previa en el checkout del Portal; la fecha de fin real siempre la calcula y
 *  devuelve el servidor. */
export function addRentalPeriods(startDate: Date, rentalType: RentalTypeOption, periods: number): Date {
  const result = new Date(startDate)
  switch (rentalType) {
    case "Daily":
      result.setDate(result.getDate() + periods)
      break
    case "Weekly":
      result.setDate(result.getDate() + periods * 7)
      break
    case "Biweekly":
      result.setDate(result.getDate() + periods * 15)
      break
    case "Monthly":
      result.setMonth(result.getMonth() + periods)
      break
    case "Annual":
      result.setFullYear(result.getFullYear() + periods)
      break
  }
  return result
}

export interface Asset {
  id: string
  categoryId: number
  name: string
  description: string | null
  imageUrl: string | null
  status: AssetStatus
  rentalType: RentalTypeOption
  basePrice: number
  stock: number
  createdAt: string
}

export interface CreateAssetRequest {
  categoryId: number
  name: string
  description?: string | null
  imageUrl?: string | null
  rentalType: RentalTypeOption
  basePrice: number
  stock: number
}

export interface UpdateAssetRequest {
  categoryId: number
  name: string
  description?: string | null
  imageUrl?: string | null
  basePrice: number
  stock: number
}
