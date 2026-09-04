import { useMemo, useState } from "react"
import { useTranslation } from "react-i18next"
import { toast } from "sonner"
import { Check, X } from "lucide-react"
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
import { ReviewRentalRequestDialog } from "@/presentation/components/ReviewRentalRequestDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useRentalRequests } from "@/application/rentalRequests/useRentalRequests"
import { useApproveRentalRequest } from "@/application/rentalRequests/useApproveRentalRequest"
import { useRejectRentalRequest } from "@/application/rentalRequests/useRejectRentalRequest"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { RentalRequest, RentalRequestStatus, RentalRequestType } from "@/domain/types/rentalRequest"

const typeVariants: Record<RentalRequestType, "default" | "secondary"> = {
  Extension: "default",
  Cancellation: "secondary",
}

const statusVariants: Record<RentalRequestStatus, "secondary" | "default" | "destructive"> = {
  Pending: "secondary",
  Approved: "default",
  Rejected: "destructive",
}

/** Bandeja de pedidos de prórroga/cancelación que los clientes hacen desde su link público `/mi-renta/{token}`. */
export function RentalRequestsPage() {
  const { t, i18n } = useTranslation("rentalRequests")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.RentalRequestsManage)

  const { data: requests, isLoading } = useRentalRequests()
  const approveRequest = useApproveRentalRequest()
  const rejectRequest = useRejectRentalRequest()
  const { page, setPage, pageCount, paginated: paginatedRequests, totalCount } = usePagination(requests, 10)

  const [reviewTarget, setReviewTarget] = useState<RentalRequest | null>(null)
  const [reviewAction, setReviewAction] = useState<"approve" | "reject">("approve")

  const dateFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      }),
    [i18n.language]
  )

  function openReview(request: RentalRequest, action: "approve" | "reject") {
    setReviewTarget(request)
    setReviewAction(action)
  }

  function handleConfirm(staffNote: string | null) {
    if (!reviewTarget) return

    const mutation = reviewAction === "approve" ? approveRequest : rejectRequest
    mutation.mutate(
      { id: reviewTarget.id, payload: { staffNote } },
      {
        onSuccess: () => {
          toast.success(
            reviewAction === "approve" ? t("toast.approved") : t("toast.rejected")
          )
          setReviewTarget(null)
        },
        onError: () => toast.error(t("toast.error")),
      }
    )
  }

  const isPending = approveRequest.isPending || rejectRequest.isPending

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("page.title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("page.subtitle")}</p>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.asset")}</TableHead>
              <TableHead>{t("table.headers.customer")}</TableHead>
              <TableHead>{t("table.headers.type")}</TableHead>
              <TableHead>{t("table.headers.detail")}</TableHead>
              <TableHead>{t("table.headers.status")}</TableHead>
              <TableHead>{t("table.headers.created")}</TableHead>
              {canManage && (
                <TableHead className="w-[110px] text-right">{t("table.headers.actions")}</TableHead>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={canManage ? 7 : 6}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && requests?.length === 0 && (
              <TableRow>
                <TableCell colSpan={canManage ? 7 : 6} className="h-24 text-center text-muted-foreground">
                  {t("table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedRequests.map((request) => (
              <TableRow key={request.id}>
                <TableCell className="font-medium">{request.assetName}</TableCell>
                <TableCell className="text-muted-foreground">{request.customerName}</TableCell>
                <TableCell>
                  <Badge variant={typeVariants[request.type]}>{t(`type.${request.type}`)}</Badge>
                </TableCell>
                <TableCell className="max-w-[280px] truncate text-muted-foreground">
                  {request.type === "Extension"
                    ? t("table.extensionDetail", { count: request.requestedPeriods ?? "—" })
                    : request.reason ?? "—"}
                </TableCell>
                <TableCell>
                  <Badge variant={statusVariants[request.status]}>
                    {t(`status.${request.status}`)}
                  </Badge>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {dateFormatter.format(new Date(request.createdAt))}
                </TableCell>
                {canManage && (
                  <TableCell className="text-right">
                    {request.status === "Pending" && (
                      <div className="flex justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          title={t("actions.approve")}
                          onClick={() => openReview(request, "approve")}
                        >
                          <Check className="h-4 w-4 text-primary" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          title={t("actions.reject")}
                          onClick={() => openReview(request, "reject")}
                        >
                          <X className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    )}
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

      <ReviewRentalRequestDialog
        open={!!reviewTarget}
        onOpenChange={(open) => !open && setReviewTarget(null)}
        action={reviewAction}
        isPending={isPending}
        description={
          reviewTarget
            ? t("dialog.description", {
                type: t(`type.${reviewTarget.type}`),
                asset: reviewTarget.assetName,
                customer: reviewTarget.customerName,
              })
            : ""
        }
        onConfirm={handleConfirm}
      />
    </div>
  )
}
