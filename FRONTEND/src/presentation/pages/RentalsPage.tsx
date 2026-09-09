import { useMemo, useState } from "react"
import { Link } from "react-router-dom"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, Upload } from "lucide-react"
import { Button } from "@/components/ui/button"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { RentalFormDialog } from "@/presentation/components/RentalFormDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useRentals } from "@/application/rentals/useRentals"
import { useUpdateRentalStatus } from "@/application/rentals/useUpdateRentalStatus"
import { useAssets } from "@/application/assets/useAssets"
import { useCustomers } from "@/application/customers/useCustomers"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { RentalStatus } from "@/domain/types/rental"

const statusVariants: Record<
  RentalStatus,
  "default" | "secondary" | "destructive" | "outline"
> = {
  Active: "default",
  Completed: "secondary",
  Overdue: "destructive",
  Cancelled: "outline",
}

export function RentalsPage() {
  const { t, i18n } = useTranslation("rentals")
  const statusLabels: Record<RentalStatus, string> = {
    Active: t("status.Active"),
    Completed: t("status.Completed"),
    Overdue: t("status.Overdue"),
    Cancelled: t("status.Cancelled"),
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
  const { data: rentals, isLoading } = useRentals()
  const { data: assets } = useAssets()
  const { data: customers } = useCustomers()
  const updateRentalStatus = useUpdateRentalStatus()
  const { page, setPage, pageCount, paginated: paginatedRentals, totalCount } = usePagination(rentals, 10)

  const [formOpen, setFormOpen] = useState(false)

  const assetNameById = new Map(assets?.map((asset) => [asset.id, asset.name]))
  const customerNameById = new Map(
    customers?.map((customer) => [customer.id, customer.fullName])
  )

  function handleStatusChange(rentalId: string, currentStatus: RentalStatus, status: RentalStatus) {
    if (status === currentStatus) return

    updateRentalStatus.mutate(
      { id: rentalId, request: { status } },
      {
        onSuccess: () => toast.success(t("toast.statusUpdated")),
        onError: () => toast.error(t("toast.statusUpdateError")),
      }
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link to="/rentas/importar">
              <Upload className="h-4 w-4" />
              {t("importButton")}
            </Link>
          </Button>
          <Button onClick={() => setFormOpen(true)}>
            <Plus className="h-4 w-4" />
            {t("newRental")}
          </Button>
        </div>
      </div>

      <div className="rounded-lg border border-border/60">
        <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.asset")}</TableHead>
              <TableHead className="hidden sm:table-cell">{t("table.headers.customer")}</TableHead>
              <TableHead className="hidden md:table-cell">{t("table.headers.startDate")}</TableHead>
              <TableHead className="hidden md:table-cell">{t("table.headers.endDate")}</TableHead>
              <TableHead className="hidden sm:table-cell">{t("table.headers.totalPrice")}</TableHead>
              <TableHead>{t("table.headers.status")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 4 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={6}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && rentals?.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={6}
                  className="h-24 text-center text-muted-foreground"
                >
                  {t("table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedRentals.map((rental) => (
              <TableRow key={rental.id}>
                <TableCell className="font-medium">
                  <div>{assetNameById.get(rental.assetId) ?? t("table.emptyValue")}</div>
                  <div className="mt-0.5 text-xs text-muted-foreground sm:hidden">
                    {customerNameById.get(rental.customerId) ?? t("table.emptyValue")}
                    {" · "}
                    {currencyFormatter.format(rental.totalPrice)}
                  </div>
                  <div className="mt-0.5 text-xs text-muted-foreground md:hidden sm:block hidden">
                    {dateFormatter.format(new Date(rental.startDate))} – {dateFormatter.format(new Date(rental.endDate))}
                  </div>
                </TableCell>
                <TableCell className="hidden sm:table-cell text-muted-foreground">
                  {customerNameById.get(rental.customerId) ?? t("table.emptyValue")}
                </TableCell>
                <TableCell className="hidden md:table-cell text-muted-foreground">
                  {dateFormatter.format(new Date(rental.startDate))}
                </TableCell>
                <TableCell className="hidden md:table-cell text-muted-foreground">
                  {dateFormatter.format(new Date(rental.endDate))}
                </TableCell>
                <TableCell className="hidden sm:table-cell">
                  {currencyFormatter.format(rental.totalPrice)}
                </TableCell>
                <TableCell>
                  <Select
                    value={rental.status}
                    onValueChange={(value) =>
                      handleStatusChange(rental.id, rental.status, value as RentalStatus)
                    }
                  >
                    <SelectTrigger size="sm" className="w-[130px]">
                      <SelectValue asChild>
                        <Badge variant={statusVariants[rental.status]}>
                          {statusLabels[rental.status]}
                        </Badge>
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      {(Object.keys(statusLabels) as RentalStatus[]).map(
                        (status) => (
                          <SelectItem key={status} value={status}>
                            {statusLabels[status]}
                          </SelectItem>
                        )
                      )}
                    </SelectContent>
                  </Select>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        </div>
        <TablePagination
          page={page}
          pageCount={pageCount}
          totalCount={totalCount}
          pageSize={10}
          onPageChange={setPage}
        />
      </div>

      <RentalFormDialog open={formOpen} onOpenChange={setFormOpen} />
    </div>
  )
}
