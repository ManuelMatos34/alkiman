import { useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, Pencil, Trash2, CheckCircle2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { ContractTemplateFormDialog } from "@/presentation/components/ContractTemplateFormDialog"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useCategories } from "@/application/categories/useCategories"
import { useContractTemplates } from "@/application/contractTemplates/useContractTemplates"
import { useDeleteContractTemplate } from "@/application/contractTemplates/useDeleteContractTemplate"
import { useActivateContractTemplate } from "@/application/contractTemplates/useActivateContractTemplate"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import type { ContractTemplate } from "@/domain/types/contractTemplate"

const ALL_CATEGORIES = "__all__"

/** Mantenimiento de plantillas de contrato: una por Categoría puede estar "activa" a la vez. */
export function ContractTemplatesPage() {
  const { t } = useTranslation("contractTemplates")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.ContractsManage)

  const { data: categories } = useCategories()
  const { data: templates, isLoading } = useContractTemplates()
  const deleteTemplate = useDeleteContractTemplate()
  const activateTemplate = useActivateContractTemplate()

  const [categoryFilter, setCategoryFilter] = useState(ALL_CATEGORIES)
  const [formOpen, setFormOpen] = useState(false)
  const [editingTemplate, setEditingTemplate] = useState<ContractTemplate | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<ContractTemplate | null>(null)

  const filteredTemplates = useMemo(() => {
    if (!templates) return templates
    if (categoryFilter === ALL_CATEGORIES) return templates
    return templates.filter((t) => String(t.categoryId) === categoryFilter)
  }, [templates, categoryFilter])

  const { page, setPage, pageCount, paginated: paginatedTemplates, totalCount } = usePagination(
    filteredTemplates,
    10
  )

  useEffect(() => {
    setPage(1)
  }, [categoryFilter, setPage])

  function handleCreate() {
    setEditingTemplate(null)
    setFormOpen(true)
  }

  function handleEdit(template: ContractTemplate) {
    setEditingTemplate(template)
    setFormOpen(true)
  }

  function handleActivate(template: ContractTemplate) {
    activateTemplate.mutate(template.id, {
      onSuccess: () => toast.success(t("toast.activateSuccess")),
      onError: () => toast.error(t("toast.activateError")),
    })
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return

    deleteTemplate.mutate(deleteTarget.id, {
      onSuccess: () => toast.success(t("toast.deleteSuccess")),
      onError: () => toast.error(t("toast.deleteError")),
    })
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("subtitle")}</p>
        </div>
        {canManage && (
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4" />
            {t("newButton")}
          </Button>
        )}
      </div>

      <div className="max-w-xs">
        <Select value={categoryFilter} onValueChange={setCategoryFilter}>
          <SelectTrigger className="w-full">
            <SelectValue placeholder={t("filter.allCategories")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_CATEGORIES}>{t("filter.allCategories")}</SelectItem>
            {categories?.map((category) => (
              <SelectItem key={category.id} value={String(category.id)}>
                {category.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.category")}</TableHead>
              <TableHead>{t("table.headers.name")}</TableHead>
              <TableHead>{t("table.headers.status")}</TableHead>
              <TableHead>{t("table.headers.created")}</TableHead>
              <TableHead className="w-[160px] text-right">
                {t("table.headers.actions")}
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

            {!isLoading && filteredTemplates?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  {t("table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedTemplates.map((template) => (
              <TableRow key={template.id}>
                <TableCell className="text-muted-foreground">{template.categoryName}</TableCell>
                <TableCell className="font-medium">{template.name}</TableCell>
                <TableCell>
                  <Badge variant={template.isActive ? "default" : "secondary"}>
                    {template.isActive ? t("table.statusActive") : t("table.statusInactive")}
                  </Badge>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {new Date(template.createdAt).toLocaleDateString()}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    {canManage && !template.isActive && (
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        title={t("actions.activate")}
                        onClick={() => handleActivate(template)}
                        disabled={activateTemplate.isPending}
                      >
                        <CheckCircle2 className="h-4 w-4 text-primary" />
                      </Button>
                    )}
                    {canManage && (
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        onClick={() => handleEdit(template)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    )}
                    {canManage && (
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        onClick={() => setDeleteTarget(template)}
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    )}
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

      <ContractTemplateFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        template={editingTemplate}
        defaultCategoryId={categoryFilter === ALL_CATEGORIES ? null : Number(categoryFilter)}
      />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("deleteDialog.title")}
        description={t("deleteDialog.description", { name: deleteTarget?.name })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
