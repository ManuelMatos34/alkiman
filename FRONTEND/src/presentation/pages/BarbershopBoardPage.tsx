import { useMemo, useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, X, Loader2, CalendarDays, LayoutGrid, DollarSign } from "lucide-react"
import FullCalendar from "@fullcalendar/react"
import dayGridPlugin from "@fullcalendar/daygrid"
import timeGridPlugin from "@fullcalendar/timegrid"
import interactionPlugin from "@fullcalendar/interaction"
import esLocale from "@fullcalendar/core/locales/es"
import type { DatesSetArg } from "@fullcalendar/core"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { Label } from "@/components/ui/label"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import "@/presentation/styles/fullcalendar-theme.css"
import { BarbershopAppointmentFormDialog } from "@/presentation/components/BarbershopAppointmentFormDialog"
import {
  useBarbershopAppointments,
  useBarbershopAppointmentsRange,
  useAdvanceBarbershopAppointment,
  useCancelBarbershopAppointment,
  useMarkBarbershopAppointmentPaid,
} from "@/application/barbershop/useBarbershopAppointments"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import { cn } from "@/lib/utils"
import type { BarbershopAppointment, BarbershopAppointmentStatus } from "@/domain/types/barbershop"

type BoardStatus = 'Scheduled' | 'Confirmed' | 'InProgress' | 'Completed'
const BOARD_COLUMNS: BoardStatus[] = ['Scheduled', 'Confirmed', 'InProgress', 'Completed']

const STATUS_COLOR: Record<BoardStatus, string> = {
  Scheduled:  "bg-blue-400",
  Confirmed:  "bg-amber-400",
  InProgress: "bg-violet-500",
  Completed:  "bg-emerald-500",
}

// FullCalendar event hex colors
const STATUS_HEX: Record<BarbershopAppointmentStatus, string> = {
  Scheduled:  "#60a5fa",
  Confirmed:  "#fbbf24",
  InProgress: "#8b5cf6",
  Completed:  "#10b981",
  Cancelled:  "#94a3b8",
}

function toDateInputValue(date: Date) {
  const month = String(date.getMonth() + 1).padStart(2, "0")
  const day = String(date.getDate()).padStart(2, "0")
  return `${date.getFullYear()}-${month}-${day}`
}

function getMonthRange(date: Date): { from: string; to: string } {
  const start = new Date(date.getFullYear(), date.getMonth(), 1)
  const end = new Date(date.getFullYear(), date.getMonth() + 1, 0)
  return { from: toDateInputValue(start), to: toDateInputValue(end) }
}

function nextStatusFor(status: BarbershopAppointmentStatus): BarbershopAppointmentStatus | null {
  switch (status) {
    case 'Scheduled':  return 'Confirmed'
    case 'Confirmed':  return 'InProgress'
    case 'InProgress': return 'Completed'
    default:           return null
  }
}

export function BarbershopBoardPage() {
  const { t, i18n } = useTranslation("barbershop")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.BarbershopBoardManage)
  const canWork   = hasPermission(PermissionCodes.BarbershopBoardWork)

  // Board view state
  const [boardView, setBoardView] = useState<'board' | 'calendar'>('board')
  const [date, setDate] = useState(() => toDateInputValue(new Date()))

  // Calendar range state — default to current month
  const [calendarRange, setCalendarRange] = useState<{ from: string; to: string }>(
    () => getMonthRange(new Date())
  )

  const [formOpen, setFormOpen] = useState(false)
  const [cancelTarget, setCancelTarget] = useState<BarbershopAppointment | null>(null)
  const [completeTarget, setCompleteTarget] = useState<BarbershopAppointment | null>(null)
  const [isPaid, setIsPaid] = useState(false)

  // Board data (by selected date)
  const { data: boardAppointments, isLoading: boardLoading } = useBarbershopAppointments(date)

  // Calendar data (by visible month range)
  const { data: calendarAppointments, isLoading: calendarLoading } = useBarbershopAppointmentsRange(
    calendarRange.from,
    calendarRange.to
  )

  const advanceStatus = useAdvanceBarbershopAppointment()
  const markAsPaid = useMarkBarbershopAppointmentPaid()
  const cancelAppointment = useCancelBarbershopAppointment()

  const timeFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        hour: "2-digit",
        minute: "2-digit",
      }),
    [i18n.language]
  )

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "USD",
        minimumFractionDigits: 2,
      }),
    [i18n.language]
  )

  function formatTime(iso: string) {
    return timeFormatter.format(new Date(iso))
  }

  function formatPrice(price: number | null) {
    if (price == null) return null
    return currencyFormatter.format(price)
  }

  function handleAdvance(appointment: BarbershopAppointment) {
    const next = nextStatusFor(appointment.status)
    if (!next) return

    // Completing an InProgress appointment — ask about payment first
    if (appointment.status === 'InProgress') {
      setIsPaid(false)
      setCompleteTarget(appointment)
      return
    }

    advanceStatus.mutate(
      { id: appointment.id, isPaid: false },
      { onError: () => toast.error(t("toast.updateError")) }
    )
  }

  function handleConfirmComplete() {
    if (!completeTarget) return
    advanceStatus.mutate(
      { id: completeTarget.id, isPaid },
      {
        onSuccess: () => toast.success(t("toast.updateSuccess")),
        onError: () => toast.error(t("toast.updateError")),
      }
    )
    setCompleteTarget(null)
    setIsPaid(false)
  }

  function handleConfirmCancel() {
    if (!cancelTarget) return
    cancelAppointment.mutate(cancelTarget.id, {
      onSuccess: () => toast.success(t("toast.cancelSuccess")),
      onError: () => toast.error(t("toast.cancelError")),
    })
    setCancelTarget(null)
  }

  function handleDatesSet(arg: DatesSetArg) {
    // FullCalendar fires this when the visible range changes
    const from = toDateInputValue(arg.start)
    // arg.end is exclusive, subtract 1 day for our inclusive `to`
    const toDate = new Date(arg.end)
    toDate.setDate(toDate.getDate() - 1)
    const to = toDateInputValue(toDate)
    setCalendarRange({ from, to })
  }

  // Build FullCalendar events from calendar appointments
  const calendarEvents = useMemo(() => {
    return (calendarAppointments ?? []).map((a) => {
      const start = new Date(a.scheduledAt)
      const end = new Date(start.getTime() + 30 * 60 * 1000) // 30 min default
      return {
        id: a.id,
        title: `${a.clientName}${a.serviceName ? ` · ${a.serviceName}` : ""}`,
        start,
        end,
        color: STATUS_HEX[a.status],
        extendedProps: { appointment: a },
      }
    })
  }, [calendarAppointments])

  const allAppointments = boardAppointments ?? []
  const isLoading = boardView === 'board' ? boardLoading : calendarLoading

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("board.title")}</h1>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          {/* Date picker — only in board view */}
          {boardView === 'board' && (
            <Input
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-40"
            />
          )}

          {/* View toggle */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => setBoardView(boardView === 'board' ? 'calendar' : 'board')}
            className="gap-1.5"
          >
            {boardView === 'board' ? (
              <>
                <CalendarDays className="h-4 w-4" />
                {t("board.viewCalendar")}
              </>
            ) : (
              <>
                <LayoutGrid className="h-4 w-4" />
                {t("board.viewBoard")}
              </>
            )}
          </Button>

          {canManage && (
            <Button onClick={() => setFormOpen(true)} size="sm">
              <Plus className="h-4 w-4" />
              {t("board.newAppointment")}
            </Button>
          )}
        </div>
      </div>

      {isLoading && (
        <div className="flex justify-center py-12">
          <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
        </div>
      )}

      {/* ===== BOARD VIEW ===== */}
      {!isLoading && boardView === 'board' && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {BOARD_COLUMNS.map((column) => {
            const columnItems = allAppointments.filter((a) => a.status === column)

            return (
              <div key={column} className="space-y-2">
                <div className="flex items-center gap-2">
                  <div className={cn("h-2 w-2 rounded-full shrink-0", STATUS_COLOR[column])} />
                  <span className="text-sm font-semibold">
                    {t(`board.columns.${column.toLowerCase() as Lowercase<BoardStatus>}`)}
                  </span>
                  <Badge variant="outline" className="text-xs ml-auto">{columnItems.length}</Badge>
                </div>

                <div className="space-y-2">
                  {columnItems.length === 0 && (
                    <p className="rounded-lg border border-dashed border-border/60 py-8 text-center text-xs text-muted-foreground">
                      {t("board.empty")}
                    </p>
                  )}

                  {columnItems.map((appointment) => {
                    const next = nextStatusFor(appointment.status)
                    return (
                      <div
                        key={appointment.id}
                        className="rounded-lg border border-border/60 bg-card p-3 space-y-2 text-sm"
                      >
                        <div className="flex items-start justify-between gap-2">
                          <span className="font-medium leading-tight">{appointment.clientName}</span>
                          <span className="shrink-0 text-xs text-muted-foreground font-mono">
                            {formatTime(appointment.scheduledAt)}
                          </span>
                        </div>

                        <div className="text-xs text-muted-foreground space-y-0.5">
                          <p className="flex items-center justify-between gap-1">
                            <span>{appointment.serviceName ?? t("board.card.noService")}</span>
                            {appointment.servicePrice != null && (
                              <span className="font-medium text-foreground">
                                {formatPrice(appointment.servicePrice)}
                              </span>
                            )}
                          </p>
                          <p>{appointment.stylistName ?? t("board.card.noStylist")}</p>
                          {appointment.clientPhone && <p>{appointment.clientPhone}</p>}
                          {appointment.isPaid && column === 'Completed' && (
                            <p className="flex items-center gap-1 text-emerald-600 font-medium">
                              <DollarSign className="h-3 w-3" />
                              {t("board.card.paid")}
                            </p>
                          )}
                        </div>

                        {(canWork || canManage) && column !== 'Completed' && (
                          <div className="flex gap-1 pt-1">
                            {canWork && next && (
                              <Button
                                size="sm"
                                className="flex-1 h-7 text-xs"
                                disabled={advanceStatus.isPending}
                                onClick={() => handleAdvance(appointment)}
                              >
                                {column === 'Scheduled' && t("board.actions.confirm")}
                                {column === 'Confirmed' && t("board.actions.start")}
                                {column === 'InProgress' && t("board.actions.complete")}
                              </Button>
                            )}
                            {canManage && (
                              <Button
                                size="sm"
                                variant="ghost"
                                className="h-7 px-2"
                                disabled={cancelAppointment.isPending}
                                onClick={() => setCancelTarget(appointment)}
                              >
                                <X className="h-3.5 w-3.5 text-destructive" />
                              </Button>
                            )}
                          </div>
                        )}

                        {canWork && column === 'Completed' && !appointment.isPaid && (
                          <div className="flex gap-1 pt-1">
                            <Button
                              size="sm"
                              variant="outline"
                              className="flex-1 h-7 text-xs gap-1 text-emerald-700 border-emerald-300 hover:bg-emerald-50"
                              disabled={markAsPaid.isPending}
                              onClick={() =>
                                markAsPaid.mutate(appointment.id, {
                                  onSuccess: () => toast.success(t("toast.updateSuccess")),
                                  onError: () => toast.error(t("toast.updateError")),
                                })
                              }
                            >
                              <DollarSign className="h-3 w-3" />
                              {t("board.actions.markPaid")}
                              {appointment.servicePrice != null && (
                                <span className="font-semibold">
                                  {formatPrice(appointment.servicePrice)}
                                </span>
                              )}
                            </Button>
                          </div>
                        )}
                      </div>
                    )
                  })}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* ===== CALENDAR VIEW ===== */}
      {!isLoading && boardView === 'calendar' && (
        <div className="rounded-lg border border-border/60 bg-card p-4">
          <FullCalendar
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
            initialView="timeGridWeek"
            locale={i18n.language.startsWith("es") ? esLocale : undefined}
            headerToolbar={{
              left: "prev,next today",
              center: "title",
              right: "dayGridMonth,timeGridWeek,timeGridDay",
            }}
            buttonText={{
              today: t("board.calendar.today"),
              month: t("board.calendar.month"),
              week:  t("board.calendar.week"),
              day:   t("board.calendar.day"),
            }}
            events={calendarEvents}
            datesSet={handleDatesSet}
            contentHeight="auto"
            slotMinTime="07:00:00"
            slotMaxTime="21:00:00"
            slotDuration="00:30:00"
            nowIndicator
            eventClick={({ event }) => {
              const appointment = event.extendedProps.appointment as BarbershopAppointment
              if (!canWork || appointment.status === 'Completed' || appointment.status === 'Cancelled') return
              handleAdvance(appointment)
            }}
            eventContent={(arg) => {
              const appt = arg.event.extendedProps.appointment as BarbershopAppointment
              return (
                <div className="fc-event-inner overflow-hidden leading-tight px-1 py-0.5">
                  <div className="font-semibold truncate text-[0.7rem]">{appt.clientName}</div>
                  {appt.serviceName && (
                    <div className="truncate text-[0.65rem] opacity-90">{appt.serviceName}</div>
                  )}
                </div>
              )
            }}
          />
          {canWork && (
            <p className="mt-2 text-xs text-muted-foreground text-center">
              {t("board.calendar.clickHint")}
            </p>
          )}
        </div>
      )}

      {/* ===== Appointment form ===== */}
      <BarbershopAppointmentFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        defaultDate={date}
      />

      {/* ===== Cancel dialog ===== */}
      <AlertDialog open={!!cancelTarget} onOpenChange={(open) => !open && setCancelTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("board.cancelDialog.title")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("board.cancelDialog.description", { name: cancelTarget?.clientName })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("common:buttons.cancel")}</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={handleConfirmCancel}
            >
              {t("board.actions.cancel")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* ===== Complete (mark as paid) dialog ===== */}
      <Dialog
        open={!!completeTarget}
        onOpenChange={(open) => {
          if (!open) { setCompleteTarget(null); setIsPaid(false) }
        }}
      >
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{t("board.completeDialog.title")}</DialogTitle>
            <DialogDescription>
              {t("board.completeDialog.description", { name: completeTarget?.clientName })}
            </DialogDescription>
          </DialogHeader>

          <div className="flex items-center justify-between rounded-lg border border-border/60 px-4 py-3">
            <Label htmlFor="isPaidSwitch" className="flex items-center gap-2 cursor-pointer">
              <DollarSign className="h-4 w-4 text-emerald-600" />
              {t("board.completeDialog.markPaid")}
            </Label>
            <Switch
              id="isPaidSwitch"
              checked={isPaid}
              onCheckedChange={setIsPaid}
            />
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => { setCompleteTarget(null); setIsPaid(false) }}>
              {t("common:buttons.cancel")}
            </Button>
            <Button
              onClick={handleConfirmComplete}
              disabled={advanceStatus.isPending}
              className="bg-emerald-600 hover:bg-emerald-700 text-white"
            >
              {t("board.completeDialog.confirm")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
