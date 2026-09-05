import { useMemo, useState } from "react"
import { useTranslation } from "react-i18next"
import {
  AlertTriangle,
  Car,
  Clock,
  Coins,
  HandCoins,
  Receipt,
  Timer,
  Wallet,
} from "lucide-react"
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
import { Badge } from "@/components/ui/badge"
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
import { useCarwashMetrics } from "@/application/carwash/useCarwashMetrics"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

/**
 * Fecha local en formato YYYY-MM-DD.
 *
 * A mano y no con toISOString(): ese convierte a UTC primero, así que en cualquier
 * huso al oeste de Greenwich (el nuestro) "hoy" a las 20:00 se manda como mañana y
 * el rango queda corrido un día.
 */
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
  hint?: string
  icon: React.ComponentType<{ className?: string }>
  tone?: "default" | "positive" | "negative"
}

function StatCard({ label, value, hint, icon: Icon, tone = "default" }: StatCardProps) {
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
          {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
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

/** Tablero del módulo Carwash: volumen, facturación, servicios, horarios y lavadores. */
export function CarwashMetricsPage() {
  const { t, i18n } = useTranslation("carwash")

  const [from, setFrom] = useState(() => toDateInputValue(daysAgo(29)))
  const [to, setTo] = useState(() => toDateInputValue(new Date()))

  const { data, isLoading, isError } = useCarwashMetrics(from, to)

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

  /** Las fechas vienen como "2026-09-03T00:00:00": se parte el string para no reinterpretarlas en UTC. */
  function dayLabel(isoDate: string) {
    const [year, month, day] = isoDate.slice(0, 10).split("-").map(Number)
    return dayLabelFormatter.format(new Date(year, month - 1, day))
  }

  function applyPreset(days: number) {
    setFrom(toDateInputValue(daysAgo(days - 1)))
    setTo(toDateInputValue(new Date()))
  }

  function minutesLabel(minutes: number | null) {
    return minutes === null ? "—" : t("metrics.minutesValue", { count: minutes })
  }

  const rangeFilter = (
    <div className="flex flex-wrap items-end gap-3">
      <div className="space-y-1.5">
        <Label htmlFor="metrics-from">{t("metrics.filters.from")}</Label>
        <Input
          id="metrics-from"
          type="date"
          value={from}
          max={to}
          onChange={(event) => setFrom(event.target.value)}
          className="w-40"
        />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="metrics-to">{t("metrics.filters.to")}</Label>
        <Input
          id="metrics-to"
          type="date"
          value={to}
          min={from}
          onChange={(event) => setTo(event.target.value)}
          className="w-40"
        />
      </div>
      <div className="flex gap-2">
        {PRESET_DAYS.map((days) => (
          <Button key={days} variant="outline" size="sm" onClick={() => applyPreset(days)}>
            {t("metrics.filters.lastDays", { count: days })}
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
            {t("metrics.loadError")}
          </CardContent>
        </Card>
      </div>
    )
  }

  if (isLoading || !data) {
    return (
      <div className="space-y-6">
        {header}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-24 w-full" />
          ))}
        </div>
        <Skeleton className="h-80 w-full" />
      </div>
    )
  }

  const { volume, revenue, dailyVolume, topServices, topExtras, hourlyDistribution, washerRanking } =
    data

  const hasActivity = volume.washed > 0

  return (
    <div className="space-y-8">
      {header}

      {/* Volumen */}
      <div className="space-y-3">
        <SectionTitle>{t("metrics.sections.volume")}</SectionTitle>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <StatCard label={t("metrics.stats.washed")} value={String(volume.washed)} icon={Car} />
          <StatCard
            label={t("metrics.stats.averageServiceTime")}
            value={minutesLabel(volume.averageServiceMinutes)}
            hint={t("metrics.stats.averageServiceTimeHint")}
            icon={Timer}
          />
          <StatCard
            label={t("metrics.stats.cancellationRate")}
            value={`${volume.cancellationRatePercent}%`}
            hint={t("metrics.stats.cancellationRateHint", {
              cancelled: volume.cancelled,
              expired: volume.expired,
            })}
            icon={AlertTriangle}
            tone={volume.cancellationRatePercent > 15 ? "negative" : "default"}
          />
        </div>
      </div>

      {/* Facturación */}
      <div className="space-y-3">
        <SectionTitle>{t("metrics.sections.revenue")}</SectionTitle>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <StatCard
            label={t("metrics.stats.totalRevenue")}
            value={currencyFormatter.format(revenue.totalRevenue)}
            hint={t("metrics.stats.totalRevenueHint", {
              services: currencyFormatter.format(revenue.servicesRevenue),
              extras: currencyFormatter.format(revenue.extrasRevenue),
            })}
            icon={Wallet}
            tone="positive"
          />
          <StatCard
            label={t("metrics.stats.averageTicket")}
            value={currencyFormatter.format(revenue.averageTicket)}
            icon={Receipt}
          />
          <StatCard
            label={t("metrics.stats.tipsTotal")}
            value={currencyFormatter.format(revenue.tipsTotal)}
            hint={t("metrics.stats.tipsCoverageHint", { percent: revenue.tipsCoveragePercent })}
            icon={HandCoins}
          />
        </div>
      </div>

      {/* Vehículos y facturación por día */}
      <Card>
        <CardHeader>
          <CardTitle>{t("metrics.charts.daily.title")}</CardTitle>
        </CardHeader>
        <CardContent className="h-80 px-6">
          {!hasActivity ? (
            <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
              {t("metrics.emptyRange")}
            </p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={dailyVolume.map((d) => ({ ...d, label: dayLabel(d.date) }))}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis dataKey="label" tick={{ fontSize: 12 }} minTickGap={16} />
                {/* Dos ejes: cantidad y dinero no comparten escala. Con uno solo, una
                    barra de 12 lavados desaparece al lado de 18.000 pesos. */}
                <YAxis yAxisId="left" tick={{ fontSize: 12 }} width={40} allowDecimals={false} />
                <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} width={70} />
                <Tooltip
                  formatter={(value, name) =>
                    name === t("metrics.charts.daily.revenueSeries")
                      ? currencyFormatter.format(Number(value))
                      : value
                  }
                />
                <Legend />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="washed"
                  name={t("metrics.charts.daily.washedSeries")}
                  stroke="#6366f1"
                  strokeWidth={2}
                  dot={false}
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="revenue"
                  name={t("metrics.charts.daily.revenueSeries")}
                  stroke="#10b981"
                  strokeWidth={2}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Servicios más usados */}
        <Card>
          <CardHeader>
            <CardTitle>{t("metrics.charts.topServices.title")}</CardTitle>
          </CardHeader>
          <CardContent className="h-80 px-6">
            {topServices.length === 0 ? (
              <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
                {t("metrics.emptyRange")}
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
                    name={t("metrics.charts.topServices.countSeries")}
                    fill="#6366f1"
                    radius={[0, 4, 4, 0]}
                  />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Distribución por hora */}
        <Card>
          <CardHeader>
            <CardTitle>{t("metrics.charts.hourly.title")}</CardTitle>
            <p className="text-sm text-muted-foreground">{t("metrics.charts.hourly.subtitle")}</p>
          </CardHeader>
          <CardContent className="h-80 px-6">
            {!hasActivity ? (
              <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
                {t("metrics.emptyRange")}
              </p>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart
                  data={hourlyDistribution.map((h) => ({
                    ...h,
                    label: `${String(h.hour).padStart(2, "0")}h`,
                  }))}
                >
                  <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} interval={1} />
                  <YAxis tick={{ fontSize: 12 }} width={40} allowDecimals={false} />
                  <Tooltip />
                  <Bar
                    dataKey="washed"
                    name={t("metrics.charts.hourly.washedSeries")}
                    fill="#f59e0b"
                    radius={[4, 4, 0, 0]}
                  />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Ranking de lavadores */}
      <div className="space-y-3">
        <SectionTitle>{t("metrics.sections.washerRanking")}</SectionTitle>
        <div className="rounded-lg border border-border/60">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("metrics.table.washer")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.washed")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.averageTime")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.revenue")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.tips")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {washerRanking.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    {t("metrics.table.emptyWashers")}
                  </TableCell>
                </TableRow>
              )}
              {washerRanking.map((washer) => (
                <TableRow key={washer.washerId}>
                  <TableCell className="font-medium">
                    <span className="inline-flex items-center gap-2">
                      {washer.washerName}
                      {/* Un lavador dado de baja sigue en el ranking: hizo el trabajo.
                          El badge evita que alguien lo busque en la lista de activos. */}
                      {!washer.isActive && (
                        <Badge variant="secondary">{t("metrics.table.inactive")}</Badge>
                      )}
                    </span>
                  </TableCell>
                  <TableCell className="text-right">{washer.washed}</TableCell>
                  <TableCell className="text-right text-muted-foreground">
                    {minutesLabel(washer.averageServiceMinutes)}
                  </TableCell>
                  <TableCell className="text-right">
                    {currencyFormatter.format(washer.revenue)}
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {currencyFormatter.format(washer.tipsTotal)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </div>

      {/* Extras más vendidos */}
      <div className="space-y-3">
        <SectionTitle>{t("metrics.sections.topExtras")}</SectionTitle>
        <div className="rounded-lg border border-border/60">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("metrics.table.extra")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.timesSold")}</TableHead>
                <TableHead className="text-right">{t("metrics.table.revenue")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {topExtras.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                    {t("metrics.table.emptyExtras")}
                  </TableCell>
                </TableRow>
              )}
              {topExtras.map((extra) => (
                <TableRow key={extra.extraId}>
                  <TableCell className="font-medium">
                    <span className="inline-flex items-center gap-2">
                      <Coins className="h-4 w-4 text-muted-foreground" />
                      {extra.extraName}
                    </span>
                  </TableCell>
                  <TableCell className="text-right">{extra.count}</TableCell>
                  <TableCell className="text-right font-medium">
                    {currencyFormatter.format(extra.revenue)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </div>

      <p className="flex items-center gap-2 text-xs text-muted-foreground">
        <Clock className="h-3.5 w-3.5" />
        {t("metrics.deliveredNote")}
      </p>
    </div>
  )
}
