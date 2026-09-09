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
  FormDescription,
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
import { useCreateBarbershopPortalLink } from "@/application/barbershop/useBarbershopPortalLinks"
import { useBarbershopStylists } from "@/application/barbershop/useBarbershopStylists"

const NO_STYLIST = "__none__"

function buildSchema(t: TFunction) {
  return z.object({
    title: z
      .string()
      .trim()
      .min(1, t("validation.nameRequired"))
      .max(150, t("validation.nameMax")),
    slug: z
      .string()
      .trim()
      .min(1, t("validation.slugRequired"))
      .max(100, t("validation.slugMax"))
      .regex(/^[a-z0-9-]+$/, t("validation.slugInvalid")),
    stylistId: z.string().optional(),
  })
}

type FormValues = z.infer<ReturnType<typeof buildSchema>>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function BarbershopPortalLinkFormDialog({ open, onOpenChange }: Props) {
  const { t } = useTranslation(["barbershop", "common"])
  const create = useCreateBarbershopPortalLink()
  const { data: stylists } = useBarbershopStylists()

  const schema = useMemo(() => buildSchema(t), [t])

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { title: "", slug: "", stylistId: NO_STYLIST },
  })

  useEffect(() => {
    if (open) {
      form.reset({ title: "", slug: "", stylistId: NO_STYLIST })
    }
  }, [open, form])

  function onSubmit(values: FormValues) {
    create.mutate(
      {
        title: values.title,
        slug: values.slug,
        stylistId: values.stylistId === NO_STYLIST ? null : (values.stylistId ?? null),
      },
      {
        onSuccess: () => { toast.success(t("toast.createSuccess")); onOpenChange(false) },
        onError: () => toast.error(t("toast.createError")),
      }
    )
  }

  const isPending = create.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("formDialog.portalLink.titleNew")}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.portalLink.fields.title")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.portalLink.placeholders.title")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="slug"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.portalLink.fields.slug")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.portalLink.placeholders.slug")} {...field} />
                  </FormControl>
                  <FormDescription>{t("formDialog.portalLink.slugHint")}</FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="stylistId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.portalLink.fields.stylist")}</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value={NO_STYLIST}>
                        {t("formDialog.portalLink.placeholders.stylistNone")}
                      </SelectItem>
                      {stylists
                        ?.filter((s) => s.isActive)
                        .map((stylist) => (
                          <SelectItem key={stylist.id} value={stylist.id}>
                            {stylist.fullName}
                          </SelectItem>
                        ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

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
