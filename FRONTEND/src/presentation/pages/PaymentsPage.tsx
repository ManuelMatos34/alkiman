import { useMemo } from "react"
import { useTranslation } from "react-i18next"
import { Badge } from "@/components/ui/badge"
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
import { usePayments } from "@/application/payments/usePayments"
import { useRentals } from "@/application/rentals/useRentals"
import { useAssets } from "@/application/assets/useAssets"
import { useCustomers } from "@/application/customers/useCustomers"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { PaymentType } from "@/domain/types/payment"

const paymentTypeVariants: Record<PaymentType, "default" | "destructive"> = {
  Income: "default",
  Expense: "destructive",
}

/** Historial de movimientos (débitos y créditos) del negocio. Solo lectura. */
export function PaymentsPage() {
  const { t, i18n } = useTranslation("payments")
  const { data: payments, isLoading } = usePayments()
  const { data: rentals } = useRentals()
  const { data: assets } = useAssets()
  const { data: customers } = useCustomers()
  const { page, setPage, pageCount, paginated: paginatedPayments, totalCount } = usePagination(payments, 10)

  const paymentTypeLabels: Record<PaymentType, string> = {
    Income: t("type.income"),
    Expense: t("type.expense"),
  }

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
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

  const assetNameById = new Map(assets?.map((asset) => [asset.id, asset.name]))
  const customerNameById = new Map(
    customers?.map((customer) => [customer.id, customer.fullName])
  )
  const rentalById = new Map(rentals?.map((rental) => [rental.id, rental]))

  function rentalLabel(rentalId: string | null) {
    if (!rentalId) return "—"
    const rental = rentalById.get(rentalId)
    if (!rental) return "—"
    return (
      (assetNameById.get(rental.assetId) ?? t("table.unknownAsset")) +
      " — " +
      (customerNameById.get(rental.customerId) ?? t("table.unknownCustomer"))
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("subtitle")}</p>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.date")}</TableHead>
              <TableHead>{t("table.headers.type")}</TableHead>
              <TableHead>{t("table.headers.amount")}</TableHead>
              <TableHead>{t("table.headers.relatedRental")}</TableHead>
              <TableHead>{t("table.headers.reference")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 4 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={5}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && payments?.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="h-24 text-center text-muted-foreground"
                >
                  {t("table.emptyState")}
                </TableCell>
              </TableRow>
            )}

            {paginatedPayments.map((payment) => (
              <TableRow key={payment.id}>
                <TableCell className="text-muted-foreground">
                  {dateFormatter.format(new Date(payment.paymentDate))}
                </TableCell>
                <TableCell>
                  <Badge variant={paymentTypeVariants[payment.type]}>
                    {paymentTypeLabels[payment.type]}
                  </Badge>
                </TableCell>
                <TableCell className="font-medium">
                  {currencyFormatter.format(payment.amount)}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {rentalLabel(payment.rentalId)}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {payment.stripeTransactionId ?? "—"}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <TablePagination
          page={page}
          pageCount={pageCount}
          totalCount={totalCount}
          pageSize={10}
          onPageChange={setPage}
        />
      </div>
    </div>
  )
}
