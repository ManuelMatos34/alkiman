import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, Copy, ExternalLink, CheckCircle2, XCircle, Trash2 } from "lucide-react"
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
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { BarbershopPortalLinkFormDialog } from "@/presentation/components/BarbershopPortalLinkFormDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import {
  useBarbershopPortalLinks,
  useSetBarbershopPortalLinkActive,
  useDeleteBarbershopPortalLink,
} from "@/application/barbershop/useBarbershopPortalLinks"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import type { BarbershopPortalLink } from "@/domain/types/barbershop"

function portalUrl(slug: string) {
  return `${window.location.origin}/barberia/${slug}`
}

export function BarbershopPortalLinksPage() {
  const { t } = useTranslation(["barbershop", "common"])
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.BarbershopPortalManage)

  const { data: portalLinks, isLoading } = useBarbershopPortalLinks()
  const setActive = useSetBarbershopPortalLinkActive()
  const deleteLink = useDeleteBarbershopPortalLink()

  const { page, setPage, pageCount, paginated, totalCount } = usePagination(portalLinks, 10)

  const [formOpen, setFormOpen] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<BarbershopPortalLink | null>(null)

  function handleConfirmDelete() {
    if (!deleteTarget) return
    deleteLink.mutate(deleteTarget.id, {
      onSuccess: () => {
        toast.success(t("toast.deleteSuccess"))
        setDeleteTarget(null)
      },
      onError: () => {
        toast.error(t("toast.deleteError"))
        setDeleteTarget(null)
      },
    })
  }

  async function handleCopy(link: BarbershopPortalLink) {
    try {
      await navigator.clipboard.writeText(portalUrl(link.slug))
      toast.success(t("common:labels.copied"))
    } catch {
      toast.error(t("common:labels.copyFailed"))
    }
  }

  function handleToggleActive(link: BarbershopPortalLink) {
    setActive.mutate(
      { id: link.id, isActive: !link.isActive },
      {
        onSuccess: () => toast.success(t("toast.updateSuccess")),
        onError: () => toast.error(t("toast.updateError")),
      }
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("portalLinks.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("portalLinks.subtitle")}</p>
        </div>
        {canManage && (
          <Button onClick={() => setFormOpen(true)}>
            <Plus className="h-4 w-4" />
            {t("portalLinks.newButton")}
          </Button>
        )}
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("portalLinks.table.headers.title")}</TableHead>
              <TableHead>{t("portalLinks.table.headers.stylist")}</TableHead>
              <TableHead>{t("portalLinks.table.headers.slug")}</TableHead>
              <TableHead>{t("portalLinks.table.headers.status")}</TableHead>
              <TableHead className="w-[180px] text-right">
                {t("portalLinks.table.headers.actions")}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={5}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && portalLinks?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  {t("portalLinks.table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginated.map((link) => (
              <TableRow key={link.id}>
                <TableCell className="font-medium">{link.title}</TableCell>
                <TableCell className="text-muted-foreground">
                  {link.stylistName ?? t("portalLinks.table.noStylist")}
                </TableCell>
                <TableCell className="font-mono text-sm text-muted-foreground">
                  {link.slug}
                </TableCell>
                <TableCell>
                  <Badge variant={link.isActive ? "default" : "secondary"}>
                    {link.isActive
                      ? t("portalLinks.table.statusActive")
                      : t("portalLinks.table.statusInactive")}
                  </Badge>
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => handleCopy(link)}
                    >
                      <Copy className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon-sm" asChild>
                      <a href={portalUrl(link.slug)} target="_blank" rel="noreferrer">
                        <ExternalLink className="h-4 w-4" />
                      </a>
                    </Button>
                    {canManage && (
                      <>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          disabled={setActive.isPending}
                          onClick={() => handleToggleActive(link)}
                        >
                          {link.isActive ? (
                            <XCircle className="h-4 w-4 text-destructive" />
                          ) : (
                            <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                          )}
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          onClick={() => setDeleteTarget(link)}
                        >
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </>
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

      <BarbershopPortalLinkFormDialog open={formOpen} onOpenChange={setFormOpen} />

      <AlertDialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("portalLinks.deleteDialog.title")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("portalLinks.deleteDialog.description", { title: deleteTarget?.title })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("common:buttons.cancel")}</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={handleConfirmDelete}
              disabled={deleteLink.isPending}
            >
              {t("common:buttons.delete")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
