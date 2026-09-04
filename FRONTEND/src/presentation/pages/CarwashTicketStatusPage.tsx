import { useEffect, useMemo, useState } from "react"
import { useParams } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { Loader2, Timer } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useCarwashTicketStatus } from "@/application/carwash/useCarwashTicketStatus"
import { PublicAppearanceSync } from "@/presentation/components/PublicAppearanceSync"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { CarwashTicketStatus } from "@/domain/types/carwash"

const STATUS_BADGE_VARIANT: Record<CarwashTicketStatus, "default" | "secondary" | "destructive" | "outline"> = {
  Waiting: "secondary",
  ArrivalPending: "outline",
  InProgress: "default",
  Drying: "default",
  Waxing: "default",
  Ready: "default",
  Delivered: "secondary",
  Cancelled: "destructive",
  Expired: "destructive",
}

function formatCountdown(deadline: string, now: number) {
  const diffMs = new Date(deadline).getTime() - now
  if (diffMs <= 0) return "00:00"
  const totalSeconds = Math.floor(diffMs / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`
}

/** Link público de estado del turno de Carwash: el cliente ve en vivo cómo avanza su vehículo. */
export function CarwashTicketStatusPage() {
  const { t, i18n } = useTranslation("carwash")
  const { token } = useParams<{ token: string }>()
  const { data: status, isLoading, isError } = useCarwashTicketStatus(token)

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

  if (!token) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-4 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">{t("ticketStatus.invalidLink.title")}</h1>
        <p className="text-sm text-muted-foreground">{t("ticketStatus.invalidLink.description")}</p>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError || !status) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-4 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">{t("ticketStatus.notFound.title")}</h1>
        <p className="text-sm text-muted-foreground">{t("ticketStatus.notFound.description")}</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background px-4 py-10">
      <PublicAppearanceSync themeMode={status.themeMode} accentColor={status.accentColor} />
      <div className="mx-auto max-w-md space-y-4">
        <div className="text-center">
          <h1 className="text-2xl font-semibold tracking-tight">{t("ticketStatus.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("ticketStatus.subtitle")}</p>
        </div>

        <Card>
          <CardHeader className="flex-row items-start justify-between space-y-0">
            <div>
              <CardTitle className="text-lg">{status.vehiclePlate}</CardTitle>
              <p className="mt-1 text-sm text-muted-foreground">{status.serviceName}</p>
            </div>
            <Badge variant={STATUS_BADGE_VARIANT[status.status]}>
              {t(`status.${status.status}`)}
            </Badge>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t("ticketStatus.queueNumber")}</span>
              <span className="font-medium">{status.queueNumber}</span>
            </div>

            {status.extras.map((extra) => (
              <div key={extra.extraId} className="flex justify-between">
                <span className="text-muted-foreground">{extra.name}</span>
                <span>{currencyFormatter.format(extra.price)}</span>
              </div>
            ))}

            <div className="flex justify-between border-t border-border/60 pt-3">
              <span className="font-medium">{t("extras.total")}</span>
              <span className="font-semibold">{currencyFormatter.format(status.total)}</span>
            </div>

            <p className="text-muted-foreground">{t(`ticketStatus.statusMessage.${status.status}`)}</p>

            {status.status === "ArrivalPending" && status.arrivalDeadline && (
              <div className="flex items-center gap-2 rounded-lg border border-primary/40 bg-muted/40 p-3">
                <Timer className="h-4 w-4 text-muted-foreground" />
                <div>
                  <p className="text-xs text-muted-foreground">{t("ticketStatus.countdownLabel")}</p>
                  <p className="font-mono text-lg font-semibold">
                    {formatCountdown(status.arrivalDeadline, now)}
                  </p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
