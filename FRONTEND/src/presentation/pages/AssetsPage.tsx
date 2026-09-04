import { useMemo, useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, MoreHorizontal, Pencil, Trash2 } from "lucide-react"
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { AssetFormDialog } from "@/presentation/components/AssetFormDialog"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useAssets } from "@/application/assets/useAssets"
import { useCategories } from "@/application/categories/useCategories"
import { useDeleteAsset } from "@/application/assets/useDeleteAsset"
import { useUpdateAssetStatus } from "@/application/assets/useUpdateAssetStatus"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { Asset, AssetStatus } from "@/domain/types/asset"

const statusVariants: Record<
  AssetStatus,
  "default" | "secondary" | "destructive"
> = {
  Available: "secondary",
  Rented: "default",
  Maintenance: "destructive",
}

export function AssetsPage() {
  const { t, i18n } = useTranslation(["assets", "common"])
  const statusLabels: Record<AssetStatus, string> = {
    Available: t("status.Available"),
    Rented: t("status.Rented"),
    Maintenance: t("status.Maintenance"),
  }
  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
      }),
    [i18n.language]
  )
  const { data: assets, isLoading } = useAssets()
  const { data: categories } = useCategories()
  const deleteAsset = useDeleteAsset()
  const updateAssetStatus = useUpdateAssetStatus()
  const { page, setPage, pageCount, paginated: paginatedAssets, totalCount } = usePagination(assets, 10)

  const [formOpen, setFormOpen] = useState(false)
  const [editingAsset, setEditingAsset] = useState<Asset | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Asset | null>(null)

  const categoryNameById = new Map(
    categories?.map((category) => [category.id, category.name])
  )

  function handleCreate() {
    setEditingAsset(null)
    setFormOpen(true)
  }

  function handleEdit(asset: Asset) {
    setEditingAsset(asset)
    setFormOpen(true)
  }

  function handleStatusChange(asset: Asset, status: AssetStatus) {
    if (status === asset.status) return

    updateAssetStatus.mutate(
      { id: asset.id, status },
      {
        onSuccess: () => toast.success(t("toast.statusUpdated")),
        onError: () => toast.error(t("toast.statusUpdateError")),
      }
    )
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return

    deleteAsset.mutate(deleteTarget.id, {
      onSuccess: () => toast.success(t("toast.deleted")),
      onError: () => toast.error(t("toast.deleteError")),
    })
    setDeleteTarget(null)
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
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4" />
          {t("newAsset")}
        </Button>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.name")}</TableHead>
              <TableHead>{t("table.headers.category")}</TableHead>
              <TableHead>{t("table.headers.status")}</TableHead>
              <TableHead>{t("table.headers.basePrice")}</TableHead>
              <TableHead>{t("table.headers.stock")}</TableHead>
              <TableHead className="w-[60px] text-right">
                {t("table.headers.actions")}
              </TableHead>
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

            {!isLoading && assets?.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={6}
                  className="h-24 text-center text-muted-foreground"
                >
                  {t("table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedAssets.map((asset) => (
              <TableRow key={asset.id}>
                <TableCell className="font-medium">{asset.name}</TableCell>
                <TableCell className="text-muted-foreground">
                  {categoryNameById.get(asset.categoryId) ?? t("table.noCategory")}
                </TableCell>
                <TableCell>
                  <Select
                    value={asset.status}
                    onValueChange={(value) =>
                      handleStatusChange(asset, value as AssetStatus)
                    }
                  >
                    <SelectTrigger size="sm" className="w-[150px]">
                      <SelectValue asChild>
                        <Badge variant={statusVariants[asset.status]}>
                          {statusLabels[asset.status]}
                        </Badge>
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      {(Object.keys(statusLabels) as AssetStatus[]).map(
                        (status) => (
                          <SelectItem key={status} value={status}>
                            {statusLabels[status]}
                          </SelectItem>
                        )
                      )}
                    </SelectContent>
                  </Select>
                </TableCell>
                <TableCell>
                  {currencyFormatter.format(asset.basePrice)}
                </TableCell>
                <TableCell>{asset.stock}</TableCell>
                <TableCell className="text-right">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon-sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem onClick={() => handleEdit(asset)}>
                        <Pencil className="h-4 w-4" />
                        {t("common:buttons.edit")}
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        variant="destructive"
                        onClick={() => setDeleteTarget(asset)}
                      >
                        <Trash2 className="h-4 w-4" />
                        {t("common:buttons.delete")}
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
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

      <AssetFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        asset={editingAsset}
      />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("delete.title")}
        description={t("delete.description", { name: deleteTarget?.name })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
