import { useMemo, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Mail, Users, Plus, Pencil, Trash2, MoreHorizontal, CheckCircle2, XCircle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import { Checkbox } from "@/components/ui/checkbox"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { ReminderFormDialog } from "@/presentation/components/ReminderFormDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useCustomers } from "@/application/customers/useCustomers"
import { useEmails } from "@/application/emails/useEmails"
import { useSendMassEmail } from "@/application/emails/useSendMassEmail"
import { useReminders } from "@/application/reminders/useReminders"
import { useUpdateReminder } from "@/application/reminders/useUpdateReminder"
import { useDeleteReminder } from "@/application/reminders/useDeleteReminder"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import type { Reminder, ReminderStatus } from "@/domain/types/reminder"

function buildMassFormSchema(t: (key: string) => string) {
  return z.object({
    subject: z
      .string()
      .trim()
      .min(1, t("validation.subjectRequired"))
      .max(200, t("validation.subjectMax")),
    body: z.string().trim().min(1, t("validation.bodyRequired")),
  })
}
type MassFormValues = z.infer<ReturnType<typeof buildMassFormSchema>>

function EmailStatusBadge({ status }: { status: string }) {
  const { t } = useTranslation("emails")
  return status === "Sent" ? (
    <Badge variant="default">{t("history.status.sent")}</Badge>
  ) : (
    <Badge variant="destructive">{t("history.status.failed")}</Badge>
  )
}

function ReminderStatusBadge({ status }: { status: ReminderStatus }) {
  const { t } = useTranslation("emails")
  if (status === "Completed") return <Badge variant="default">{t("reminders.status.completed")}</Badge>
  if (status === "Cancelled") return <Badge variant="outline">{t("reminders.status.cancelled")}</Badge>
  return <Badge variant="secondary">{t("reminders.status.pending")}</Badge>
}

/** Envío de correo masivo a varios clientes seleccionados. */
function MassEmailTab() {
  const { t } = useTranslation(["emails", "common"])
  const { data: customers, isLoading: loadingCustomers } = useCustomers()
  const sendMass = useSendMassEmail()
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  const massFormSchema = useMemo(() => buildMassFormSchema(t), [t])

  const form = useForm<MassFormValues>({
    resolver: zodResolver(massFormSchema),
    defaultValues: { subject: "", body: "" },
  })

  const allSelected = !!customers?.length && selectedIds.length === customers.length

  function toggleAll() {
    setSelectedIds(allSelected ? [] : (customers?.map((c) => c.id) ?? []))
  }

  function toggleOne(id: string) {
    setSelectedIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]))
  }

  function onSubmit(values: MassFormValues) {
    if (selectedIds.length === 0) {
      toast.error(t("massTab.toast.noRecipients"))
      return
    }

    sendMass.mutate(
      { customerIds: selectedIds, subject: values.subject, body: values.body },
      {
        onSuccess: (result) => {
          if (result.failed === 0) {
            toast.success(t("massTab.toast.success", { count: result.sent }))
          } else {
            toast.error(t("massTab.toast.partialFailure", { sent: result.sent, failed: result.failed }))
          }
          form.reset({ subject: "", body: "" })
          setSelectedIds([])
        },
        onError: () => toast.error(t("massTab.toast.processError")),
      }
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("massTab.cardTitle")}</CardTitle>
      </CardHeader>
      <CardContent>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <FormLabel>{t("massTab.recipientsLabel")}</FormLabel>
                {!!customers?.length && (
                  <button
                    type="button"
                    onClick={toggleAll}
                    className="text-xs font-medium text-primary hover:underline"
                  >
                    {allSelected ? t("massTab.deselectAll") : t("massTab.selectAll")}
                  </button>
                )}
              </div>
              <div className="max-h-56 space-y-1 overflow-y-auto rounded-lg border border-border/60 p-2">
                {loadingCustomers && <Skeleton className="h-6 w-full" />}
                {!loadingCustomers && customers?.length === 0 && (
                  <p className="p-2 text-sm text-muted-foreground">{t("massTab.noCustomers")}</p>
                )}
                {customers?.map((customer) => (
                  <label
                    key={customer.id}
                    className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-muted"
                  >
                    <Checkbox
                      checked={selectedIds.includes(customer.id)}
                      onCheckedChange={() => toggleOne(customer.id)}
                    />
                    <span className="font-medium">{customer.fullName}</span>
                    <span className="text-muted-foreground">{customer.email ?? t("massTab.noEmail")}</span>
                  </label>
                ))}
              </div>
              <p className="text-xs text-muted-foreground">
                {t("massTab.selectedCount", { count: selectedIds.length })}
              </p>
            </div>

            <FormField
              control={form.control}
              name="subject"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("massTab.subjectLabel")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("massTab.subjectPlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="body"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("massTab.bodyLabel")}</FormLabel>
                  <FormControl>
                    <Textarea rows={6} placeholder={t("massTab.bodyPlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="flex justify-end">
              <Button type="submit" disabled={sendMass.isPending}>
                <Users className="h-4 w-4" />
                {sendMass.isPending ? t("common:status.sending") : t("massTab.submit")}
              </Button>
            </div>
          </form>
        </Form>
      </CardContent>
    </Card>
  )
}

/** Historial de correos enviados (individuales y masivos). */
function EmailHistoryCard() {
  const { t, i18n } = useTranslation("emails")
  const { data: emails, isLoading } = useEmails()
  const { page, setPage, pageCount, paginated: paginatedEmails, totalCount } = usePagination(emails, 10)

  const dateTimeFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }),
    [i18n.language]
  )

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("history.cardTitle")}</CardTitle>
      </CardHeader>
      <CardContent className="px-6">
        <div className="rounded-lg border border-border/60">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("history.table.headers.recipient")}</TableHead>
                <TableHead>{t("history.table.headers.subject")}</TableHead>
                <TableHead>{t("history.table.headers.type")}</TableHead>
                <TableHead>{t("history.table.headers.status")}</TableHead>
                <TableHead>{t("history.table.headers.date")}</TableHead>
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

              {!isLoading && emails?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    {t("history.table.emptyState")}
                  </TableCell>
                </TableRow>
              )}

              {paginatedEmails.map((email) => (
                <TableRow key={email.id} title={email.errorMessage ?? undefined}>
                  <TableCell className="font-medium">
                    {email.recipientName}
                    <div className="text-xs font-normal text-muted-foreground">
                      {email.recipientEmail || "—"}
                    </div>
                  </TableCell>
                  <TableCell className="text-muted-foreground">{email.subject}</TableCell>
                  <TableCell className="text-muted-foreground">
                    {email.type === "Mass" ? t("history.type.mass") : t("history.type.individual")}
                  </TableCell>
                  <TableCell>
                    <EmailStatusBadge status={email.status} />
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {dateTimeFormatter.format(new Date(email.createdAt))}
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
      </CardContent>
    </Card>
  )
}

/** Listado y gestión de recordatorios de seguimiento. */
function RemindersTab() {
  const { t, i18n } = useTranslation(["emails", "common"])
  const { data: reminders, isLoading } = useReminders()
  const updateReminder = useUpdateReminder()
  const deleteReminder = useDeleteReminder()

  const [formOpen, setFormOpen] = useState(false)
  const [editingReminder, setEditingReminder] = useState<Reminder | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Reminder | null>(null)

  const dateTimeFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }),
    [i18n.language]
  )

  const sortedReminders = useMemo(
    () => [...(reminders ?? [])].sort((a, b) => a.remindAt.localeCompare(b.remindAt)),
    [reminders]
  )

  const { page, setPage, pageCount, paginated: paginatedReminders, totalCount } = usePagination(
    sortedReminders,
    10
  )

  function handleCreate() {
    setEditingReminder(null)
    setFormOpen(true)
  }

  function handleEdit(reminder: Reminder) {
    setEditingReminder(reminder)
    setFormOpen(true)
  }

  function handleStatusChange(reminder: Reminder, status: ReminderStatus) {
    updateReminder.mutate(
      {
        id: reminder.id,
        request: {
          title: reminder.title,
          message: reminder.message,
          remindAt: reminder.remindAt,
          customerId: reminder.customerId,
          rentalId: reminder.rentalId,
          status,
        },
      },
      {
        onSuccess: () => toast.success(t("reminders.toast.updated")),
        onError: () => toast.error(t("reminders.toast.updateError")),
      }
    )
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return
    deleteReminder.mutate(deleteTarget.id, {
      onSuccess: () => toast.success(t("reminders.toast.deleted")),
      onError: () => toast.error(t("reminders.toast.deleteError")),
    })
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-3">
      <div className="flex justify-end">
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4" />
          {t("reminders.newButton")}
        </Button>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("reminders.table.headers.title")}</TableHead>
              <TableHead>{t("reminders.table.headers.customer")}</TableHead>
              <TableHead>{t("reminders.table.headers.date")}</TableHead>
              <TableHead>{t("reminders.table.headers.status")}</TableHead>
              <TableHead className="w-[60px] text-right">{t("common:labels.actions")}</TableHead>
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

            {!isLoading && sortedReminders.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  {t("reminders.table.emptyState")}
                </TableCell>
              </TableRow>
            )}

            {paginatedReminders.map((reminder) => (
              <TableRow key={reminder.id}>
                <TableCell className="font-medium">
                  {reminder.title}
                  {reminder.message && (
                    <div className="text-xs font-normal text-muted-foreground">{reminder.message}</div>
                  )}
                </TableCell>
                <TableCell className="text-muted-foreground">{reminder.customerName ?? "—"}</TableCell>
                <TableCell className="text-muted-foreground">
                  {dateTimeFormatter.format(new Date(reminder.remindAt))}
                </TableCell>
                <TableCell>
                  <ReminderStatusBadge status={reminder.status} />
                </TableCell>
                <TableCell className="text-right">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon-sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem onClick={() => handleEdit(reminder)}>
                        <Pencil className="h-4 w-4" />
                        {t("common:buttons.edit")}
                      </DropdownMenuItem>
                      {reminder.status !== "Completed" && (
                        <DropdownMenuItem onClick={() => handleStatusChange(reminder, "Completed")}>
                          <CheckCircle2 className="h-4 w-4" />
                          {t("reminders.menu.markCompleted")}
                        </DropdownMenuItem>
                      )}
                      {reminder.status !== "Cancelled" && (
                        <DropdownMenuItem onClick={() => handleStatusChange(reminder, "Cancelled")}>
                          <XCircle className="h-4 w-4" />
                          {t("common:buttons.cancel")}
                        </DropdownMenuItem>
                      )}
                      <DropdownMenuItem variant="destructive" onClick={() => setDeleteTarget(reminder)}>
                        <Trash2 className="h-4 w-4" />
                        {t("common:buttons.delete")}
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
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

      <ReminderFormDialog open={formOpen} onOpenChange={setFormOpen} reminder={editingReminder} />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("reminders.deleteDialog.title")}
        description={t("reminders.deleteDialog.description", { title: deleteTarget?.title })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}

/** Pantalla de Correos: envío masivo y gestión de recordatorios. */
export function EmailsPage() {
  const { t } = useTranslation("emails")

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("subtitle")}</p>
        <p className="mt-1 flex items-center gap-1.5 text-xs text-amber-600">
          <Mail className="h-3.5 w-3.5" />
          {t("providerWarning")}
        </p>
      </div>

      <Tabs defaultValue="mass">
        <TabsList>
          <TabsTrigger value="mass">{t("tabs.mass")}</TabsTrigger>
          <TabsTrigger value="reminders">{t("tabs.reminders")}</TabsTrigger>
        </TabsList>

        <TabsContent value="mass" className="space-y-6">
          <MassEmailTab />
          <EmailHistoryCard />
        </TabsContent>

        <TabsContent value="reminders">
          <RemindersTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}
