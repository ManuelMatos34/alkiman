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
import { BarbershopServiceFormDialog } from "@/presentation/components/BarbershopServiceFormDialog"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useBarbershopServices, useDeleteBarbershopService } from "@/application/barbershop/useBarbershopServices"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { BarbershopService } from "@/domain/types/barbershop"

export function BarbershopServicesPage() {
  const { t, i18n } = useTranslation("barbershop")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.BarbershopCatalogManage)

  const { data: services, isLoading } = useBarbershopServices()
  const deleteService = useDeleteBarbershopService()

  const { page, setPage, pageCount, paginated, totalCount } = usePagination(services, 10)

  const [formOpen, setFormOpen] = useState(false)
  const [editingService, setEditingService] = useState<BarbershopService | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<BarbershopService | null>(null)

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
      }),
    [i18n.language]
  )

  function handleCreate() {
    setEditingService(null)
    setFormOpen(true)
  }

  function handleEdit(service: BarbershopService) {
    setEditingService(service)
    setFormOpen(true)
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return
    deleteService.mutate(deleteTarget.id, {
      onSuccess: () => toast.success(t("toast.deleteSuccess")),
      onError: () => toast.error(t("toast.deleteError")),
    })
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("services.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("services.subtitle")}</p>
        </div>
        {canManage && (
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4" />
            {t("services.newButton")}
          </Button>
        )}
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("services.table.headers.name")}</TableHead>
              <TableHead>{t("services.table.headers.description")}</TableHead>
              <TableHead>{t("services.table.headers.price")}</TableHead>
              <TableHead>{t("services.table.headers.duration")}</TableHead>
              <TableHead>{t("services.table.headers.status")}</TableHead>
              {canManage && (
                <TableHead className="w-[100px] text-right">
                  {t("services.table.headers.actions")}
                </TableHead>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={canManage ? 6 : 5}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && services?.length === 0 && (
              <TableRow>
                <TableCell colSpan={canManage ? 6 : 5} className="h-24 text-center text-muted-foreground">
                  {t("services.table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginated.map((service) => (
              <TableRow key={service.id}>
                <TableCell className="font-medium">{service.name}</TableCell>
                <TableCell className="text-muted-foreground max-w-[200px] truncate">
                  {service.description ?? "—"}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {currencyFormatter.format(service.price)}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {service.durationMinutes} {t("services.table.minutesSuffix")}
                </TableCell>
                <TableCell>
                  <Badge variant={service.isActive ? "default" : "secondary"}>
                    {service.isActive
                      ? t("services.table.statusActive")
                      : t("services.table.statusInactive")}
                  </Badge>
                </TableCell>
                {canManage && (
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" size="icon-sm" onClick={() => handleEdit(service)}>
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon-sm" onClick={() => setDeleteTarget(service)}>
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </TableCell>
                )}
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

      <BarbershopServiceFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        service={editingService}
      />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("services.title")}
        description={t("portalLinks.deleteDialog.description", { title: deleteTarget?.name })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
