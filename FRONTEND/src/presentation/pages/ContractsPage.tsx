import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { FileText, PenLine, Loader2, Mail, MailX, Send } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { SignaturePad } from "@/presentation/components/SignaturePad"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useContracts } from "@/application/contracts/useContracts"
import { useContractPdf } from "@/application/contracts/useContractPdf"
import { useSignContract } from "@/application/contracts/useSignContract"
import { useResendContractEmail } from "@/application/contracts/useResendContractEmail"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"
import type { Contract, ContractStatus } from "@/domain/types/contract"

const statusVariants: Record<ContractStatus, "default" | "secondary"> = {
  Pending: "secondary",
  Signed: "default",
}

/** Listado de contratos generados automáticamente por cada renta: visualización del PDF y firma manual de los pendientes. */
export function ContractsPage() {
  const { t } = useTranslation("contracts")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PermissionCodes.ContractsManage)

  const statusLabels: Record<ContractStatus, string> = {
    Pending: t("status.pending"),
    Signed: t("status.signed"),
  }

  const { data: contracts, isLoading } = useContracts()
  const signContract = useSignContract()
  const resendContractEmail = useResendContractEmail()
  const { page, setPage, pageCount, paginated: paginatedContracts, totalCount } = usePagination(contracts, 10)

  const [pdfContract, setPdfContract] = useState<Contract | null>(null)
  const [signTarget, setSignTarget] = useState<Contract | null>(null)

  const { url: pdfUrl, isLoading: pdfLoading, isError: pdfError } = useContractPdf(pdfContract?.id)

  function handleSign(base64Png: string) {
    if (!signTarget) return

    signContract.mutate(
      { id: signTarget.id, request: { signatureImageBase64: base64Png } },
      {
        onSuccess: () => {
          toast.success(t("toast.signSuccess"))
          setSignTarget(null)
        },
        onError: () => toast.error(t("toast.signError")),
      }
    )
  }

  function handleResendEmail(contract: Contract) {
    resendContractEmail.mutate(contract.id, {
      onSuccess: () => toast.success(t("toast.resendSuccess")),
      onError: () => toast.error(t("toast.resendError")),
    })
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("subtitle")}</p>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.customer")}</TableHead>
              <TableHead>{t("table.headers.template")}</TableHead>
              <TableHead>{t("table.headers.status")}</TableHead>
              <TableHead>{t("table.headers.email")}</TableHead>
              <TableHead>{t("table.headers.created")}</TableHead>
              <TableHead className="w-[140px] text-right">
                {t("table.headers.actions")}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 3 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={6}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && contracts?.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                  {t("table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedContracts.map((contract) => (
              <TableRow key={contract.id}>
                <TableCell className="font-medium">{contract.customerName}</TableCell>
                <TableCell className="text-muted-foreground">
                  {contract.templateName ?? t("table.templateFallback")}
                </TableCell>
                <TableCell>
                  <Badge variant={statusVariants[contract.status]}>
                    {statusLabels[contract.status]}
                  </Badge>
                </TableCell>
                <TableCell>
                  {contract.emailSent ? (
                    <Mail className="h-4 w-4 text-primary" aria-label={t("email.sent")} />
                  ) : (
                    <MailX className="h-4 w-4 text-muted-foreground" aria-label={t("email.notSent")} />
                  )}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {new Date(contract.createdAt).toLocaleDateString()}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      title={t("actions.viewPdf")}
                      onClick={() => setPdfContract(contract)}
                    >
                      <FileText className="h-4 w-4" />
                    </Button>
                    {canManage && contract.status === "Pending" && (
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        title={t("actions.sign")}
                        onClick={() => setSignTarget(contract)}
                      >
                        <PenLine className="h-4 w-4" />
                      </Button>
                    )}
                    {canManage && (
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        title={t("actions.resendEmail")}
                        disabled={resendContractEmail.isPending}
                        onClick={() => handleResendEmail(contract)}
                      >
                        <Send className="h-4 w-4" />
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

      <Dialog open={!!pdfContract} onOpenChange={(open) => !open && setPdfContract(null)}>
        <DialogContent className="flex h-[85vh] flex-col sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>{t("pdfDialog.title", { name: pdfContract?.customerName })}</DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-hidden rounded-lg border border-border/60">
            {pdfLoading && (
              <div className="flex h-full items-center justify-center">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            )}
            {pdfError && (
              <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                {t("pdfDialog.loadError")}
              </div>
            )}
            {pdfUrl && !pdfLoading && !pdfError && (
              <iframe src={pdfUrl} title={t("pdfDialog.iframeTitle")} className="h-full w-full" />
            )}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={!!signTarget} onOpenChange={(open) => !open && setSignTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("signDialog.title", { name: signTarget?.customerName })}</DialogTitle>
          </DialogHeader>
          <SignaturePad onSave={handleSign} />
        </DialogContent>
      </Dialog>
    </div>
  )
}
