import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Switch } from "@/components/ui/switch"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
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

const DURATION_OPTIONS = [
  { value: "15",  label: "15 min" },
  { value: "20",  label: "20 min" },
  { value: "30",  label: "30 min" },
  { value: "45",  label: "45 min" },
  { value: "60",  label: "1 hora" },
  { value: "75",  label: "1 h 15 min" },
  { value: "90",  label: "1 h 30 min" },
  { value: "120", label: "2 horas" },
  { value: "150", label: "2 h 30 min" },
  { value: "180", label: "3 horas" },
]
import { useCreateBarbershopService, useUpdateBarbershopService } from "@/application/barbershop/useBarbershopServices"
import type { BarbershopService } from "@/domain/types/barbershop"

function buildSchema(t: TFunction) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, t("validation.nameRequired"))
      .max(150, t("validation.nameMax")),
    description: z.string().trim().optional(),
    price: z
      .string()
      .min(1)
      .refine((val) => !Number.isNaN(Number(val)) && Number(val) >= 0, t("validation.priceMin")),
    durationMinutes: z
      .string()
      .min(1, t("validation.durationMin")),
    isActive: z.boolean(),
  })
}

type FormValues = z.infer<ReturnType<typeof buildSchema>>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  service?: BarbershopService | null
}

export function BarbershopServiceFormDialog({ open, onOpenChange, service }: Props) {
  const { t } = useTranslation(["barbershop", "common"])
  const isEditing = !!service
  const create = useCreateBarbershopService()
  const update = useUpdateBarbershopService()

  const schema = useMemo(() => buildSchema(t), [t])

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", description: "", price: "0", durationMinutes: "30", isActive: true },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        name: service?.name ?? "",
        description: service?.description ?? "",
        price: service ? String(service.price) : "0",
        durationMinutes: service ? String(service.durationMinutes) : "30",
        isActive: service?.isActive ?? true,
      })
    }
  }, [open, service, form])

  function onSubmit(values: FormValues) {
    const common = {
      name: values.name,
      description: values.description?.length ? values.description : null,
      price: Number(values.price),
      durationMinutes: Number(values.durationMinutes),
    }

    if (service) {
      update.mutate(
        { id: service.id, request: { ...common, isActive: values.isActive } },
        {
          onSuccess: () => { toast.success(t("toast.updateSuccess")); onOpenChange(false) },
          onError: () => toast.error(t("toast.updateError")),
        }
      )
      return
    }

    create.mutate(common, {
      onSuccess: () => { toast.success(t("toast.createSuccess")); onOpenChange(false) },
      onError: () => toast.error(t("toast.createError")),
    })
  }

  const isPending = create.isPending || update.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("formDialog.service.titleEdit") : t("formDialog.service.titleNew")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.service.fields.name")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.service.placeholders.name")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="price"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.service.fields.price")}</FormLabel>
                    <FormControl>
                      <Input
                        type="number"
                        step="0.01"
                        min="0"
                        placeholder={t("formDialog.service.placeholders.price")}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="durationMinutes"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.service.fields.duration")}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder={t("formDialog.service.placeholders.duration")} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {DURATION_OPTIONS.map((opt) => (
                          <SelectItem key={opt.value} value={opt.value}>
                            {opt.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.service.fields.description")}</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder={t("formDialog.service.placeholders.description")}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {isEditing && (
              <FormField
                control={form.control}
                name="isActive"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border border-border/60 p-3">
                    <FormLabel className="mb-0">{t("services.table.statusActive")}</FormLabel>
                    <FormControl>
                      <Switch checked={field.value} onCheckedChange={field.onChange} />
                    </FormControl>
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
