import { useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, ArrowRight, X, PhoneCall, Check, Timer, Loader2, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
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
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import { PermissionCodes } from "@/domain/types/permission"
import type { CarwashTicket, CarwashTicketStatus } from "@/domain/types/carwash"

/** Columnas del tablero (Delivered/Cancelled/Expired quedan fuera: ya no requieren acción). */
const BOARD_COLUMNS: CarwashTicketStatus[] = ["Waiting", "InProgress", "Drying", "Waxing", "Ready"]

/** Valor centinela del combo de lavador: Select de Radix no admite un item con value="". */
const UNASSIGNED = "__unassigned__"

function formatCountdown(deadline: string, now: number) {
  const diffMs = new Date(deadline).getTime() - now
  if (diffMs <= 0) return "00:00"
  const totalSeconds = Math.floor(diffMs / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`
}

/**
 * Encerado es un paso OPCIONAL. Desde Secado siempre se ofrecen las dos salidas (encerar o cerrar):
 * los extras solo deciden cuál va como botón principal, porque el catálogo no distingue un extra de
 * encerado de uno que no lo es y adivinarlo dejaría al encargado sin la otra opción.
 */
function nextStatusFor(ticket: CarwashTicket): CarwashTicketStatus | null {
  switch (ticket.status) {
    case "Waiting":
      return "InProgress"
    case "InProgress":
      return "Drying"
    case "Drying":
      return ticket.extras.length > 0 ? "Waxing" : "Ready"
    case "Waxing":
      return "Ready"
    case "Ready":
      return "Delivered"
    default:
      return null
  }
}

/** La salida secundaria de Secado: la que no quedó como botón principal. */
function alternateStatusFor(ticket: CarwashTicket): CarwashTicketStatus | null {
  if (ticket.status !== "Drying") return null
  return ticket.extras.length > 0 ? "Ready" : "Waxing"
}

/** Resumen legible del vehículo: "Toyota Corolla 2020 · Gris", omitiendo lo que no se cargó. */
function vehicleSummary(ticket: CarwashTicket) {
  const model = [ticket.vehicleBrand, ticket.vehicleModel, ticket.vehicleYear]
    .filter(Boolean)
    .join(" ")
  return [model, ticket.vehicleColor].filter(Boolean).join(" · ")
}

/** Tablero de la cola de Carwash: kanban por estado + sección de turnos llamados (portal). */
export function CarwashBoardPage() {
  const { t, i18n } = useTranslation("carwash")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.CarwashManage)
  const canWork = hasPermission(PermissionCodes.CarwashWork)

  const { data: tickets, isLoading } = useCarwashQueue()
  const { data: settings } = useCarwashSettings()
  const isSoloMode = settings?.operationMode === "Solitario"
  // En modo Solitario no hay a quién asignar: el ticket se auto-asigna al iniciar el lavado.
  const { data: washers } = useCarwashWashers(canManage && settings?.operationMode === "Empresa")

  const advanceStatus = useAdvanceTicketStatus()
  const assignWasher = useAssignWasher()
  const cancelTicket = useCancelTicket()
  const callArrival = useCallArrival()
  const confirmArrival = useConfirmArrival()
  const expireTicket = useExpireTicket()

  const [registerOpen, setRegisterOpen] = useState(false)
  const [now, setNow] = useState(() => Date.now())

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), { style: "currency", currency: "DOP" }),
    [i18n.language]
  )

  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(interval)
  }, [])

  const arrivalPendingTickets = tickets?.filter((ticket) => ticket.status === "ArrivalPending") ?? []
  const showWasherPicker = !isSoloMode && canManage

  function handleAdvance(ticket: CarwashTicket) {
    const next = nextStatusFor(ticket)
    if (!next) return
    advanceStatus.mutate(
      { id: ticket.id, status: next },
      { onError: () => toast.error(t("board.toast.advanceError")) }
    )
  }

  function handleAdvanceTo(ticket: CarwashTicket, status: CarwashTicketStatus) {
    advanceStatus.mutate(
      { id: ticket.id, status },
      { onError: () => toast.error(t("board.toast.advanceError")) }
    )
  }

  function handleAssign(ticket: CarwashTicket, value: string) {
    assignWasher.mutate(
      { id: ticket.id, washerId: value === UNASSIGNED ? null : value },
      {
        onSuccess: () => toast.success(t("board.toast.assignSuccess")),
        onError: () => toast.error(t("board.toast.assignError")),
      }
    )
  }

  function handleCallArrival(ticket: CarwashTicket) {
    callArrival.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.callArrivalSuccess")),
      onError: () => toast.error(t("board.toast.callArrivalError")),
    })
  }

  function handleCancel(ticket: CarwashTicket) {
    cancelTicket.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.cancelSuccess")),
      onError: () => toast.error(t("board.toast.cancelError")),
    })
  }

  function handleConfirmArrival(ticket: CarwashTicket) {
    confirmArrival.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.confirmArrivalSuccess")),
      onError: () => toast.error(t("board.toast.confirmArrivalError")),
    })
  }

  function handleExpire(ticket: CarwashTicket) {
    expireTicket.mutate(ticket.id, {
      onSuccess: () => toast.success(t("board.toast.expireSuccess")),
      onError: () => toast.error(t("board.toast.expireError")),
    })
  }

  return (
    <div className="space-y-6">
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

      {!isLoading && arrivalPendingTickets.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-lg font-semibold tracking-tight">{t("board.arrivalPending.title")}</h2>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {arrivalPendingTickets.map((ticket) => (
              <Card key={ticket.id} className="border-primary/40">
                <CardHeader className="pb-2">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-base">{ticket.vehiclePlate}</CardTitle>
                    <Badge variant="secondary">
                      {t("board.queueNumber", { number: ticket.queueNumber })}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <div>
                    <p className="font-medium">{ticket.customerName}</p>
                    <p className="text-muted-foreground">{ticket.serviceName}</p>
                  </div>
                  {ticket.arrivalDeadline && (
                    <div className="flex items-center gap-1.5 font-mono text-base font-semibold">
                      <Timer className="h-4 w-4 text-muted-foreground" />
                      {formatCountdown(ticket.arrivalDeadline, now)}
                    </div>
                  )}
                  {canManage && (
                    <div className="flex gap-2 pt-1">
                      <Button
                        size="sm"
                        className="flex-1"
                        disabled={confirmArrival.isPending}
                        onClick={() => handleConfirmArrival(ticket)}
                      >
                        <Check className="h-4 w-4" />
                        {t("board.actions.confirmArrival")}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="flex-1"
                        disabled={expireTicket.isPending}
                        onClick={() => handleExpire(ticket)}
                      >
                        <X className="h-4 w-4" />
                        {t("board.actions.markExpired")}
                      </Button>
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}

      {!isLoading && (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
          {BOARD_COLUMNS.map((column) => {
            const columnTickets = tickets?.filter((ticket) => ticket.status === column) ?? []
            return (
              <div key={column} className="space-y-3">
                <div className="flex items-center justify-between">
                  <h2 className="text-sm font-semibold tracking-tight text-muted-foreground">
                    {t(`board.columns.${column}`)}
                  </h2>
                  <Badge variant="outline">{columnTickets.length}</Badge>
                </div>

                <div className="space-y-3">
                  {columnTickets.length === 0 && (
                    <p className="rounded-lg border border-dashed border-border/60 p-4 text-center text-xs text-muted-foreground">
                      {t("board.columnEmpty")}
                    </p>
                  )}

                  {columnTickets.map((ticket) => {
                    const next = nextStatusFor(ticket)
                    const alternate = alternateStatusFor(ticket)
                    const summary = vehicleSummary(ticket)
                    return (
                      <Card key={ticket.id}>
                        <CardHeader className="pb-2">
                          <div className="flex items-center justify-between">
                            <CardTitle className="text-base">{ticket.vehiclePlate}</CardTitle>
                            <Badge variant="secondary">
                              {t("board.queueNumber", { number: ticket.queueNumber })}
                            </Badge>
                          </div>
                        </CardHeader>
                        <CardContent className="space-y-3 text-sm">
                          <div>
                            <p className="font-medium">{ticket.customerName}</p>
                            {summary && <p className="text-muted-foreground">{summary}</p>}
                            <p className="text-muted-foreground">{ticket.serviceName}</p>
                          </div>

                          {ticket.extras.length > 0 && (
                            <div className="flex flex-wrap gap-1">
                              {ticket.extras.map((extra) => (
                                <Badge
                                  key={extra.extraId}
                                  variant="outline"
                                  className="gap-1 font-normal"
                                >
                                  <Sparkles className="h-3 w-3" />
                                  {extra.name}
                                </Badge>
                              ))}
                            </div>
                          )}

                          <p className="font-semibold">{currencyFormatter.format(ticket.total)}</p>

                          {showWasherPicker && (
                            <Select
                              value={ticket.assignedToWasherId ?? UNASSIGNED}
                              onValueChange={(value) => handleAssign(ticket, value)}
                              disabled={assignWasher.isPending}
                            >
                              <SelectTrigger size="sm" className="w-full">
                                <SelectValue placeholder={t("board.washer.placeholder")} />
                              </SelectTrigger>
                              <SelectContent>
                                <SelectItem value={UNASSIGNED}>
                                  {t("board.washer.unassigned")}
                                </SelectItem>
                                {/*
                                  Sólo lavadores activos, más el ya asignado aunque
                                  esté dado de baja: si no estuviera en la lista, el
                                  Select no encontraría su value y la tarjeta se
                                  mostraría como "sin asignar".
                                */}
                                {washers
                                  ?.filter(
                                    (washer) =>
                                      washer.isActive || washer.id === ticket.assignedToWasherId
                                  )
                                  .map((washer) => (
                                    <SelectItem key={washer.id} value={washer.id}>
                                      {washer.fullName}
                                      {!washer.isActive && ` (${t("washers.table.statusInactive")})`}
                                    </SelectItem>
                                  ))}
                              </SelectContent>
                            </Select>
                          )}

                          {isSoloMode && ticket.assignedToName && (
                            <p className="text-xs text-muted-foreground">
                              {t("board.washer.assignedTo", { name: ticket.assignedToName })}
                            </p>
                          )}

                          <div className="flex flex-wrap gap-2 pt-1">
                            {canManage && column === "Waiting" && ticket.source === "Portal" && (
                              <Button
                                size="sm"
                                variant="outline"
                                disabled={callArrival.isPending}
                                onClick={() => handleCallArrival(ticket)}
                              >
                                <PhoneCall className="h-4 w-4" />
                                {t("board.actions.callArrival")}
                              </Button>
                            )}

                            {canWork && next && (
                              <Button
                                size="sm"
                                disabled={advanceStatus.isPending}
                                onClick={() => handleAdvance(ticket)}
                              >
                                <ArrowRight className="h-4 w-4" />
                                {t(`board.actions.advanceTo.${next}`)}
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

                            {canManage && (
                              <Button
                                size="sm"
                                variant="ghost"
                                disabled={cancelTicket.isPending}
                                onClick={() => handleCancel(ticket)}
                              >
                                <X className="h-4 w-4 text-destructive" />
                                {t("board.actions.cancel")}
                              </Button>
                            )}
                          </div>
                        </CardContent>
                      </Card>
                    )
                  })}
                </div>
              </div>
            )
          })}
        </div>
      )}

      <CarwashRegisterTicketDialog open={registerOpen} onOpenChange={setRegisterOpen} />
    </div>
  )
}
