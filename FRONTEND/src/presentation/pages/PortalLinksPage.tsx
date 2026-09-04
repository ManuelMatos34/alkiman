import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, Pencil, Trash2, Copy, ExternalLink } from "lucide-react"
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
import { PortalLinkFormDialog } from "@/presentation/components/PortalLinkFormDialog"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { usePortalLinks } from "@/application/portalLinks/usePortalLinks"
import { useDeletePortalLink } from "@/application/portalLinks/useDeletePortalLink"
import type { PortalLink } from "@/domain/types/portal"

function portalUrl(slug: string) {
  return `${window.location.origin}/p/${slug}`
}

/** Administración de links públicos del Portal de Rentas: cada uno muestra los activos de un Grupo de Activos y permite auto-rentar sin login. */
export function PortalLinksPage() {
  const { t } = useTranslation(["portalLinks", "common"])
  const { data: portalLinks, isLoading } = usePortalLinks()
  const deletePortalLink = useDeletePortalLink()
  const { page, setPage, pageCount, paginated: paginatedLinks, totalCount } = usePagination(portalLinks, 10)

  const [formOpen, setFormOpen] = useState(false)
  const [editingLink, setEditingLink] = useState<PortalLink | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<PortalLink | null>(null)

  function handleCreate() {
    setEditingLink(null)
    setFormOpen(true)
  }

  function handleEdit(link: PortalLink) {
    setEditingLink(link)
    setFormOpen(true)
  }

  async function handleCopy(link: PortalLink) {
    try {
      await navigator.clipboard.writeText(portalUrl(link.slug))
      toast.success(t("toast.copySuccess"))
    } catch {
      toast.error(t("toast.copyError"))
    }
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return

    deletePortalLink.mutate(deleteTarget.id, {
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
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4" />
          {t("newButton")}
        </Button>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.title")}</TableHead>
              <TableHead>{t("table.headers.assetGroup")}</TableHead>
              <TableHead>{t("table.headers.status")}</TableHead>
              <TableHead className="w-[160px] text-right">
                {t("table.headers.actions")}
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

            {!isLoading && portalLinks?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
                  {t("table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedLinks.map((link) => (
              <TableRow key={link.id}>
                <TableCell className="font-medium">{link.title}</TableCell>
                <TableCell className="text-muted-foreground">
                  {link.assetGroupName}
                </TableCell>
                <TableCell>
                  <Badge variant={link.isActive ? "default" : "secondary"}>
                    {link.isActive ? t("table.statusActive") : t("table.statusInactive")}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      title={t("actions.copy")}
                      onClick={() => handleCopy(link)}
                    >
                      <Copy className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon-sm" title={t("actions.open")} asChild>
                      <a href={portalUrl(link.slug)} target="_blank" rel="noreferrer">
                        <ExternalLink className="h-4 w-4" />
                      </a>
                    </Button>
                    <Button variant="ghost" size="icon-sm" onClick={() => handleEdit(link)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => setDeleteTarget(link)}
                    >
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

      <PortalLinkFormDialog open={formOpen} onOpenChange={setFormOpen} portalLink={editingLink} />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("deleteDialog.title")}
        description={t("deleteDialog.description", { title: deleteTarget?.title })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
