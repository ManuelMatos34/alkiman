import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
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
import { useCustomers } from "@/application/customers/useCustomers"
import { useCreateReminder } from "@/application/reminders/useCreateReminder"
import { useUpdateReminder } from "@/application/reminders/useUpdateReminder"
import type { Reminder, ReminderStatus } from "@/domain/types/reminder"

const NO_CUSTOMER = "__none__"

function buildReminderFormSchema(t: (key: string) => string) {
  return z.object({
    title: z
      .string()
      .trim()
      .min(1, t("reminders.form.validation.titleRequired"))
      .max(150, t("reminders.form.validation.titleMax")),
    message: z.string().trim().max(1000, t("reminders.form.validation.messageMax")).optional(),
    remindAt: z.string().min(1, t("reminders.form.validation.dateRequired")),
    customerId: z.string(),
    status: z.enum(["Pending", "Completed", "Cancelled"]),
  })
}

type ReminderFormValues = z.infer<ReturnType<typeof buildReminderFormSchema>>

interface ReminderFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  reminder?: Reminder | null
}

/** Convierte un ISO string a formato aceptado por <input type="datetime-local">. */
function toDatetimeLocal(value: string) {
  const date = new Date(value)
  const pad = (n: number) => String(n).padStart(2, "0")
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function defaultRemindAt() {
  const inOneWeek = new Date()
  inOneWeek.setDate(inOneWeek.getDate() + 7)
  return toDatetimeLocal(inOneWeek.toISOString())
}

/** Diálogo de creación/edición de recordatorios de seguimiento. */
export function ReminderFormDialog({ open, onOpenChange, reminder }: ReminderFormDialogProps) {
  const { t } = useTranslation(["emails", "common"])
  const isEditing = !!reminder
  const { data: customers } = useCustomers()
  const createReminder = useCreateReminder()
  const updateReminder = useUpdateReminder()

  const reminderFormSchema = useMemo(() => buildReminderFormSchema(t), [t])

  const form = useForm<ReminderFormValues>({
    resolver: zodResolver(reminderFormSchema),
    defaultValues: {
      title: "",
      message: "",
      remindAt: defaultRemindAt(),
      customerId: NO_CUSTOMER,
      status: "Pending",
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        title: reminder?.title ?? "",
        message: reminder?.message ?? "",
        remindAt: reminder ? toDatetimeLocal(reminder.remindAt) : defaultRemindAt(),
        customerId: reminder?.customerId ?? NO_CUSTOMER,
        status: reminder?.status ?? "Pending",
      })
    }
  }, [open, reminder, form])

  function onSubmit(values: ReminderFormValues) {
    const common = {
      title: values.title,
      message: values.message?.length ? values.message : null,
      remindAt: new Date(values.remindAt).toISOString(),
      customerId: values.customerId === NO_CUSTOMER ? null : values.customerId,
      rentalId: reminder?.rentalId ?? null,
    }

    if (reminder) {
      updateReminder.mutate(
        { id: reminder.id, request: { ...common, status: values.status as ReminderStatus } },
        {
          onSuccess: () => {
            toast.success(t("reminders.toast.updated"))
            onOpenChange(false)
          },
          onError: () => toast.error(t("reminders.toast.updateErrorRetry")),
        }
      )
      return
    }

    createReminder.mutate(common, {
      onSuccess: () => {
        toast.success(t("reminders.toast.created"))
        onOpenChange(false)
      },
      onError: () => toast.error(t("reminders.toast.createError")),
    })
  }

  const isPending = createReminder.isPending || updateReminder.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("reminders.form.editTitle") : t("reminders.form.createTitle")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("reminders.form.titleLabel")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("reminders.form.titlePlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="message"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("reminders.form.messageLabel")}</FormLabel>
                  <FormControl>
                    <Textarea rows={3} placeholder={t("reminders.form.messagePlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="remindAt"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("reminders.form.dateLabel")}</FormLabel>
                    <FormControl>
                      <Input type="datetime-local" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="customerId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("reminders.form.customerLabel")}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder={t("reminders.form.noCustomerPlaceholder")} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value={NO_CUSTOMER}>{t("reminders.form.noCustomerPlaceholder")}</SelectItem>
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
            </div>

            {isEditing && (
              <FormField
                control={form.control}
                name="status"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("reminders.form.statusLabel")}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="Pending">{t("reminders.status.pending")}</SelectItem>
                        <SelectItem value="Completed">{t("reminders.status.completed")}</SelectItem>
                        <SelectItem value="Cancelled">{t("reminders.status.cancelled")}</SelectItem>
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
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
