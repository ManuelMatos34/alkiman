const CURRENT_YEAR = new Date().getFullYear()

/** Años desde 1980 hasta el año siguiente, del más reciente al más antiguo. */
export const VEHICLE_YEARS: string[] = Array.from(
  { length: CURRENT_YEAR + 1 - 1980 + 1 },
  (_, i) => String(CURRENT_YEAR + 1 - i)
)

/** Colores comunes de vehículos en RD. */
export const VEHICLE_COLORS: string[] = [
  "Blanco", "Negro", "Gris", "Plata", "Rojo", "Azul", "Azul Marino",
  "Verde", "Beige", "Champagne", "Marrón", "Amarillo", "Naranja", "Morado", "Dorado",
]
