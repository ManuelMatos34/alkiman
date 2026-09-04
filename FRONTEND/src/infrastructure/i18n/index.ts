import i18n from "i18next"
import { initReactI18next } from "react-i18next"
import LanguageDetector from "i18next-browser-languagedetector"

/**
 * Cada feature/página tiene su propio archivo de namespace (`locales/{es,en}/<namespace>.json`)
 * en vez de un único diccionario gigante. Se descubren automáticamente con `import.meta.glob`
 * para que agregar un namespace nuevo sea simplemente crear el archivo -- sin tener que tocar
 * este archivo (evita conflictos cuando se traduce en paralelo).
 */
type LocaleModule = { default: Record<string, unknown> }

const esModules = import.meta.glob("./locales/es/*.json", { eager: true }) as Record<
  string,
  LocaleModule
>
const enModules = import.meta.glob("./locales/en/*.json", { eager: true }) as Record<
  string,
  LocaleModule
>

function toNamespaceResources(modules: Record<string, LocaleModule>) {
  const result: Record<string, Record<string, unknown>> = {}
  for (const [path, mod] of Object.entries(modules)) {
    const match = /([^/]+)\.json$/.exec(path)
    if (!match) continue
    result[match[1]] = mod.default
  }
  return result
}

const resources = {
  es: toNamespaceResources(esModules),
  en: toNamespaceResources(enModules),
}

export const LANGUAGE_STORAGE_KEY = "alkiman.language"

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: "es",
    supportedLngs: ["es", "en"],
    load: "languageOnly",
    defaultNS: "common",
    ns: Object.keys(resources.es),
    interpolation: { escapeValue: false },
    detection: {
      order: ["localStorage", "navigator"],
      caches: ["localStorage"],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
    },
  })

export default i18n
