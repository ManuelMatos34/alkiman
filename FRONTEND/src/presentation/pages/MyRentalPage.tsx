import { useMemo, useState } from "react"
import { useParams } from "react-router-dom"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { CalendarClock, CheckCircle2, FileText, XCircle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { MyRentalExtensionDialog } from "@/presentation/components/MyRentalExtensionDialog"
import { MyRentalCancellationDialog } from "@/presentation/components/MyRentalCancellationDialog"
import { useVerifyMyRental } from "@/application/myRental/useVerifyMyRental"
import type { MyRentalDetails } from "@/domain/types/myRental"
import { PublicAppearanceSync } from "@/presentation/components/PublicAppearanceSync"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

function buildIdentityFormSchema(t: TFunction) {
  return z.object({
    identifier: z
      .string()
      .trim()
      .min(1, t("validation.identifierRequired")),
  })
}

type IdentityFormValues = z.infer<ReturnType<typeof buildIdentityFormSchema>>

function getErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError(error) && typeof error.response?.data?.detail === "string") {
    return error.response.data.detail
  }
  return fallback
}

/** Link público de autogestión de una renta: el cliente verifica su identidad y puede pedir prórroga o cancelación anticipada. */
export function MyRentalPage() {
  const { t, i18n } = useTranslation("myRental")
  const { token } = useParams<{ token: string }>()
  const verifyMyRental = useVerifyMyRental(token)

  const [identifier, setIdentifier] = useState<string | null>(null)
  const [details, setDetails] = useState<MyRentalDetails | null>(null)
  const [verifyError, setVerifyError] = useState<string | null>(null)

  const [extensionOpen, setExtensionOpen] = useState(false)
  const [cancellationOpen, setCancellationOpen] = useState(false)
  const [requestSent, setRequestSent] = useState(false)

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
      }),
    [i18n.language]
  )

  const dateFormatter = useMemo(
    () => new Intl.DateTimeFormat(getIntlLocale(i18n.language), { dateStyle: "medium" }),
    [i18n.language]
  )

  const identityFormSchema = useMemo(() => buildIdentityFormSchema(t), [t])

  const form = useForm<IdentityFormValues>({
    resolver: zodResolver(identityFormSchema),
    defaultValues: { identifier: "" },
  })

  async function onSubmitIdentity(values: IdentityFormValues) {
    setVerifyError(null)
    try {
      const response = await verifyMyRental.mutateAsync({ identifier: values.identifier })
      setDetails(response)
      setIdentifier(values.identifier)
    } catch (error) {
      setVerifyError(getErrorMessage(error, t("errors.verifyFailed")))
    }
  }

  async function refreshDetails() {
    if (!identifier) return
    try {
      const response = await verifyMyRental.mutateAsync({ identifier })
      setDetails(response)
    } catch {
      // Si el refetch falla, dejamos los datos previos: el pedido ya se envió igual.
    }
  }

  function handleExtensionRequested() {
    setExtensionOpen(false)
    setRequestSent(true)
    void refreshDetails()
  }

  function handleCancellationRequested() {
    setCancellationOpen(false)
    setRequestSent(true)
    void refreshDetails()
  }

  if (!token) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-4 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">{t("invalidLink.title")}</h1>
        <p className="text-sm text-muted-foreground">{t("invalidLink.description")}</p>
      </div>
    )
  }

  if (!details) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <Card className="w-full max-w-sm">
          <CardHeader className="items-center text-center">
            <CardTitle className="text-xl">{t("identity.title")}</CardTitle>
            <p className="text-sm text-muted-foreground">{t("identity.description")}</p>
          </CardHeader>
          <CardContent>
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmitIdentity)} className="space-y-4">
                <FormField
                  control={form.control}
                  name="identifier"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("identity.label")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("identity.placeholder")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                {verifyError && <p className="text-sm text-destructive">{verifyError}</p>}

                <Button type="submit" className="w-full" disabled={verifyMyRental.isPending}>
                  {verifyMyRental.isPending ? t("identity.verifying") : t("common:buttons.continue")}
                </Button>
              </form>
            </Form>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background px-4 py-10">
      <PublicAppearanceSync themeMode={details.themeMode} accentColor={details.accentColor} />
      <div className="mx-auto max-w-lg space-y-4">
        <div className="text-center">
          <h1 className="text-2xl font-semibold tracking-tight">{t("details.title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("details.subtitle")}</p>
        </div>

        <Card>
          <CardHeader className="flex-row items-start justify-between space-y-0">
            <div>
              <CardTitle className="text-lg">{details.assetName}</CardTitle>
              {details.assetDescription && (
                <p className="mt-1 text-sm text-muted-foreground">{details.assetDescription}</p>
              )}
            </div>
            <Badge variant={details.status === "Overdue" ? "destructive" : "default"}>
              {t(`status.${details.status}`, { defaultValue: details.status })}
            </Badge>
          </CardHeader>
          <CardContent className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t("details.startDate")}</span>
              <span>{dateFormatter.format(new Date(details.startDate))}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t("details.endDate")}</span>
              <span>{dateFormatter.format(new Date(details.endDate))}</span>
            </div>
            <div className="flex justify-between font-medium">
              <span>{t("details.totalPrice")}</span>
              <span>{currencyFormatter.format(details.totalPrice)}</span>
            </div>
            {details.contractPdfUrl && (
              <div className="flex items-center gap-1.5 pt-1 text-muted-foreground">
                <FileText className="h-4 w-4" />
                <span>{t("details.contractAvailable")}</span>
              </div>
            )}
          </CardContent>
        </Card>

        {requestSent && !details.pendingRequest && (
          <div className="flex items-start gap-2 rounded-lg border border-border/60 bg-muted/40 p-3 text-sm text-muted-foreground">
            <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
            <span>{t("requestSent")}</span>
          </div>
        )}

        {details.pendingRequest ? (
          <div className="flex items-start gap-2 rounded-lg border border-border/60 bg-muted/40 p-3 text-sm text-muted-foreground">
            <CalendarClock className="mt-0.5 h-4 w-4 shrink-0" />
            <span>
              {t("pendingRequest", {
                type: t(`requestType.${details.pendingRequest.type}`, {
                  defaultValue: details.pendingRequest.type,
                }),
                date: dateFormatter.format(new Date(details.pendingRequest.createdAt)),
              })}
            </span>
          </div>
        ) : (
          (details.canRequestExtension || details.canRequestCancellation) && (
            <div className="flex flex-col gap-2 sm:flex-row">
              {details.canRequestExtension && (
                <Button className="flex-1" onClick={() => setExtensionOpen(true)}>
                  <CalendarClock className="h-4 w-4" />
                  {t("actions.requestExtension")}
                </Button>
              )}
              {details.canRequestCancellation && (
                <Button
                  variant="outline"
                  className="flex-1"
                  onClick={() => setCancellationOpen(true)}
                >
                  <XCircle className="h-4 w-4" />
                  {t("actions.requestCancellation")}
                </Button>
              )}
            </div>
          )
        )}
      </div>

      <MyRentalExtensionDialog
        open={extensionOpen}
        onOpenChange={setExtensionOpen}
        token={token}
        identifier={identifier ?? ""}
        onRequested={handleExtensionRequested}
      />
      <MyRentalCancellationDialog
        open={cancellationOpen}
        onOpenChange={setCancellationOpen}
        token={token}
        identifier={identifier ?? ""}
        onRequested={handleCancellationRequested}
      />
    </div>
  )
}
