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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
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
import { useAssetGroups } from "@/application/assetGroups/useAssetGroups"
import { useCreatePortalLink } from "@/application/portalLinks/useCreatePortalLink"
import { useUpdatePortalLink } from "@/application/portalLinks/useUpdatePortalLink"
import type { PortalLink } from "@/domain/types/portal"

function buildPortalLinkFormSchema(t: TFunction) {
  return z.object({
    title: z
      .string()
      .trim()
      .min(1, t("validation.titleRequired"))
      .max(150, t("validation.titleMax")),
    assetGroupId: z.string().min(1, t("validation.assetGroupRequired")),
    isActive: z.boolean(),
  })
}

type PortalLinkFormValues = z.infer<ReturnType<typeof buildPortalLinkFormSchema>>

interface PortalLinkFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  portalLink?: PortalLink | null
}

/** Diálogo de creación/edición de links del Portal de Rentas. */
export function PortalLinkFormDialog({
  open,
  onOpenChange,
  portalLink,
}: PortalLinkFormDialogProps) {
  const { t } = useTranslation(["portalLinks", "common"])
  const isEditing = !!portalLink
  const { data: assetGroups } = useAssetGroups()
  const createPortalLink = useCreatePortalLink()
  const updatePortalLink = useUpdatePortalLink()

  const portalLinkFormSchema = useMemo(() => buildPortalLinkFormSchema(t), [t])

  const form = useForm<PortalLinkFormValues>({
    resolver: zodResolver(portalLinkFormSchema),
    defaultValues: {
      title: "",
      assetGroupId: "",
      isActive: true,
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        title: portalLink?.title ?? "",
        assetGroupId: portalLink ? String(portalLink.assetGroupId) : "",
        isActive: portalLink?.isActive ?? true,
      })
    }
  }, [open, portalLink, form])

  function onSubmit(values: PortalLinkFormValues) {
    if (portalLink) {
      updatePortalLink.mutate(
        {
          id: portalLink.id,
          request: {
            title: values.title,
            assetGroupId: Number(values.assetGroupId),
            isActive: values.isActive,
          },
        },
        {
          onSuccess: () => {
            toast.success(t("toast.updateSuccess"))
            onOpenChange(false)
          },
          onError: () => {
            toast.error(t("toast.updateError"))
          },
        }
      )
      return
    }

    createPortalLink.mutate(
      { title: values.title, assetGroupId: Number(values.assetGroupId) },
      {
        onSuccess: () => {
          toast.success(t("toast.createSuccess"))
          onOpenChange(false)
        },
        onError: () => {
          toast.error(t("toast.createError"))
        },
      }
    )
  }

  const isPending = createPortalLink.isPending || updatePortalLink.isPending
  const hasAssetGroups = (assetGroups?.length ?? 0) > 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("formDialog.titleEdit") : t("formDialog.titleNew")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.title")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.placeholders.title")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="assetGroupId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.assetGroup")}</FormLabel>
                  <Select
                    onValueChange={field.onChange}
                    value={field.value}
                    disabled={!hasAssetGroups}
                  >
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue
                          placeholder={
                            hasAssetGroups
                              ? t("formDialog.placeholders.assetGroupSelect")
                              : t("formDialog.placeholders.assetGroupEmpty")
                          }
                        />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {assetGroups?.map((group) => (
                        <SelectItem key={group.id} value={String(group.id)}>
                          {group.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
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
                    <FormLabel className="mb-0">{t("formDialog.fields.active")}</FormLabel>
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
              <Button type="submit" disabled={isPending || !hasAssetGroups}>
                {isPending ? t("common:status.saving") : t("common:buttons.save")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
