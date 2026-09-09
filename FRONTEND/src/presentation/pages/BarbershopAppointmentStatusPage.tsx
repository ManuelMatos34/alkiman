import { useMemo } from "react"
import { useParams } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { Loader2 } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useBarbershopAppointmentStatus } from "@/application/barbershop/useBarbershopPublic"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { BarbershopAppointmentStatus } from "@/domain/types/barbershop"

const STATUS_BADGE_VARIANT: Record<
  BarbershopAppointmentStatus,
  "default" | "secondary" | "destructive" | "outline"
> = {
  Scheduled:  "outline",
  Confirmed:  "secondary",
  InProgress: "default",
  Completed:  "default",
  Cancelled:  "destructive",
}

const STATUS_COLOR_CLASS: Record<BarbershopAppointmentStatus, string> = {
  Scheduled:  "text-blue-600",
  Confirmed:  "text-amber-600",
  InProgress: "text-violet-600",
  Completed:  "text-emerald-600",
  Cancelled:  "text-destructive",
}

export function BarbershopAppointmentStatusPage() {
  const { t, i18n } = useTranslation("barbershop")
  const { token } = useParams<{ token: string }>()
  const { data: status, isLoading, isError } = useBarbershopAppointmentStatus(token)

  const dateTimeFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        dateStyle: "long",
        timeStyle: "short",
      }),
    [i18n.language]
  )

  if (!token || isError) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-4 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("public.status.title")}
        </h1>
        <p className="text-sm text-muted-foreground">—</p>
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

  if (!status) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-4 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">{t("public.status.title")}</h1>
        <p className="text-sm text-muted-foreground">—</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background px-4 py-10">
      <div className="mx-auto max-w-md space-y-4">
        <div className="text-center">
          <h1 className="text-2xl font-semibold tracking-tight">{t("public.status.title")}</h1>
        </div>

        <Card>
          <CardHeader className="flex-row items-start justify-between space-y-0">
            <CardTitle className="text-lg">{status.clientName}</CardTitle>
            <Badge
              variant={STATUS_BADGE_VARIANT[status.status]}
              className={STATUS_COLOR_CLASS[status.status]}
            >
              {t(`public.status.statuses.${status.status}`)}
            </Badge>
          </CardHeader>
          <CardContent className="space-y-3 text-sm">
            {status.stylistName && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t("public.status.stylist")}</span>
                <span className="font-medium">{status.stylistName}</span>
              </div>
            )}
            {status.serviceName && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t("public.status.service")}</span>
                <span className="font-medium">{status.serviceName}</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t("public.status.scheduledAt")}</span>
              <span className="font-medium">
                {dateTimeFormatter.format(new Date(status.scheduledAt))}
              </span>
            </div>
            <div className="flex justify-between border-t border-border/60 pt-3">
              <span className="text-muted-foreground">{t("public.status.status")}</span>
              <span className={`font-semibold ${STATUS_COLOR_CLASS[status.status]}`}>
                {t(`public.status.statuses.${status.status}`)}
              </span>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
