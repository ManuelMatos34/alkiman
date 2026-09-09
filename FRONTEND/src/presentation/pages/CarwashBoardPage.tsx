import { useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, ArrowRight, ArrowLeft, X, PhoneCall, Check, Timer, Loader2, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { CarwashRegisterTicketDialog } from "@/presentation/components/CarwashRegisterTicketDialog"
import { useCarwashQueue } from "@/application/carwash/useCarwashQueue"
import { useCarwashSettings } from "@/application/carwash/useCarwashSettings"
import { useCarwashWashers } from "@/application/carwash/useCarwashWashers"
import { useAssignWasher } from "@/application/carwash/useAssignWasher"
import { useAdvanceTicketStatus } from "@/application/carwash/useAdvanceTicketStatus"
import { useCancelTicket } from "@/application/carwash/useCancelTicket"
import { useCallArrival } from "@/application/carwash/useCallArrival"
import { useConfirmArrival } from "@/application/carwash/useConfirmArrival"
import { useExpireTicket } from "@/application/carwash/useExpireTicket"
import { useGoBackTicketStatus } from "@/application/carwash/useGoBackTicketStatus"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import { PermissionCodes } from "@/domain/types/permission"
import type { CarwashTicket, CarwashTicketStatus } from "@/domain/types/carwash"
import { cn } from "@/lib/utils"

/** Secciones del tablero en orden de flujo. Ready sale del tablero: va a Caja. */
const BOARD_COLUMNS: CarwashTicketStatus[] = ["Waiting", "InProgress", "Drying", "Waxing"]

/** Color del indicador por estado. */
const STATUS_COLOR: Record<CarwashTicketStatus, string> = {
  Waiting:       "bg-amber-400",
  InProgress:    "bg-blue-500",
  Drying:        "bg-cyan-500",
  Waxing:        "bg-violet-500",
  Ready:         "bg-emerald-500",
  ArrivalPending:"bg-primary",
  Delivered:     "bg-muted-foreground",
  Cancelled:     "bg-muted-foreground",
  Expired:       "bg-muted-foreground",
}

const UNASSIGNED = "__unassigned__"

function formatCountdown(deadline: string, now: number) {
  const diffMs = new Date(deadline).getTime() - now
  if (diffMs <= 0) return "00:00"
  const totalSeconds = Math.floor(diffMs / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`
}

function nextStatusFor(ticket: CarwashTicket): CarwashTicketStatus | null {
  switch (ticket.status) {
    case "Waiting":    return "InProgress"
    case "InProgress": return "Drying"
    case "Drying":     return ticket.extras.length > 0 ? "Waxing" : "Ready"
    case "Waxing":     return "Ready"
    case "Ready":      return "Delivered"
    default:           return null
  }
}

function alternateStatusFor(ticket: CarwashTicket): CarwashTicketStatus | null {
  if (ticket.status !== "Drying") return null
  return ticket.extras.length > 0 ? "Ready" : "Waxing"
}

function vehicleSummary(ticket: CarwashTicket) {
  const model = [ticket.vehicleBrand, ticket.vehicleModel, ticket.vehicleYear]
    .filter(Boolean).join(" ")
  return [model, ticket.vehicleColor].filter(Boolean).join(" · ")
}

// ─────────────────────────────────────────────────────────────────────────────
// Componente principal
// ─────────────────────────────────────────────────────────────────────────────

export function CarwashBoardPage() {
  const { t, i18n } = useTranslation("carwash")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.CarwashBoardManage)
  const canWork   = hasPermission(PermissionCodes.CarwashBoardWork)

  const { data: tickets, isLoading } = useCarwashQueue()
  const { data: settings }           = useCarwashSettings()
  const isSoloMode                   = settings?.operationMode === "Solitario"
  const { data: washers }            = useCarwashWashers(canManage && settings?.operationMode === "Empresa")

  const advanceStatus  = useAdvanceTicketStatus()
  const goBack         = useGoBackTicketStatus()
  const assignWasher   = useAssignWasher()
  const cancelTicket   = useCancelTicket()
  const callArrival    = useCallArrival()
  const confirmArrival = useConfirmArrival()
  const expireTicket   = useExpireTicket()

  const [registerOpen, setRegisterOpen] = useState(false)
  const [now, setNow]                   = useState(() => Date.now())

  const currencyFormatter = useMemo(
    () => new Intl.NumberFormat(getIntlLocale(i18n.language), { style: "currency", currency: "DOP" }),
    [i18n.language],
  )

  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(interval)
  }, [])

  const arrivalPendingTickets = tickets?.filter((t) => t.status === "ArrivalPending") ?? []
  const showWasherPicker      = !isSoloMode && canManage

  // ── Handlers ────────────────────────────────────────────────────────────

  function handleAdvance(ticket: CarwashTicket) {
    const next = nextStatusFor(ticket)
    // Ready -> Delivered ya no se maneja acá: lo hace la Cajera desde /caja
    if (!next || next === "Delivered") return
    advanceStatus.mutate(
      { id: ticket.id, status: next },
      { onError: () => toast.error(t("board.toast.advanceError")) },
    )
  }

  function handleAdvanceTo(ticket: CarwashTicket, status: CarwashTicketStatus) {
    if (status === "Delivered") return
    advanceStatus.mutate(
      { id: ticket.id, status },
      { onError: () => toast.error(t("board.toast.advanceError")) },
    )
  }

  function handleGoBack(ticket: CarwashTicket) {
    goBack.mutate(ticket.id, {
      onError: () => toast.error(t("board.toast.advanceError")),
    })
  }

  function handleAssign(ticket: CarwashTicket, value: string) {
    assignWasher.mutate(
      { id: ticket.id, washerId: value === UNASSIGNED ? null : value },
      {
        onSuccess: () => toast.success(t("board.toast.assignSuccess")),
        onError:   () => toast.error(t("board.toast.assignError")),
      },
    )
  }

  function handleCallArrival(ticket: CarwashTicket) {
    callArrival.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.callArrivalSuccess")),
      onError:   () => toast.error(t("board.toast.callArrivalError")),
    })
  }

  function handleCancel(ticket: CarwashTicket) {
    cancelTicket.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.cancelSuccess")),
      onError:   () => toast.error(t("board.toast.cancelError")),
    })
  }

  function handleConfirmArrival(ticket: CarwashTicket) {
    confirmArrival.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.confirmArrivalSuccess")),
      onError:   () => toast.error(t("board.toast.confirmArrivalError")),
    })
  }

  function handleExpire(ticket: CarwashTicket) {
    expireTicket.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.expireSuccess")),
      onError:   () => toast.error(t("board.toast.expireError")),
    })
  }

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Encabezado de página */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("board.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {isSoloMode ? t("board.subtitleSolo") : t("board.subtitle")}
          </p>
        </div>
        {canManage && (
          <Button onClick={() => setRegisterOpen(true)}>
            <Plus className="h-4 w-4" />
            {t("board.registerButton")}
          </Button>
        )}
      </div>

      {isLoading && (
        <div className="flex justify-center py-12">
          <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
        </div>
      )}

      {/* Turnos del portal esperando llegada */}
      {!isLoading && arrivalPendingTickets.length > 0 && (
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <div className={cn("h-2 w-2 rounded-full", STATUS_COLOR.ArrivalPending)} />
            <h2 className="text-sm font-semibold tracking-tight">
              {t("board.arrivalPending.title")}
            </h2>
            <Badge variant="outline" className="text-xs">{arrivalPendingTickets.length}</Badge>
          </div>

          <div className="overflow-hidden rounded-lg border">
            {arrivalPendingTickets.map((ticket, i) => (
              <div
                key={ticket.id}
                className={cn(
                  "flex flex-wrap items-center gap-x-4 gap-y-2 px-4 py-3 text-sm",
                  i !== arrivalPendingTickets.length - 1 && "border-b",
                )}
              >
                {/* Número + placa */}
                <div className="flex items-center gap-2 shrink-0">
                  <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                    {ticket.queueNumber}
                  </span>
                  <span className="font-mono font-semibold tracking-wide">{ticket.vehiclePlate}</span>
                </div>

                {/* Cliente */}
                <span className="font-medium">{ticket.customerName}</span>

                {/* Servicio */}
                <span className="text-muted-foreground">{ticket.serviceName}</span>

                {/* Countdown */}
                {ticket.arrivalDeadline && (
                  <div className="flex items-center gap-1 font-mono text-sm font-semibold text-primary">
                    <Timer className="h-3.5 w-3.5" />
                    {formatCountdown(ticket.arrivalDeadline, now)}
                  </div>
                )}

                {/* Acciones */}
                {canManage && (
                  <div className="ml-auto flex gap-2">
                    <Button
                      size="sm"
                      disabled={confirmArrival.isPending}
                      onClick={() => handleConfirmArrival(ticket)}
                    >
                      <Check className="h-3.5 w-3.5" />
                      {t("board.actions.confirmArrival")}
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={expireTicket.isPending}
                      onClick={() => handleExpire(ticket)}
                    >
                      <X className="h-3.5 w-3.5" />
                      {t("board.actions.markExpired")}
                    </Button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Secciones por estado */}
      {!isLoading && (
        <div className="space-y-3">
          {BOARD_COLUMNS.map((column) => {
            const columnTickets = tickets?.filter((t) => t.status === column) ?? []

            return (
              <div key={column} className="overflow-hidden rounded-lg border">
                {/* Cabecera de sección */}
                <div className="flex items-center gap-2.5 border-b bg-muted/40 px-4 py-2.5">
                  <div className={cn("h-2 w-2 rounded-full shrink-0", STATUS_COLOR[column])} />
                  <span className="text-sm font-semibold">
                    {t(`board.columns.${column}`)}
                  </span>
                  <Badge variant="outline" className="text-xs">{columnTickets.length}</Badge>
                </div>

                {/* Estado vacío */}
                {columnTickets.length === 0 && (
                  <p className="px-4 py-4 text-center text-xs text-muted-foreground">
                    {t("board.columnEmpty")}
                  </p>
                )}

                {/* Filas de tickets */}
                {columnTickets.map((ticket, i) => {
                  const next      = nextStatusFor(ticket)
                  const alternate = alternateStatusFor(ticket)
                  const summary   = vehicleSummary(ticket)
                  const needsWasher = column === "Waiting" && showWasherPicker && !ticket.assignedToWasherId

                  return (
                    <div
                      key={ticket.id}
                      className={cn(
                        "px-4 py-3 text-sm transition-colors hover:bg-muted/20",
                        i !== columnTickets.length - 1 && "border-b",
                      )}
                    >
                      {/* ── Fila principal ── */}
                      <div className="flex items-center gap-x-3 gap-y-1 flex-wrap">
                        {/* Número de turno */}
                        <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-bold">
                          {ticket.queueNumber}
                        </span>

                        {/* Placa */}
                        <span className="shrink-0 font-mono text-sm font-semibold tracking-wide">
                          {ticket.vehiclePlate}
                        </span>

                        {/* Cliente + vehículo */}
                        <div className="min-w-0 flex-1">
                          <span className="font-medium">{ticket.customerName}</span>
                          {summary && (
                            <span className="ml-2 hidden text-xs text-muted-foreground sm:inline">{summary}</span>
                          )}
                        </div>

                        {/* Precio */}
                        <span className="shrink-0 font-semibold">
                          {currencyFormatter.format(ticket.total)}
                        </span>

                        {/* Acciones */}
                        <div className="flex shrink-0 items-center gap-1">
                          {canManage && column === "Waiting" && ticket.source === "Portal" && (
                            <Button
                              size="sm"
                              variant="outline"
                              disabled={callArrival.isPending}
                              onClick={() => handleCallArrival(ticket)}
                              className="px-2 sm:px-3"
                            >
                              <PhoneCall className="h-3.5 w-3.5" />
                              <span className="hidden sm:inline">{t("board.actions.callArrival")}</span>
                            </Button>
                          )}

                          {canWork && alternate && (
                            <Button
                              size="sm"
                              variant="outline"
                              disabled={advanceStatus.isPending}
                              onClick={() => handleAdvanceTo(ticket, alternate)}
                            >
                              {t(`board.actions.advanceTo.${alternate}`)}
                            </Button>
                          )}

                          {/* Retroceder estado */}
                          {canWork && column !== "Waiting" && (
                            <Button
                              size="sm"
                              variant="outline"
                              disabled={goBack.isPending}
                              onClick={() => handleGoBack(ticket)}
                              title={t("board.actions.goBack")}
                              className="px-2"
                            >
                              <ArrowLeft className="h-3.5 w-3.5" />
                            </Button>
                          )}

                          {canWork && next && (
                            <Button
                              size="sm"
                              disabled={advanceStatus.isPending || needsWasher}
                              title={needsWasher ? t("board.washer.requiredToAdvance") : undefined}
                              onClick={() => handleAdvance(ticket)}
                            >
                              <ArrowRight className="h-3.5 w-3.5" />
                              <span className="hidden sm:inline">
                                {t(`board.actions.advanceTo.${next}`)}
                              </span>
                            </Button>
                          )}

                          {canManage && (
                            <Button
                              size="sm"
                              variant="ghost"
                              disabled={cancelTicket.isPending}
                              onClick={() => handleCancel(ticket)}
                              className="px-2"
                            >
                              <X className="h-3.5 w-3.5 text-destructive" />
                            </Button>
                          )}
                        </div>
                      </div>

                      {/* ── Fila secundaria: servicio + extras + lavador ── */}
                      <div className="mt-1.5 flex flex-wrap items-center gap-2 pl-9">
                        {/* Servicio + extras */}
                        <span className="text-xs text-muted-foreground">{ticket.serviceName}</span>
                        {ticket.extras.map((extra) => (
                          <Badge key={extra.extraId} variant="outline" className="gap-1 px-1.5 py-0 text-xs font-normal">
                            <Sparkles className="h-2.5 w-2.5" />
                            {extra.name}
                          </Badge>
                        ))}

                        {/* Lavador: editable en Waiting */}
                        {showWasherPicker && column === "Waiting" && (
                          <Select
                            value={ticket.assignedToWasherId ?? UNASSIGNED}
                            onValueChange={(value) => handleAssign(ticket, value)}
                            disabled={assignWasher.isPending}
                          >
                            <SelectTrigger size="sm" className="h-7 w-40 text-xs">
                              <SelectValue placeholder={t("board.washer.placeholder")} />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value={UNASSIGNED}>
                                {t("board.washer.unassigned")}
                              </SelectItem>
                              {washers
                                ?.filter((w) => w.isActive || w.id === ticket.assignedToWasherId)
                                .map((washer) => (
                                  <SelectItem key={washer.id} value={washer.id}>
                                    {washer.fullName}
                                    {!washer.isActive && ` (${t("washers.table.statusInactive")})`}
                                  </SelectItem>
                                ))}
                            </SelectContent>
                          </Select>
                        )}

                        {/* Lavador asignado — lectura en otros estados */}
                        {showWasherPicker && column !== "Waiting" && ticket.assignedToName && (
                          <span className="text-xs text-muted-foreground">
                            👤 {ticket.assignedToName}
                          </span>
                        )}

                        {/* Lavador (modo Solitario) */}
                        {isSoloMode && ticket.assignedToName && (
                          <span className="text-xs text-muted-foreground">
                            👤 {ticket.assignedToName}
                          </span>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            )
          })}
        </div>
      )}

      <CarwashRegisterTicketDialog open={registerOpen} onOpenChange={setRegisterOpen} />
    </div>
  )
}
