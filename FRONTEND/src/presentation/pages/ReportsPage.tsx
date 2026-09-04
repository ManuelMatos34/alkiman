import { useMemo } from "react"
import { useTranslation } from "react-i18next"
import {
  TrendingUp,
  TrendingDown,
  Wallet,
  Receipt,
  AlertTriangle,
  Boxes,
  Percent,
  Warehouse,
  Award,
  Users,
} from "lucide-react"
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import { Badge } from "@/components/ui/badge"
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
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useReportSummary } from "@/application/reports/useReportSummary"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

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

/** Tablero de métricas: finanzas, rentas, activos, vencidos y rankings del negocio. */
export function ReportsPage() {
  const { t, i18n } = useTranslation("reports")
  const { data, isLoading } = useReportSummary()
  const {
    page: overduePage,
    setPage: setOverduePage,
    pageCount: overduePageCount,
    paginated: paginatedOverdueRentals,
    totalCount: overdueTotalCount,
  } = usePagination(data?.overdueRentals, 10)

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
        maximumFractionDigits: 0,
      }),
    [i18n.language]
  )

  const dateFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      }),
    [i18n.language]
  )

  const monthLabelFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        month: "short",
        year: "2-digit",
      }),
    [i18n.language]
  )

  function monthLabel(yearMonth: string) {
    const [year, month] = yearMonth.split("-").map(Number)
    return monthLabelFormatter.format(new Date(year, month - 1, 1))
  }

  if (isLoading || !data) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("subtitle")}</p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 8 }).map((_, index) => (
            <Skeleton key={index} className="h-24 w-full" />
          ))}
        </div>
      </div>
    )
  }

  const { financial, rentals, assets, revenueByCategory, revenueByMonth, overdueRentals, topAssets, topCustomers } =
    data

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("subtitle")}</p>
      </div>

      {/* Finanzas */}
      <div className="space-y-3">
        <SectionTitle>{t("sections.finances")}</SectionTitle>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            label={t("stats.totalIncome")}
            value={currencyFormatter.format(financial.totalIncome)}
            icon={TrendingUp}
            tone="positive"
          />
          <StatCard
            label={t("stats.totalExpenses")}
            value={currencyFormatter.format(financial.totalExpenses)}
            icon={TrendingDown}
            tone="negative"
          />
          <StatCard
            label={t("stats.netProfit")}
            value={currencyFormatter.format(financial.netProfit)}
            icon={Wallet}
            tone={financial.netProfit >= 0 ? "positive" : "negative"}
          />
          <StatCard
            label={t("stats.totalContractedValue")}
            value={currencyFormatter.format(financial.totalContractedValue)}
            icon={Receipt}
          />
        </div>
      </div>

      {/* Rentas */}
      <div className="space-y-3">
        <SectionTitle>{t("sections.rentals")}</SectionTitle>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label={t("stats.totalRentals")} value={String(rentals.total)} icon={Receipt} />
          <StatCard label={t("stats.active")} value={String(rentals.active)} icon={TrendingUp} />
          <StatCard label={t("stats.completed")} value={String(rentals.completed)} icon={Award} />
          <StatCard
            label={t("stats.overdue")}
            value={String(rentals.overdue)}
            icon={AlertTriangle}
            tone={rentals.overdue > 0 ? "negative" : "default"}
          />
        </div>
      </div>

      {/* Activos */}
      <div className="space-y-3">
        <SectionTitle>{t("sections.assets")}</SectionTitle>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label={t("stats.totalAssets")} value={String(assets.total)} icon={Boxes} />
          <StatCard label={t("stats.available")} value={String(assets.available)} icon={Boxes} />
          <StatCard label={t("stats.rented")} value={String(assets.rented)} icon={Boxes} />
          <StatCard label={t("stats.maintenance")} value={String(assets.maintenance)} icon={Boxes} />
          <StatCard
            label={t("stats.utilizationRate")}
            value={`${assets.utilizationRatePercent}%`}
            icon={Percent}
          />
          <StatCard
            label={t("stats.inventoryValue")}
            value={currencyFormatter.format(assets.inventoryValue)}
            icon={Warehouse}
          />
        </div>
      </div>

      {/* Ingresos vs egresos por mes */}
      <Card>
        <CardHeader>
          <CardTitle>{t("charts.incomeVsExpenses.title")}</CardTitle>
        </CardHeader>
        <CardContent className="h-80 px-6">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={revenueByMonth.map((m) => ({ ...m, label: monthLabel(m.month) }))}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="label" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} width={80} />
              <Tooltip formatter={(value) => currencyFormatter.format(Number(value))} />
              <Legend />
              <Bar
                dataKey="income"
                name={t("charts.incomeVsExpenses.incomeSeries")}
                fill="#10b981"
                radius={[4, 4, 0, 0]}
              />
              <Bar
                dataKey="expenses"
                name={t("charts.incomeVsExpenses.expensesSeries")}
                fill="#ef4444"
                radius={[4, 4, 0, 0]}
              />
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      {/* Ingresos por categoría */}
      <Card>
        <CardHeader>
          <CardTitle>{t("charts.revenueByCategory.title")}</CardTitle>
        </CardHeader>
        <CardContent className="h-80 px-6">
          {revenueByCategory.length === 0 ? (
            <p className="flex h-full items-center justify-center text-sm text-muted-foreground">
              {t("charts.revenueByCategory.emptyState")}
            </p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={revenueByCategory} layout="vertical" margin={{ left: 24 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis type="number" tick={{ fontSize: 12 }} />
                <YAxis type="category" dataKey="categoryName" tick={{ fontSize: 12 }} width={120} />
                <Tooltip formatter={(value) => currencyFormatter.format(Number(value))} />
                <Bar
                  dataKey="revenue"
                  name={t("charts.revenueByCategory.incomeSeries")}
                  fill="#6366f1"
                  radius={[0, 4, 4, 0]}
                />
              </BarChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      {/* Rentas vencidas */}
      <div className="space-y-3">
        <SectionTitle>{t("sections.overdueAssets")}</SectionTitle>
        <div className="rounded-lg border border-border/60">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("table.headers.asset")}</TableHead>
                <TableHead>{t("table.headers.customer")}</TableHead>
                <TableHead>{t("table.headers.dueDate")}</TableHead>
                <TableHead>{t("table.headers.daysOverdue")}</TableHead>
                <TableHead className="text-right">{t("table.headers.amount")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {overdueRentals.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    {t("table.emptyOverdue")}
                  </TableCell>
                </TableRow>
              )}
              {paginatedOverdueRentals.map((item) => (
                <TableRow key={item.rentalId}>
                  <TableCell className="font-medium">{item.assetName}</TableCell>
                  <TableCell className="text-muted-foreground">{item.customerName}</TableCell>
                  <TableCell className="text-muted-foreground">
                    {dateFormatter.format(new Date(item.endDate))}
                  </TableCell>
                  <TableCell>
                    <Badge variant="destructive">
                      {item.daysOverdue} {t("table.daysOverdueSuffix")}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {currencyFormatter.format(item.totalPrice)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <TablePagination
            page={overduePage}
            pageCount={overduePageCount}
            totalCount={overdueTotalCount}
            pageSize={10}
            onPageChange={setOverduePage}
          />
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Top activos */}
        <div className="space-y-3">
          <SectionTitle>{t("sections.topAssets")}</SectionTitle>
          <div className="rounded-lg border border-border/60">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t("table.headers.asset")}</TableHead>
                  <TableHead>{t("table.headers.rentals")}</TableHead>
                  <TableHead className="text-right">{t("table.headers.income")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {topAssets.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                      {t("table.emptyData")}
                    </TableCell>
                  </TableRow>
                )}
                {topAssets.map((item) => (
                  <TableRow key={item.assetId}>
                    <TableCell className="font-medium">{item.assetName}</TableCell>
                    <TableCell className="text-muted-foreground">{item.rentalsCount}</TableCell>
                    <TableCell className="text-right font-medium">
                      {currencyFormatter.format(item.revenue)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>

        {/* Top clientes */}
        <div className="space-y-3">
          <SectionTitle>{t("sections.topCustomers")}</SectionTitle>
          <div className="rounded-lg border border-border/60">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t("table.headers.customer")}</TableHead>
                  <TableHead>{t("table.headers.rentals")}</TableHead>
                  <TableHead className="text-right">{t("table.headers.totalPaid")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {topCustomers.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                      {t("table.emptyData")}
                    </TableCell>
                  </TableRow>
                )}
                {topCustomers.map((item) => (
                  <TableRow key={item.customerId}>
                    <TableCell className="font-medium">
                      <span className="inline-flex items-center gap-2">
                        <Users className="h-4 w-4 text-muted-foreground" />
                        {item.customerName}
                      </span>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{item.rentalsCount}</TableCell>
                    <TableCell className="text-right font-medium">
                      {currencyFormatter.format(item.totalPaid)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      </div>
    </div>
  )
}
