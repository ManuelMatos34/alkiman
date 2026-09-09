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
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  MetricsDateFilter,
  MetricsSection,
  StatCard,
  daysAgo,
  toDateInputValue,
} from "@/presentation/components/metrics"
import { useBarbershopMetrics } from "@/application/barbershop/useBarbershopMetrics"
import { exportSectionsToCsv } from "@/infrastructure/export/exportToCsv"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

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

  function handleExport() {
    if (!data) return

    const { stylistRanking, topServices } = data

    exportSectionsToCsv(`alkiman-barbershop-${from}-${to}`, [
      {
        title: t("metrics.stats.total"),
        headers: [
          t("metrics.stats.total"),
          t("metrics.stats.completed"),
          t("metrics.stats.cancelled"),
          t("metrics.stats.revenue"),
        ],
        rows: [[
          data.totalAppointments,
          data.completedAppointments,
          data.cancelledAppointments,
          currencyFormatter.format(data.totalRevenue),
        ]],
      },
      {
        title: t("metrics.charts.stylists"),
        headers: [t("metrics.table.stylistName"), t("metrics.table.count"), t("metrics.table.revenue")],
        rows: stylistRanking.map((s) => [s.stylistName, s.count, currencyFormatter.format(s.revenue)]),
      },
      {
        title: t("metrics.charts.services"),
        headers: [t("metrics.charts.services"), t("metrics.table.count")],
        rows: topServices.map((s) => [s.serviceName, s.count]),
      },
    ])
  }

  const header = (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("metrics.title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("metrics.subtitle")}</p>
      </div>
      <MetricsDateFilter
        from={from}
        to={to}
        onFromChange={setFrom}
        onToChange={setTo}
        fromLabel={t("metrics.filters.from")}
        toLabel={t("metrics.filters.to")}
        presetLabel={(count) => t("metrics.filters.lastDays", { count })}
        onExport={data ? handleExport : undefined}
        exportLabel={t("metrics.filters.exportCsv")}
      />
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

      {/* Stylist ranking */}
      <div className="space-y-3">
        <MetricsSection>{t("metrics.charts.stylists")}</MetricsSection>
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
