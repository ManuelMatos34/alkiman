import { useMemo, useState } from "react"
import { useTranslation } from "react-i18next"
import { toast } from "sonner"
import { CheckCircle2, Car, CreditCard, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog"
import { cn } from "@/lib/utils"
import { useCarwashQueue } from "@/application/carwash/useCarwashQueue"
import { useCarwashSettings } from "@/application/carwash/useCarwashSettings"
import { useDeliverTicket } from "@/application/carwash/useDeliverTicket"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { CarwashTicket } from "@/domain/types/carwash"

// ─────────────────────────────────────────────────────────────────────────────
// Tip quick-pick amounts (fixed + suggested percentage slot)
// ─────────────────────────────────────────────────────────────────────────────

const FIXED_TIPS = [50, 100, 150, 200, 300]

// ─────────────────────────────────────────────────────────────────────────────
// Modal de entrega
// ─────────────────────────────────────────────────────────────────────────────

interface DeliverModalProps {
  ticket: CarwashTicket
  tipMode: string
  tipSuggestedPercent: number
  currencyFormatter: Intl.NumberFormat
  onClose: () => void
}

function DeliverModal({
  ticket,
  tipMode,
  tipSuggestedPercent,
  currencyFormatter,
  onClose,
}: DeliverModalProps) {
  const { t } = useTranslation("carwash")
  const deliver = useDeliverTicket()

  const isPortal = ticket.source === "Portal"
  const showTip  = !isPortal && tipMode !== "Disabled"

  // suggested amount based on percentage
  const suggestedAmount = Math.round((ticket.total * tipSuggestedPercent) / 100 / 50) * 50

  // selected quick-pick (null = none selected, "custom" = manual)
  const [selected, setSelected] = useState<number | "custom" | null>(
    tipMode === "Suggested" && suggestedAmount > 0 ? suggestedAmount : null
  )
  const [customInput, setCustomInput] = useState("")

  const tipAmount: number | null = (() => {
    if (!showTip || selected === null) return null
    if (selected === "custom") {
      const v = parseFloat(customInput)
      return isNaN(v) || v < 0 ? null : v
    }
    return selected
  })()

  // Para portal: el total ya incluye la propina prepagada
  const grandTotal = isPortal
    ? ticket.total + (ticket.tipAmount ?? 0)
    : ticket.total + (tipAmount ?? 0)

  function handleDeliver() {
    deliver.mutate(
      { id: ticket.id, tipAmount },
      {
        onSuccess: () => {
          toast.success(t("caja.toast.success"))
          onClose()
        },
        onError: () => toast.error(t("caja.toast.error")),
      }
    )
  }

  const vehicleSummary = [ticket.vehicleBrand, ticket.vehicleModel, ticket.vehicleYear, ticket.vehicleColor]
    .filter(Boolean).join(" · ")

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <CheckCircle2 className="h-5 w-5 text-emerald-500" />
            {t("caja.modal.title")}
          </DialogTitle>
        </DialogHeader>

        {/* Resumen del ticket */}
        <div className="space-y-2 rounded-lg border bg-muted/30 p-4 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t("caja.modal.queue")}</span>
            <span className="font-bold">#{ticket.queueNumber}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t("caja.modal.plate")}</span>
            <span className="font-mono font-semibold tracking-wider">{ticket.vehiclePlate}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t("caja.modal.customer")}</span>
            <span className="font-medium">{ticket.customerName}</span>
          </div>
          {vehicleSummary && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t("caja.modal.vehicle")}</span>
              <span>{vehicleSummary}</span>
            </div>
          )}

          <hr className="border-border" />

          <div className="flex justify-between">
            <span className="text-muted-foreground">{ticket.serviceName}</span>
            <span>{currencyFormatter.format(ticket.servicePrice)}</span>
          </div>
          {ticket.extras.map((e) => (
            <div key={e.extraId} className="flex justify-between">
              <span className="flex items-center gap-1 text-muted-foreground">
                <Sparkles className="h-3 w-3" /> {e.name}
              </span>
              <span>{currencyFormatter.format(e.price)}</span>
            </div>
          ))}

          <hr className="border-border" />

          <div className="flex justify-between font-semibold">
            <span>{t("caja.modal.subtotal")}</span>
            <span>{currencyFormatter.format(ticket.total)}</span>
          </div>

          {/* Propina prepagada (portal) — solo lectura */}
          {isPortal && (ticket.tipAmount ?? 0) > 0 && (
            <div className="flex justify-between text-emerald-600 dark:text-emerald-400">
              <span>{t("caja.modal.tip")}</span>
              <span>+ {currencyFormatter.format(ticket.tipAmount!)}</span>
            </div>
          )}

          {/* Propina elegida en caja (presencial) */}
          {showTip && tipAmount !== null && (
            <div className="flex justify-between text-emerald-600 dark:text-emerald-400">
              <span>{t("caja.modal.tip")}</span>
              <span>+ {currencyFormatter.format(tipAmount)}</span>
            </div>
          )}

          <div className="flex justify-between text-base font-bold">
            <span>{isPortal ? t("caja.modal.totalPaid") : t("caja.modal.total")}</span>
            <span>{currencyFormatter.format(grandTotal)}</span>
          </div>

          {isPortal && (
            <div className="flex items-center gap-2 rounded-md border border-emerald-500/40 bg-emerald-500/10 px-3 py-2.5 text-sm font-medium text-emerald-700 dark:border-emerald-400/30 dark:bg-emerald-400/10 dark:text-emerald-400">
              <CreditCard className="h-4 w-4 shrink-0 text-emerald-500" />
              <span>{t("caja.modal.prepaid")}</span>
            </div>
          )}
        </div>

        {/* Quick-picks de propina */}
        {showTip && (
          <div className="space-y-2">
            <p className="text-sm font-medium">{t("tips.dialog.amount")}</p>

            <div className="flex flex-wrap gap-2">
              {/* Ninguna propina */}
              <button
                type="button"
                onClick={() => setSelected(null)}
                className={cn(
                  "rounded-md border px-3 py-1.5 text-sm transition-colors",
                  selected === null
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-background hover:bg-muted"
                )}
              >
                {t("caja.noTip")}
              </button>

              {/* Suggested (si aplica y es diferente a los fixed) */}
              {tipMode === "Suggested" && suggestedAmount > 0 && !FIXED_TIPS.includes(suggestedAmount) && (
                <button
                  type="button"
                  onClick={() => setSelected(suggestedAmount)}
                  className={cn(
                    "rounded-md border px-3 py-1.5 text-sm transition-colors",
                    selected === suggestedAmount
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-emerald-500 text-emerald-600 hover:bg-emerald-50 dark:text-emerald-400"
                  )}
                >
                  {currencyFormatter.format(suggestedAmount)} <span className="text-xs opacity-70">({tipSuggestedPercent}%)</span>
                </button>
              )}

              {/* Fixed amounts */}
              {FIXED_TIPS.map((amount) => (
                <button
                  key={amount}
                  type="button"
                  onClick={() => setSelected(amount)}
                  className={cn(
                    "rounded-md border px-3 py-1.5 text-sm transition-colors",
                    selected === amount
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-border bg-background hover:bg-muted"
                  )}
                >
                  {currencyFormatter.format(amount)}
                </button>
              ))}

              {/* Custom */}
              <button
                type="button"
                onClick={() => setSelected("custom")}
                className={cn(
                  "rounded-md border px-3 py-1.5 text-sm transition-colors",
                  selected === "custom"
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-background hover:bg-muted"
                )}
              >
                {t("caja.customTip")}
              </button>
            </div>

            {selected === "custom" && (
              <Input
                autoFocus
                type="number"
                min="0"
                step="1"
                placeholder={t("tips.dialog.amountPlaceholder")}
                value={customInput}
                onChange={(e) => setCustomInput(e.target.value)}
                className="mt-1"
              />
            )}
          </div>
        )}

        <DialogFooter>
          <Button
            className="w-full gap-2"
            disabled={deliver.isPending}
            onClick={handleDeliver}
          >
            <CheckCircle2 className="h-4 w-4" />
            {deliver.isPending ? t("caja.delivering") : t("caja.modal.confirm")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Fila de un ticket listo
// ─────────────────────────────────────────────────────────────────────────────

interface ReadyTicketRowProps {
  ticket: CarwashTicket
  currencyFormatter: Intl.NumberFormat
  onDeliver: () => void
}

function ReadyTicketRow({ ticket, currencyFormatter, onDeliver }: ReadyTicketRowProps) {
  const { t } = useTranslation("carwash")
  const isPortal = ticket.source === "Portal"

  const vehicleSummary = [ticket.vehicleBrand, ticket.vehicleModel, ticket.vehicleColor]
    .filter(Boolean).join(" · ")

  return (
    <div className="flex flex-col gap-4 rounded-lg border bg-card p-4 sm:flex-row sm:items-center sm:gap-6">
      <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-emerald-500/10 text-lg font-bold text-emerald-600 dark:text-emerald-400">
        {ticket.queueNumber}
      </div>

      <div className="min-w-0 flex-1 space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-mono text-sm font-semibold tracking-wider">{ticket.vehiclePlate}</span>
          {vehicleSummary && (
            <span className="text-xs text-muted-foreground">{vehicleSummary}</span>
          )}
          {isPortal && (
            <Badge variant="outline" className="gap-1 border-emerald-500/40 text-emerald-600 dark:text-emerald-400">
              <CreditCard className="h-3 w-3" />
              {t("caja.portal")}
            </Badge>
          )}
        </div>
        <p className="font-medium">{ticket.customerName}</p>
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="text-sm text-muted-foreground">{ticket.serviceName}</span>
          {ticket.extras.map((e) => (
            <Badge key={e.extraId} variant="outline" className="gap-1 px-1.5 py-0 text-xs font-normal">
              <Sparkles className="h-2.5 w-2.5" />{e.name}
            </Badge>
          ))}
        </div>
      </div>

      <div className="flex shrink-0 items-center gap-3 sm:flex-col sm:items-end sm:gap-1.5">
        <span className="text-base font-semibold">{currencyFormatter.format(ticket.total)}</span>
        <Button size="sm" className="gap-1.5 whitespace-nowrap" onClick={onDeliver}>
          <CheckCircle2 className="h-4 w-4" />
          {t("caja.deliver")}
        </Button>
      </div>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Página de Caja
// ─────────────────────────────────────────────────────────────────────────────

export function CarwashCajaPage() {
  const { t, i18n } = useTranslation("carwash")
  const { data: queue = [] } = useCarwashQueue()
  const { data: settings }   = useCarwashSettings()

  const tipMode             = settings?.tipMode ?? "Optional"
  const tipSuggestedPercent = settings?.tipSuggestedPercent ?? 10

  const [activeTicket, setActiveTicket] = useState<CarwashTicket | null>(null)

  const readyTickets = useMemo(() => queue.filter((t) => t.status === "Ready"), [queue])

  const currencyFormatter = useMemo(
    () => new Intl.NumberFormat(getIntlLocale(i18n.language), { style: "currency", currency: "DOP" }),
    [i18n.language]
  )

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <div>
        <h1 className="text-2xl font-bold">{t("caja.title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("caja.subtitle")}</p>
      </div>

      {readyTickets.length > 0 && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Car className="h-4 w-4" />
          <span>{readyTickets.length} {readyTickets.length === 1 ? "vehículo listo" : "vehículos listos"}</span>
        </div>
      )}

      {readyTickets.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed py-20 text-center text-muted-foreground">
          <CheckCircle2 className="h-10 w-10 opacity-30" />
          <p className="text-sm">{t("caja.empty")}</p>
        </div>
      ) : (
        <div className="space-y-3">
          {readyTickets.map((ticket) => (
            <ReadyTicketRow
              key={ticket.id}
              ticket={ticket}
              currencyFormatter={currencyFormatter}
              onDeliver={() => setActiveTicket(ticket)}
            />
          ))}
        </div>
      )}

      {activeTicket && (
        <DeliverModal
          ticket={activeTicket}
          tipMode={tipMode}
          tipSuggestedPercent={tipSuggestedPercent}
          currencyFormatter={currencyFormatter}
          onClose={() => setActiveTicket(null)}
        />
      )}
    </div>
  )
}
