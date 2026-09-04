import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useAssets } from "@/application/assets/useAssets"
import { useCustomers } from "@/application/customers/useCustomers"
import { useCreateRental } from "@/application/rentals/useCreateRental"

function buildRentalFormSchema(t: TFunction) {
  return z
    .object({
      assetId: z.string().min(1, t("validation.assetRequired")),
      customerId: z.string().min(1, t("validation.customerRequired")),
      startDate: z.string().min(1, t("validation.startDateRequired")),
      endDate: z.string().min(1, t("validation.endDateRequired")),
      totalPrice: z
        .string()
        .min(1, t("validation.priceRequired"))
        .refine(
          (val) => !Number.isNaN(Number(val)),
          t("validation.priceInvalid")
        )
        .refine((val) => Number(val) >= 0, t("validation.priceNegative")),
    })
    .refine((data) => new Date(data.endDate) > new Date(data.startDate), {
      message: t("validation.endDateAfterStart"),
      path: ["endDate"],
    })
}

type RentalFormValues = z.infer<ReturnType<typeof buildRentalFormSchema>>

interface RentalFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Diálogo de creación de rentas (asigna un cliente a un activo disponible). */
export function RentalFormDialog({ open, onOpenChange }: RentalFormDialogProps) {
  const { t } = useTranslation(["rentals", "common"])
  const { data: assets } = useAssets()
  const { data: customers } = useCustomers()
  const createRental = useCreateRental()

  const availableAssets = assets?.filter((asset) => asset.status === "Available")

  const rentalFormSchema = useMemo(() => buildRentalFormSchema(t), [t])

  const form = useForm<RentalFormValues>({
    resolver: zodResolver(rentalFormSchema),
    defaultValues: {
      assetId: "",
      customerId: "",
      startDate: "",
      endDate: "",
      totalPrice: "0",
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        assetId: "",
        customerId: "",
        startDate: "",
        endDate: "",
        totalPrice: "0",
      })
    }
  }, [open, form])

  function onSubmit(values: RentalFormValues) {
    createRental.mutate(
      {
        assetId: values.assetId,
        customerId: values.customerId,
        startDate: values.startDate,
        endDate: values.endDate,
        totalPrice: Number(values.totalPrice),
      },
      {
        onSuccess: () => {
          toast.success(t("toast.created"))
          onOpenChange(false)
        },
        onError: () => {
          toast.error(t("toast.createError"))
        },
      }
    )
  }

  const isPending = createRental.isPending
  const hasAvailableAssets = (availableAssets?.length ?? 0) > 0
  const hasCustomers = (customers?.length ?? 0) > 0
  const canSubmit = hasAvailableAssets && hasCustomers

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("dialog.createTitle")}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="assetId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.asset")}</FormLabel>
                  <Select
                    onValueChange={field.onChange}
                    value={field.value}
                    disabled={!hasAvailableAssets}
                  >
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue
                          placeholder={
                            hasAvailableAssets
                              ? t("dialog.fields.assetPlaceholder")
                              : t("dialog.fields.assetPlaceholderEmpty")
                          }
                        />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {availableAssets?.map((asset) => (
                        <SelectItem key={asset.id} value={asset.id}>
                          {asset.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="customerId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.customer")}</FormLabel>
                  <Select
                    onValueChange={field.onChange}
                    value={field.value}
                    disabled={!hasCustomers}
                  >
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue
                          placeholder={
                            hasCustomers
                              ? t("dialog.fields.customerPlaceholder")
                              : t("dialog.fields.customerPlaceholderEmpty")
                          }
                        />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {customers?.map((customer) => (
                        <SelectItem key={customer.id} value={customer.id}>
                          {customer.fullName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="startDate"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.startDate")}</FormLabel>
                    <FormControl>
                      <Input type="date" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="endDate"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.endDate")}</FormLabel>
                    <FormControl>
                      <Input type="date" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="totalPrice"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.totalPrice")}</FormLabel>
                  <FormControl>
                    <Input type="number" step="0.01" min="0" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
              >
                {t("common:buttons.cancel")}
              </Button>
              <Button type="submit" disabled={isPending || !canSubmit}>
                {isPending ? t("common:status.saving") : t("common:buttons.save")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
