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
import { BarbershopStylistFormDialog } from "@/presentation/components/BarbershopStylistFormDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useBarbershopStylists } from "@/application/barbershop/useBarbershopStylists"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import type { BarbershopStylist } from "@/domain/types/barbershop"

export function BarbershopStylistsPage() {
  const { t } = useTranslation("barbershop")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.BarbershopStylistsManage)

  const { data: stylists, isLoading } = useBarbershopStylists()
  const { page, setPage, pageCount, paginated, totalCount } = usePagination(stylists, 10)

  const [formOpen, setFormOpen] = useState(false)
  const [editingStylist, setEditingStylist] = useState<BarbershopStylist | null>(null)

  function handleCreate() {
    setEditingStylist(null)
    setFormOpen(true)
  }

  function handleEdit(stylist: BarbershopStylist) {
    setEditingStylist(stylist)
    setFormOpen(true)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("stylists.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("stylists.subtitle")}</p>
        </div>
        {canManage && (
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4" />
            {t("stylists.newButton")}
          </Button>
        )}
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("stylists.table.headers.name")}</TableHead>
              <TableHead>{t("stylists.table.headers.phone")}</TableHead>
              <TableHead>{t("stylists.table.headers.email")}</TableHead>
              <TableHead>{t("stylists.table.headers.status")}</TableHead>
              {canManage && (
                <TableHead className="w-[60px] text-right">
                  {t("stylists.table.headers.actions")}
                </TableHead>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={canManage ? 5 : 4}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && stylists?.length === 0 && (
              <TableRow>
                <TableCell colSpan={canManage ? 5 : 4} className="h-24 text-center text-muted-foreground">
                  {t("stylists.table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginated.map((stylist) => (
              <TableRow key={stylist.id}>
                <TableCell className="font-medium">{stylist.fullName}</TableCell>
                <TableCell className="text-muted-foreground">
                  {stylist.phone ?? t("stylists.table.noPhone")}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {stylist.email ?? t("stylists.table.noEmail")}
                </TableCell>
                <TableCell>
                  <Badge variant={stylist.isActive ? "default" : "secondary"}>
                    {stylist.isActive
                      ? t("stylists.table.statusActive")
                      : t("stylists.table.statusInactive")}
                  </Badge>
                </TableCell>
                {canManage && (
                  <TableCell className="text-right">
                    <Button variant="ghost" size="icon-sm" onClick={() => handleEdit(stylist)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
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

      <BarbershopStylistFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        stylist={editingStylist}
      />
    </div>
  )
}
