import { useEffect, useMemo, useState } from "react"
import { useTranslation } from "react-i18next"
import { HandCoins, Loader2 } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { CarwashTicket, CarwashTipMode } from "@/domain/types/carwash"

/** Atajos en porcentaje del total. Son los redondeos con los que se maneja el mostrador. */
const QUICK_PERCENTS = [5, 10, 15, 20] as const

function roundMoney(value: number) {
  return Math.round(value * 100) / 100
}

interface CarwashTipDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  ticket: CarwashTicket | null
  tipMode: CarwashTipMode
  suggestedPercent: number
  isPending: boolean
  /** `null` = se entrega sin propina. El diálogo nunca entrega sin pasar por acá. */
  onConfirm: (tipAmount: number | null) => void
}

/**
 * Confirmación de entrega con registro de propina.
 *
 * IMPORTANTE: esto NO cobra nada. Carwash no procesa pagos — el servicio se paga en
 * efectivo en el mostrador — así que lo único que puede hacer el software es dejar
 * asentado lo que el cliente ya entregó en mano. Por eso el botón dice "entregar" y
 * no "cobrar", y por eso siempre hay una salida sin propina: forzar un monto haría
 * que el ranking de lavadores acumule plata que nadie recibió, que es exactamente el
 * dato que la función existe para medir.
 *
 * En modo `Suggested` el campo llega pre-cargado con el porcentaje configurado, pero
 * igual hay que confirmarlo: una sugerencia que se aplica sola es un aumento de precio.
 */
export function CarwashTipDialog({
  open,
  onOpenChange,
  ticket,
  tipMode,
  suggestedPercent,
  isPending,
  onConfirm,
}: CarwashTipDialogProps) {
  const { t, i18n } = useTranslation("carwash")
  const [amount, setAmount] = useState("")

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
      }),
    [i18n.language]
  )

  const total = ticket?.total ?? 0

  // Se re-siembra en cada apertura y no en el montaje: el diálogo es uno solo y se
  // reutiliza para todos los vehículos, así que el monto del anterior quedaría pegado.
  useEffect(() => {
    if (!open) return
    setAmount(
      tipMode === "Suggested" && total > 0
        ? String(roundMoney((total * suggestedPercent) / 100))
        : ""
    )
  }, [open, tipMode, suggestedPercent, total])

  const parsed = amount.trim() === "" ? null : Number(amount)
  const isInvalid = parsed !== null && (Number.isNaN(parsed) || parsed < 0)

  function applyPercent(percent: number) {
    setAmount(String(roundMoney((total * percent) / 100)))
  }

  function handleConfirm(tip: number | null) {
    if (isInvalid) return
    onConfirm(tip === null || tip <= 0 ? null : roundMoney(tip))
  }

  const washerName = ticket?.assignedToName

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <HandCoins className="h-5 w-5" />
            {t("tips.dialog.title")}
          </DialogTitle>
          <DialogDescription>
            {washerName
              ? t("tips.dialog.descriptionWasher", { name: washerName })
              : t("tips.dialog.description")}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="flex items-center justify-between rounded-lg border border-border/60 bg-muted/40 px-4 py-3">
            <span className="text-sm text-muted-foreground">{t("tips.dialog.serviceTotal")}</span>
            <span className="text-lg font-semibold">{currencyFormatter.format(total)}</span>
          </div>

          <div className="space-y-2">
            <Label>{t("tips.dialog.quickPicks")}</Label>
            <div className="flex flex-wrap gap-2">
              {QUICK_PERCENTS.map((percent) => (
                <Button
                  key={percent}
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={total <= 0}
                  onClick={() => applyPercent(percent)}
                >
                  {percent}%
                </Button>
              ))}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="carwash-tip-amount">{t("tips.dialog.amount")}</Label>
            <Input
              id="carwash-tip-amount"
              type="number"
              min={0}
              step="0.01"
              inputMode="decimal"
              autoFocus
              value={amount}
              placeholder={t("tips.dialog.amountPlaceholder")}
              onChange={(event) => setAmount(event.target.value)}
              className={cn(isInvalid && "border-destructive")}
            />
            {isInvalid ? (
              <p className="text-xs text-destructive">{t("tips.dialog.amountInvalid")}</p>
            ) : (
              <p className="text-xs text-muted-foreground">{t("tips.dialog.amountHint")}</p>
            )}
          </div>
        </div>

        <DialogFooter className="gap-2 sm:gap-2">
          {/* La salida sin propina es un botón visible y no un "cancelar": lo más común
              es que no haya propina, y esconderlo empuja a inventar un monto. */}
          <Button
            type="button"
            variant="ghost"
            disabled={isPending}
            onClick={() => handleConfirm(null)}
          >
            {t("tips.dialog.skip")}
          </Button>
          <Button type="button" disabled={isPending || isInvalid} onClick={() => handleConfirm(parsed)}>
            {isPending && <Loader2 className="h-4 w-4 animate-spin" />}
            {t("tips.dialog.confirm")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
