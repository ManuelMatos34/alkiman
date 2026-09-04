/**
 * Mapea el idioma de la app (i18next: "es" | "en") al locale de `Intl` que se usa para
 * formatear números/monedas/fechas (`Intl.NumberFormat`, `Intl.DateTimeFormat`). La moneda
 * (DOP) no cambia con el idioma -- solo cambia cómo se puntúa/lee el número y la fecha.
 */
export function getIntlLocale(language: string): string {
  return language.startsWith("en") ? "en-US" : "es-DO"
}
