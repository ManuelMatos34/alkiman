import { useMemo, useState } from "react"
import { useTranslation } from "react-i18next"
import { AlertTriangle, Calendar, CheckCircle, DollarSign, XCircle } from "lucide-react"
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { useBarbershopMetrics } from "@/application/barbershop/useBarbershopMetrics"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

function toDateInputValue(date: Date) {
  const month = String(date.getMonth() + 1).padStart(2, "0")
  const day = String(date.getDate()).padStart(2, "0")
  return `${date.getFullYear()}-${month}-${day}`
}

function daysAgo(days: number) {
  const date = new Date()
  date.setDate(date.getDate() - days)
  return date
}

const PRESET_DAYS = [7, 30, 90] as const

interface StatCardProps {
  label: string
  value: string
  icon: React.ComponentType<{ className?: string }>
  tone?: "default" | "positive" | "negative"
}

function StatCard({ label, value, icon: Icon, tone = "default" }: StatCardProps) {
  return (
    <Card>
      <CardContent className="flex items-center justify-between gap-3 px-6">
        <div className="space-y-1">
          <p className="text-sm text-muted-foreground">{label}</p>
          <p
            className={
              "text-2xl font-semibold tracking-tight " +
              (tone === "positive"
                ? "text-emerald-600"
                : tone === "negative"
                  ? "text-destructive"
                  : "")
            }
          >
            {value}
          </p>
        </div>
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted">
          <Icon className="h-5 w-5 text-muted-foreground" />
        </div>
      </CardContent>
    </Card>
  )
}

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="text-lg font-semibold tracking-tight">{children}</h2>
}

export function BarbershopMetricsPage() {
  const { t, i18n } = useTranslation("barbershop")

  const [from, setFrom] = useState(() => toDateInputValue(daysAgo(29)))
  const [to, setTo] = useState(() => toDateInputValue(new Date()))

  const { data, isLoading, isError } = useBarbershopMetrics(from, to)

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
        maximumFractionDigits: 0,
      }),
    [i18n.language]
  )

  const dayLabelFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        day: "2-digit",
        month: "short",
      }),
    [i18n.language]
  )

  function dayLabel(isoDate: string) {
    const [year, month, day] = isoDate.slice(0, 10).split("-").map(Number)
    return dayLabelFormatter.format(new Date(year, month - 1, day))
  }

  function applyPreset(days: number) {
    setFrom(toDateInputValue(daysAgo(days - 1)))
    setTo(toDateInputValue(new Date()))
  }

  const rangeFilter = (
    <div className="flex flex-wrap items-end gap-3">
      <div className="space-y-1.5">
        <Label htmlFor="barbershop-metrics-from">{t("metrics.presets.last7").replace("7 ", "")}</Label>
        <Input
          id="barbershop-metrics-from"
          type="date"
          value={from}
          max={to}
          onChange={(e) => setFrom(e.target.value)}
          className="w-40"
        />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="barbershop-metrics-to">{t("metrics.presets.last30").replace("30 ", "")}</Label>
        <Input
          id="barbershop-metrics-to"
          type="date"
          value={to}
          min={from}
          onChange={(e) => setTo(e.target.value)}
          className="w-40"
        />
      </div>
      <div className="flex gap-2">
        {PRESET_DAYS.map((days) => (
          <Button key={days} variant="outline" size="sm" onClick={() => applyPreset(days)}>
            {days === 7
              ? t("metrics.presets.last7")
              : days === 30
                ? t("metrics.presets.last30")
                : t("metrics.presets.last90")}
          </Button>
        ))}
      </div>
    </div>
  )

  const header = (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("metrics.title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("metrics.subtitle")}</p>
      </div>
      {rangeFilter}
    </div>
  )

  if (isError) {
    return (
      <div className="space-y-6">
        {header}
        <Card>
          <CardContent className="flex items-center gap-3 px-6 py-10 text-sm text-muted-foreground">
            <AlertTriangle className="h-5 w-5 text-destructive" />
            {t("toast.updateError")}
          </CardContent>
        </Card>
      </div>
    )
  }

  if (isLoading || !data) {
    return (
      <div className="space-y-6">
        {header}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </div>
        <Skeleton className="h-80 w-full" />
      </div>
    )
  }

  const { totalAppointments, completedAppointments, cancelledAppointments, totalRevenue, dailyPoints, topServices, stylistRanking } = data

  return (
    <div className="space-y-8">
      {header}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label={t("metrics.stats.total")}
          value={String(totalAppointments)}
          icon={Calendar}
        />
        <StatCard
          label={t("metrics.stats.completed")}
          value={String(completedAppointments)}
          icon={CheckCircle}
          tone="positive"
        />
        <StatCard
          label={t("metrics.stats.cancelled")}
          value={String(cancelledAppointments)}
          icon={XCircle}
          tone={cancelledAppointments > 0 ? "negative" : "default"}
        />
        <StatCard
          label={t("metrics.stats.revenue")}
          value={currencyFormatter.format(totalRevenue)}
          icon={DollarSign}
          tone="positive"
        />
      </div>

      {/* Daily chart */}
      <Card>
        <CardHeader>
          <CardTitle>{t("metrics.charts.daily")}</CardTitle>
        </CardHeader>
        <CardContent className="h-80 px-6">
          {dailyPoints.length === 0 ? (
            <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
              —
            </p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={dailyPoints.map((d) => ({ ...d, label: dayLabel(d.date) }))}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis dataKey="label" tick={{ fontSize: 12 }} minTickGap={16} />
                <YAxis yAxisId="left" tick={{ fontSize: 12 }} width={40} allowDecimals={false} />
                <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} width={70} />
                <Tooltip
                  formatter={(value, name) =>
                    name === t("metrics.stats.revenue")
                      ? currencyFormatter.format(Number(value))
                      : value
                  }
                />
                <Legend />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="count"
                  name={t("metrics.stats.total")}
                  stroke="#6366f1"
                  strokeWidth={2}
                  dot={false}
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="revenue"
                  name={t("metrics.stats.revenue")}
                  stroke="#10b981"
                  strokeWidth={2}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      {/* Top services chart */}
      <Card>
        <CardHeader>
          <CardTitle>{t("metrics.charts.services")}</CardTitle>
        </CardHeader>
        <CardContent className="h-80 px-6">
          {topServices.length === 0 ? (
            <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
              —
            </p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={topServices} layout="vertical" margin={{ left: 24 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis type="number" tick={{ fontSize: 12 }} allowDecimals={false} />
                <YAxis
                  type="category"
                  dataKey="serviceName"
                  tick={{ fontSize: 12 }}
                  width={130}
                />
                <Tooltip />
                <Bar
                  dataKey="count"
                  name={t("metrics.stats.total")}
                  fill="#6366f1"
                  radius={[0, 4, 4, 0]}
                />
              </BarChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      {/* Stylist ranking table */}
      <div className="space-y-3">
        <SectionTitle>{t("metrics.charts.stylists")}</SectionTitle>
        <div className="rounded-lg border border-border/60">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("metrics.table.stylistName")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.count")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.revenue")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {stylistRanking.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                    —
                  </TableCell>
                </TableRow>
              )}
              {stylistRanking.map((s, i) => (
                <TableRow key={i}>
                  <TableCell className="font-medium">{s.stylistName}</TableCell>
                  <TableCell className="text-right">{s.count}</TableCell>
                  <TableCell className="text-right">{currencyFormatter.format(s.revenue)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </div>
    </div>
  )
}
