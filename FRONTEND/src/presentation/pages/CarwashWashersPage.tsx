import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, Pencil, Trash2, KeyRound } from "lucide-react"
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
import { CarwashWasherFormDialog } from "@/presentation/components/CarwashWasherFormDialog"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useCarwashWashers } from "@/application/carwash/useCarwashWashers"
import { useDeleteWasher } from "@/application/carwash/useDeleteWasher"
import type { CarwashWasher } from "@/domain/types/carwash"

/**
 * Plantel de lavadores del negocio.
 *
 * Es el equivalente de "Clientes" en Alquileres: gente que el módulo necesita
 * nombrar, no gente que usa el sistema. Por eso vive acá y no en Usuarios, y por
 * eso dar de alta a alguien no le crea una cuenta ni consume un lugar en la
 * administración del negocio.
 */
export function CarwashWashersPage() {
  const { t } = useTranslation("carwash")
  const { data: washers, isLoading } = useCarwashWashers()
  const deleteWasher = useDeleteWasher()
  const { page, setPage, pageCount, paginated: paginatedWashers, totalCount } = usePagination(
    washers,
    10
  )

  const [formOpen, setFormOpen] = useState(false)
  const [editingWasher, setEditingWasher] = useState<CarwashWasher | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<CarwashWasher | null>(null)

  function handleCreate() {
    setEditingWasher(null)
    setFormOpen(true)
  }

  function handleEdit(washer: CarwashWasher) {
    setEditingWasher(washer)
    setFormOpen(true)
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return

    deleteWasher.mutate(deleteTarget.id, {
      onSuccess: () => toast.success(t("washers.toast.deleted")),
      onError: () => toast.error(t("washers.toast.deleteError")),
    })
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("washers.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("washers.subtitle")}</p>
        </div>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4" />
          {t("washers.newButton")}
        </Button>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("washers.table.headers.name")}</TableHead>
              <TableHead>{t("washers.table.headers.phone")}</TableHead>
              <TableHead>{t("washers.table.headers.account")}</TableHead>
              <TableHead>{t("washers.table.headers.status")}</TableHead>
              <TableHead className="w-[100px] text-right">
                {t("washers.table.headers.actions")}
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

            {!isLoading && washers?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  {t("washers.table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedWashers.map((washer) => (
              <TableRow key={washer.id}>
                <TableCell className="font-medium">{washer.fullName}</TableCell>
                <TableCell className="text-muted-foreground">
                  {washer.phone ?? "—"}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {washer.userId ? (
                    <span className="inline-flex items-center gap-1.5">
                      <KeyRound className="h-3.5 w-3.5" />
                      {washer.userEmail}
                    </span>
                  ) : (
                    t("washers.table.noAccount")
                  )}
                </TableCell>
                <TableCell>
                  <Badge variant={washer.isActive ? "default" : "secondary"}>
                    {washer.isActive
                      ? t("washers.table.statusActive")
                      : t("washers.table.statusInactive")}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <Button variant="ghost" size="icon-sm" onClick={() => handleEdit(washer)}>
                      <Pencil className="h-4 w-4" />
                    </Button>

                    {/*
                      Un lavador con turnos no se puede borrar: la FK de CWS_Tickets
                      lo impide y, sobre todo, se perdería el nombre en el historial.
                      El botón queda deshabilitado con el motivo en el title, en vez
                      de dejar que el intento falle contra el backend.
                    */}
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      disabled={!washer.canDelete}
                      title={washer.canDelete ? undefined : t("washers.table.deleteBlocked")}
                      onClick={() => setDeleteTarget(washer)}
                    >
                      <Trash2
                        className={washer.canDelete ? "h-4 w-4 text-destructive" : "h-4 w-4"}
                      />
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

      <CarwashWasherFormDialog open={formOpen} onOpenChange={setFormOpen} washer={editingWasher} />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("washers.delete.title")}
        description={t("washers.delete.description", { name: deleteTarget?.fullName })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
