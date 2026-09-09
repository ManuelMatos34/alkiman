import { useState } from "react"
import { useTranslation } from "react-i18next"
import { Plus, Pencil } from "lucide-react"
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
import { CarwashTipSettingsCard } from "@/presentation/components/CarwashTipSettingsCard"
import { CarwashWasherFormDialog } from "@/presentation/components/CarwashWasherFormDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useCarwashWashers } from "@/application/carwash/useCarwashWashers"
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
  const { page, setPage, pageCount, paginated: paginatedWashers, totalCount } = usePagination(
    washers,
    10
  )

  const [formOpen, setFormOpen] = useState(false)
  const [editingWasher, setEditingWasher] = useState<CarwashWasher | null>(null)

  function handleCreate() {
    setEditingWasher(null)
    setFormOpen(true)
  }

  function handleEdit(washer: CarwashWasher) {
    setEditingWasher(washer)
    setFormOpen(true)
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
              <TableHead>{t("washers.table.headers.status")}</TableHead>
              <TableHead className="w-[60px] text-right">
                {t("washers.table.headers.actions")}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={4}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && washers?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
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
                <TableCell>
                  <Badge variant={washer.isActive ? "default" : "secondary"}>
                    {washer.isActive
                      ? t("washers.table.statusActive")
                      : t("washers.table.statusInactive")}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  <Button variant="ghost" size="icon-sm" onClick={() => handleEdit(washer)}>
                    <Pencil className="h-4 w-4" />
                  </Button>
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

      {/* La política de propinas va acá, junto a la gente que las recibe, y no en una
          pantalla de ajustes aparte: es plata de este plantel. */}
      <CarwashTipSettingsCard />

      <CarwashWasherFormDialog open={formOpen} onOpenChange={setFormOpen} washer={editingWasher} />
    </div>
  )
}
