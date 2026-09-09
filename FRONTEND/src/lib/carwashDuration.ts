/**
 * Duraciones ofrecidas en los desplegables de Carwash (servicios y agregados).
 *
 * La lista es común a los dos, salvo por el 0: un agregado puede no sumar tiempo
 * a la cola (un aromatizante se aplica mientras el auto ya está listo), pero un
 * servicio de 0 minutos dejaría el turno sin estimación y rompería el cálculo de
 * espera del portal. Por eso el 0 se filtra según quién pregunte en vez de
 * mantener dos listas que se van a desincronizar.
 */
const DURATION_PRESETS_MINUTES = [0, 5, 10, 15, 20, 30, 45, 60, 90, 120]

interface DurationOptionsConfig {
  /** `true` para agregados (0 = no suma tiempo), `false` para servicios. */
  allowZero: boolean
}

/**
 * Devuelve los presets, insertando `saved` si quedó fuera de la lista.
 *
 * Un registro cargado antes de que existieran estos presets puede tener una
 * duración arbitraria (7 min). Sin esta inyección el Select abriría en blanco y
 * el primer guardado pisaría ese valor en silencio: convertir un input libre en
 * una lista cerrada no debería destruir datos ya existentes.
 */
export function buildDurationOptions(
  saved: number | undefined,
  { allowZero }: DurationOptionsConfig
): number[] {
  const presets = allowZero
    ? DURATION_PRESETS_MINUTES
    : DURATION_PRESETS_MINUTES.filter((minutes) => minutes > 0)

  const minimum = allowZero ? 0 : 1
  if (
    saved === undefined ||
    !Number.isInteger(saved) ||
    saved < minimum ||
    presets.includes(saved)
  ) {
    return presets
  }

  return [...presets, saved].sort((a, b) => a - b)
}
