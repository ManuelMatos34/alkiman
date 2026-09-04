import { useMemo, useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, Pencil, Trash2 } from "lucide-react"
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
import { CarwashExtraFormDialog } from "@/presentation/components/CarwashExtraFormDialog"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useCarwashExtras } from "@/application/carwash/useCarwashExtras"
import { useDeleteExtra } from "@/application/carwash/useDeleteExtra"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { CarwashExtraItem } from "@/domain/types/carwash"

/** Catálogo de agregados de Carwash que se suman al servicio base (encerado, ozono, ...). */
export function CarwashExtrasPage() {
  const { t, i18n } = useTranslation("carwash")
  const { data: extras, isLoading } = useCarwashExtras()
  const deleteExtra = useDeleteExtra()
  const { page, setPage, pageCount, paginated: paginatedExtras, totalCount } = usePagination(
    extras,
    10
  )

  const [formOpen, setFormOpen] = useState(false)
  const [editingExtra, setEditingExtra] = useState<CarwashExtraItem | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<CarwashExtraItem | null>(null)

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
      }),
    [i18n.language]
  )

  function handleCreate() {
    setEditingExtra(null)
    setFormOpen(true)
  }

  function handleEdit(extra: CarwashExtraItem) {
    setEditingExtra(extra)
    setFormOpen(true)
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return

    deleteExtra.mutate(deleteTarget.id, {
      onSuccess: () => toast.success(t("extras.toast.deleted")),
      // El backend rechaza el borrado si el extra ya se usó en algún ticket: desactivarlo es la
      // salida, porque borrarlo rompería el histórico de precios congelados.
      onError: () => toast.error(t("extras.toast.deleteError")),
    })
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("extras.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("extras.subtitle")}</p>
        </div>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4" />
          {t("extras.newButton")}
        </Button>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("extras.table.headers.name")}</TableHead>
              <TableHead>{t("extras.table.headers.price")}</TableHead>
              <TableHead>{t("extras.table.headers.estimatedMinutes")}</TableHead>
              <TableHead>{t("extras.table.headers.status")}</TableHead>
              <TableHead className="w-[100px] text-right">
                {t("extras.table.headers.actions")}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={5}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && extras?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  {t("extras.table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedExtras.map((extra) => (
              <TableRow key={extra.id}>
                <TableCell className="font-medium">{extra.name}</TableCell>
                <TableCell className="text-muted-foreground">
                  {currencyFormatter.format(extra.price)}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {t("extras.table.minutesValue", { count: extra.estimatedMinutes })}
                </TableCell>
                <TableCell>
                  <Badge variant={extra.isActive ? "default" : "secondary"}>
                    {extra.isActive
                      ? t("extras.table.statusActive")
                      : t("extras.table.statusInactive")}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <Button variant="ghost" size="icon-sm" onClick={() => handleEdit(extra)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon-sm" onClick={() => setDeleteTarget(extra)}>
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
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

      <CarwashExtraFormDialog open={formOpen} onOpenChange={setFormOpen} extra={editingExtra} />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("extras.delete.title")}
        description={t("extras.delete.description", { name: deleteTarget?.name })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
