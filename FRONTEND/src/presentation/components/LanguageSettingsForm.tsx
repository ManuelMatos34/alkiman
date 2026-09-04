import { Check, Languages } from "lucide-react"
import { useTranslation } from "react-i18next"
import { toast } from "sonner"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { cn } from "@/lib/utils"

const supportedLanguages = [
  { code: "es", labelKey: "language.spanish" },
  { code: "en", labelKey: "language.english" },
] as const

/**
 * Selector de idioma de la aplicación (es-LA / en). Es una preferencia del dispositivo/navegador
 * (persistida en localStorage por `i18next-browser-languagedetector`), no del negocio: cada
 * usuario puede ver el panel en el idioma que prefiera sin afectar a los demás.
 */
export function LanguageSettingsForm() {
  const { t, i18n } = useTranslation("settings")

  function handleSelect(code: string) {
    if (code === i18n.language) return
    void i18n.changeLanguage(code).then(() => {
      toast.success(t("language.saved"))
    })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("language.title")}</CardTitle>
        <CardDescription>{t("language.description")}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex flex-wrap gap-3">
          {supportedLanguages.map(({ code, labelKey }) => {
            const isActive = i18n.language === code
            return (
              <button
                key={code}
                type="button"
                onClick={() => handleSelect(code)}
                className={cn(
                  "flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-colors",
                  isActive
                    ? "border-primary bg-accent text-accent-foreground"
                    : "border-border text-muted-foreground hover:bg-muted"
                )}
              >
                <Languages className="h-4 w-4" />
                {t(labelKey)}
                {isActive && <Check className="h-4 w-4" />}
              </button>
            )
          })}
        </div>
      </CardContent>
    </Card>
  )
}
