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
import { CarwashPortalLinkFormDialog } from "@/presentation/components/CarwashPortalLinkFormDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useCarwashPortalLinks } from "@/application/carwash/useCarwashPortalLinks"
import { useSetPortalLinkActive } from "@/application/carwash/useSetPortalLinkActive"
import { useDeletePortalLink } from "@/application/carwash/useDeletePortalLink"
import type { CarwashPortalLink } from "@/domain/types/carwash"

function portalUrl(slug: string) {
  return `${window.location.origin}/lavado/${slug}`
}

/** Administración de links públicos de Carwash: cada uno permite a un cliente auto-registrarse en la cola. */
export function CarwashPortalLinksPage() {
  const { t } = useTranslation(["carwash", "common"])
  const { data: portalLinks, isLoading } = useCarwashPortalLinks()
  const setPortalLinkActive = useSetPortalLinkActive()
  const { page, setPage, pageCount, paginated: paginatedLinks, totalCount } = usePagination(
    portalLinks,
    10
  )

  const deletePortalLink = useDeletePortalLink()

  const [formOpen, setFormOpen] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<CarwashPortalLink | null>(null)

  function handleConfirmDelete() {
    if (!deleteTarget) return
    deletePortalLink.mutate(deleteTarget.id, {
      onSuccess: () => {
        toast.success(t("portalLinks.toast.deleteSuccess"))
        setDeleteTarget(null)
      },
      onError: (err: unknown) => {
        const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
        toast.error(msg ?? t("portalLinks.toast.deleteError"))
        setDeleteTarget(null)
      },
    })
  }

  async function handleCopy(link: CarwashPortalLink) {
    try {
      await navigator.clipboard.writeText(portalUrl(link.slug))
      toast.success(t("portalLinks.toast.copySuccess"))
    } catch {
      toast.error(t("portalLinks.toast.copyError"))
    }
  }

  function handleToggleActive(link: CarwashPortalLink) {
    setPortalLinkActive.mutate(
      { id: link.id, isActive: !link.isActive },
      {
        onSuccess: () =>
          toast.success(
            link.isActive
              ? t("portalLinks.toast.deactivateSuccess")
              : t("portalLinks.toast.activateSuccess")
          ),
        onError: () => toast.error(t("portalLinks.toast.toggleError")),
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
        <Button onClick={() => setFormOpen(true)}>
          <Plus className="h-4 w-4" />
          {t("portalLinks.newButton")}
        </Button>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("portalLinks.table.headers.title")}</TableHead>
              <TableHead>{t("portalLinks.table.headers.status")}</TableHead>
              <TableHead className="w-[180px] text-right">
                {t("portalLinks.table.headers.actions")}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={3}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && portalLinks?.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                  {t("portalLinks.table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedLinks.map((link) => (
              <TableRow key={link.id}>
                <TableCell className="font-medium">{link.title}</TableCell>
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
                      title={t("portalLinks.actions.copy")}
                      onClick={() => handleCopy(link)}
                    >
                      <Copy className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon-sm" title={t("portalLinks.actions.open")} asChild>
                      <a href={portalUrl(link.slug)} target="_blank" rel="noreferrer">
                        <ExternalLink className="h-4 w-4" />
                      </a>
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      title={
                        link.isActive
                          ? t("portalLinks.actions.deactivate")
                          : t("portalLinks.actions.activate")
                      }
                      disabled={setPortalLinkActive.isPending}
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
                      title={t("portalLinks.actions.delete")}
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

      <CarwashPortalLinkFormDialog open={formOpen} onOpenChange={setFormOpen} />

      <AlertDialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("portalLinks.deleteDialog.title")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("portalLinks.deleteDialog.description", { title: deleteTarget?.title })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("common:cancel")}</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={handleConfirmDelete}
              disabled={deletePortalLink.isPending}
            >
              {t("portalLinks.deleteDialog.confirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
