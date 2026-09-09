import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Building2, User, Check, Loader2 } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { useCarwashSettings } from "@/application/carwash/useCarwashSettings"
import { useSaveCarwashSettings } from "@/application/carwash/useSaveCarwashSettings"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import type { CarwashOperationMode } from "@/domain/types/carwash"

const MODES: { value: CarwashOperationMode; icon: typeof Building2 }[] = [
  { value: "Empresa", icon: Building2 },
  { value: "Solitario", icon: User },
]

/**
 * Configuración inicial del módulo: el negocio elige si opera como Empresa (varios lavadores,
 * el trabajo se asigna) o Solitario (una sola persona, el vehículo se auto-asigna al iniciarlo).
 *
 * A propósito NO se puede cerrar sin elegir: el resto del módulo asume un modo definido, y
 * dejarlo a medias haría que el tablero se comporte distinto según por dónde se entró. Como el
 * backend key-ea por ausencia de fila, recargar o entrar por un link directo vuelve a preguntar.
 *
 * Solo se le pregunta a quien puede administrar el módulo: un Lavador entra a trabajar la cola,
 * no a configurar el negocio.
 */
export function CarwashOperationModeDialog() {
  const { t } = useTranslation("carwash")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.CarwashBoardManage)
  const { data: settings } = useCarwashSettings()
  const saveSettings = useSaveCarwashSettings()
  const [selected, setSelected] = useState<CarwashOperationMode>("Empresa")

  const open = canManage && !!settings && settings.operationMode === null

  function handleConfirm() {
    saveSettings.mutate(
      { operationMode: selected },
      {
        onSuccess: () => toast.success(t("setup.toast.success")),
        onError: () => toast.error(t("setup.toast.error")),
      }
    )
  }

  return (
    <Dialog open={open}>
      <DialogContent className="sm:max-w-xl" showCloseButton={false}>
        <DialogHeader>
          <DialogTitle>{t("setup.title")}</DialogTitle>
          <DialogDescription>{t("setup.description")}</DialogDescription>
        </DialogHeader>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {MODES.map((mode) => {
            const isSelected = selected === mode.value
            return (
              <button
                key={mode.value}
                type="button"
                onClick={() => setSelected(mode.value)}
                aria-pressed={isSelected}
                className={cn(
                  "relative flex flex-col gap-2 rounded-lg border p-4 text-left transition-colors",
                  isSelected
                    ? "border-primary bg-primary/5"
                    : "border-border hover:border-primary/40 hover:bg-muted/50"
                )}
              >
                {isSelected && (
                  <span className="absolute right-3 top-3 flex h-5 w-5 items-center justify-center rounded-full bg-primary text-primary-foreground">
                    <Check className="h-3 w-3" />
                  </span>
                )}
                <mode.icon className="h-5 w-5 text-primary" />
                <span className="text-sm font-semibold">{t(`setup.modes.${mode.value}.title`)}</span>
                <span className="text-xs text-muted-foreground">
                  {t(`setup.modes.${mode.value}.description`)}
                </span>
              </button>
            )
          })}
        </div>

        <p className="text-xs text-muted-foreground">{t("setup.hint")}</p>

        <Button onClick={handleConfirm} disabled={saveSettings.isPending} className="w-full">
          {saveSettings.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
          {t("setup.confirm")}
        </Button>
      </DialogContent>
    </Dialog>
  )
}
