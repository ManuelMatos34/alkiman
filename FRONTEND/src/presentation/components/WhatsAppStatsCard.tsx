import { MessageCircle } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useWhatsAppStats } from "@/application/whatsapp/useWhatsAppStats"

export function WhatsAppStatsCard() {
  const { data, isLoading } = useWhatsAppStats()

  if (isLoading || !data) return null

  const pct = Math.min((data.sentThisMonth / data.limit) * 100, 100)

  const barColor = data.isAtLimit
    ? "bg-destructive"
    : data.isWarning
      ? "bg-yellow-500"
      : "bg-green-500"

  const textColor = data.isAtLimit
    ? "text-destructive"
    : data.isWarning
      ? "text-yellow-600 dark:text-yellow-400"
      : "text-muted-foreground"

  return (
    <Card className="border-border/60 shadow-sm">
      <CardHeader className="pb-2">
        <CardTitle className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
          <MessageCircle className="h-4 w-4" />
          WhatsApp este mes
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        <div className="flex items-end justify-between">
          <span className="text-2xl font-bold">{data.sentThisMonth}</span>
          <span className={`text-sm ${textColor}`}>
            {data.isAtLimit
              ? "Límite alcanzado"
              : data.isWarning
                ? `¡Atención! ${data.limit - data.sentThisMonth} restantes`
                : `de ${data.limit.toLocaleString()} gratis`}
          </span>
        </div>
        {/* Progress bar */}
        <div className="h-2 w-full overflow-hidden rounded-full bg-secondary">
          <div
            className={`h-full rounded-full transition-all ${barColor}`}
            style={{ width: `${pct}%` }}
          />
        </div>
        <p className="text-xs text-muted-foreground">
          {pct.toFixed(1)}% del límite mensual gratuito de Meta Cloud API
        </p>
      </CardContent>
    </Card>
  )
}
