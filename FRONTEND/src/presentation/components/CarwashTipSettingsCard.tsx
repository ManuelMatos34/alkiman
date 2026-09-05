import { useEffect, useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { HandCoins, Loader2 } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useCarwashSettings } from "@/application/carwash/useCarwashSettings"
import { useSaveCarwashSettings } from "@/application/carwash/useSaveCarwashSettings"
import type { CarwashTipMode } from "@/domain/types/carwash"

const TIP_MODES: CarwashTipMode[] = ["Disabled", "Optional", "Suggested"]

/**
 * Política de propinas del negocio.
 *
 * Vive en Lavadores y no en una pantalla de ajustes aparte porque la propina no es una
 * configuración del catálogo: es plata de esta gente, y el ranking que la resume está a
 * un click de acá.
 *
 * Ojo con lo que NO hay: no existe un modo "cobrar siempre". Carwash no procesa pagos —
 * se cobra en efectivo en el mostrador — así que un monto agregado automáticamente no
 * sería una propina sino un aumento de precio, y quedaría asentado como recibido aunque
 * el cliente nunca lo haya dado. Lo que el software puede hacer es preguntar mejor, no
 * cobrar: por eso los tres modos sólo cambian cómo se abre el diálogo de entrega.
 */
export function CarwashTipSettingsCard() {
  const { t } = useTranslation("carwash")
  const { data: settings } = useCarwashSettings()
  const saveSettings = useSaveCarwashSettings()

  const [mode, setMode] = useState<CarwashTipMode>("Optional")
  const [percent, setPercent] = useState("10")

  // El formulario arranca vacío y se siembra cuando llega la query: sin esto, editar y
  // que el refetch pise lo tipeado sería el comportamiento por defecto.
  useEffect(() => {
    if (!settings) return
    setMode(settings.tipMode)
    setPercent(String(settings.tipSuggestedPercent))
  }, [settings])

  const parsedPercent = Number(percent)
  const isPercentInvalid =
    mode === "Suggested" &&
    (percent.trim() === "" || Number.isNaN(parsedPercent) || parsedPercent < 0 || parsedPercent > 100)

  const isDirty =
    !!settings && (settings.tipMode !== mode || settings.tipSuggestedPercent !== parsedPercent)

  function handleSave() {
    if (!settings?.operationMode || isPercentInvalid) return
    saveSettings.mutate(
      {
        // El endpoint guarda la configuración completa, así que hay que reenviar el modo
        // de operación tal cual está: omitirlo lo borraría y volvería a disparar el
        // diálogo de configuración inicial.
        operationMode: settings.operationMode,
        tipMode: mode,
        tipSuggestedPercent: mode === "Suggested" ? parsedPercent : settings.tipSuggestedPercent,
      },
      {
        onSuccess: () => toast.success(t("tips.settings.toast.saved")),
        onError: () => toast.error(t("tips.settings.toast.error")),
      }
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <HandCoins className="h-5 w-5" />
          {t("tips.settings.title")}
        </CardTitle>
        <p className="text-sm text-muted-foreground">{t("tips.settings.subtitle")}</p>
      </CardHeader>
      <CardContent className="space-y-4 px-6">
        <div className="grid gap-3 sm:grid-cols-3">
          {TIP_MODES.map((value) => (
            <button
              key={value}
              type="button"
              onClick={() => setMode(value)}
              className={cn(
                "rounded-lg border p-4 text-left transition-colors",
                mode === value
                  ? "border-primary bg-primary/5"
                  : "border-border/60 hover:border-border"
              )}
            >
              <p className="text-sm font-medium">{t(`tips.settings.modes.${value}.title`)}</p>
              <p className="mt-1 text-xs text-muted-foreground">
                {t(`tips.settings.modes.${value}.description`)}
              </p>
            </button>
          ))}
        </div>

        {mode === "Suggested" && (
          <div className="max-w-xs space-y-1.5">
            <Label htmlFor="carwash-tip-percent">{t("tips.settings.percent")}</Label>
            <Input
              id="carwash-tip-percent"
              type="number"
              min={0}
              max={100}
              step="0.5"
              value={percent}
              onChange={(event) => setPercent(event.target.value)}
              className={cn(isPercentInvalid && "border-destructive")}
            />
            <p
              className={cn(
                "text-xs",
                isPercentInvalid ? "text-destructive" : "text-muted-foreground"
              )}
            >
              {isPercentInvalid
                ? t("tips.settings.percentInvalid")
                : t("tips.settings.percentHint")}
            </p>
          </div>
        )}

        <div className="flex justify-end">
          <Button
            onClick={handleSave}
            disabled={!isDirty || isPercentInvalid || saveSettings.isPending}
          >
            {saveSettings.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
            {t("tips.settings.save")}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
