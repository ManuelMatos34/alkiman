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
import { useCreateCustomer } from "@/application/customers/useCreateCustomer"
import { useUpdateCustomer } from "@/application/customers/useUpdateCustomer"
import type { Customer } from "@/domain/types/customer"

function buildCustomerFormSchema(t: TFunction) {
  return z.object({
    fullName: z
      .string()
      .trim()
      .min(1, t("validation.fullNameRequired"))
      .max(150, t("validation.fullNameMax")),
    identityNumber: z
      .string()
      .trim()
      .min(1, t("validation.identityNumberRequired"))
      .max(50, t("validation.identityNumberMax")),
    phone: z.string().trim().optional(),
    email: z
      .string()
      .trim()
      .optional()
      .refine(
        (val) => !val || z.string().email().safeParse(val).success,
        t("validation.emailInvalid")
      ),
  })
}

type CustomerFormValues = z.infer<ReturnType<typeof buildCustomerFormSchema>>

interface CustomerFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  customer?: Customer | null
}

/** Diálogo de creación/edición de clientes (inquilinos). */
export function CustomerFormDialog({
  open,
  onOpenChange,
  customer,
}: CustomerFormDialogProps) {
  const { t } = useTranslation(["customers", "common"])
  const isEditing = !!customer
  const createCustomer = useCreateCustomer()
  const updateCustomer = useUpdateCustomer()

  const customerFormSchema = useMemo(() => buildCustomerFormSchema(t), [t])

  const form = useForm<CustomerFormValues>({
    resolver: zodResolver(customerFormSchema),
    defaultValues: {
      fullName: "",
      identityNumber: "",
      phone: "",
      email: "",
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        fullName: customer?.fullName ?? "",
        identityNumber: customer?.identityNumber ?? "",
        phone: customer?.phone ?? "",
        email: customer?.email ?? "",
      })
    }
  }, [open, customer, form])

  function onSubmit(values: CustomerFormValues) {
    const common = {
      fullName: values.fullName,
      identityNumber: values.identityNumber,
      phone: values.phone?.length ? values.phone : null,
      email: values.email?.length ? values.email : null,
    }

    if (customer) {
      updateCustomer.mutate(
        { id: customer.id, request: common },
        {
          onSuccess: () => {
            toast.success(t("toast.updated"))
            onOpenChange(false)
          },
          onError: () => {
            toast.error(t("toast.updateError"))
          },
        }
      )
      return
    }

    createCustomer.mutate(common, {
      onSuccess: () => {
        toast.success(t("toast.created"))
        onOpenChange(false)
      },
      onError: () => {
        toast.error(t("toast.createError"))
      },
    })
  }

  const isPending = createCustomer.isPending || updateCustomer.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("dialog.editTitle") : t("dialog.createTitle")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="fullName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.fullName")}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder={t("dialog.fields.fullNamePlaceholder")}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="identityNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.identityNumber")}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder={t("dialog.fields.identityNumberPlaceholder")}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="phone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.phone")}</FormLabel>
                    <FormControl>
                      <Input
                        placeholder={t("dialog.fields.phonePlaceholder")}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.email")}</FormLabel>
                    <FormControl>
                      <Input
                        placeholder={t("dialog.fields.emailPlaceholder")}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
              >
                {t("common:buttons.cancel")}
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending ? t("common:status.saving") : t("common:buttons.save")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
