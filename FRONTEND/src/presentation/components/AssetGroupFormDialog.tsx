import { useEffect, useMemo, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Checkbox } from "@/components/ui/checkbox"
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
import { useCreateAssetGroup } from "@/application/assetGroups/useCreateAssetGroup"
import { useUpdateAssetGroup } from "@/application/assetGroups/useUpdateAssetGroup"
import { useAssets } from "@/application/assets/useAssets"
import { useCategories } from "@/application/categories/useCategories"
import type { AssetGroup } from "@/domain/types/assetGroup"

function buildAssetGroupFormSchema(t: TFunction) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, t("validation.nameRequired"))
      .max(150, t("validation.nameMax")),
    description: z.string().trim().max(500, t("validation.descriptionMax")).optional(),
    assetIds: z.array(z.string()).min(1, t("validation.assetsRequired")),
  })
}

type AssetGroupFormValues = z.infer<ReturnType<typeof buildAssetGroupFormSchema>>

interface AssetGroupFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  assetGroup?: AssetGroup | null
}

/** Diálogo de alta/edición de grupos de activos: agrupa activos de cualquier categoría bajo un nombre libre. */
export function AssetGroupFormDialog({ open, onOpenChange, assetGroup }: AssetGroupFormDialogProps) {
  const { t } = useTranslation(["assetGroups", "common"])
  const isEditing = !!assetGroup
  const [search, setSearch] = useState("")
  const { data: assets } = useAssets()
  const { data: categories } = useCategories()
  const createAssetGroup = useCreateAssetGroup()
  const updateAssetGroup = useUpdateAssetGroup()

  const assetGroupFormSchema = useMemo(() => buildAssetGroupFormSchema(t), [t])

  const categoryNameById = useMemo(
    () => new Map(categories?.map((category) => [category.id, category.name])),
    [categories]
  )

  const filteredAssets = useMemo(() => {
    const term = search.trim().toLowerCase()
    const list = assets ?? []
    if (!term) return list
    return list.filter((asset) => asset.name.toLowerCase().includes(term))
  }, [assets, search])

  const form = useForm<AssetGroupFormValues>({
    resolver: zodResolver(assetGroupFormSchema),
    defaultValues: { name: "", description: "", assetIds: [] },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        name: assetGroup?.name ?? "",
        description: assetGroup?.description ?? "",
        assetIds: assetGroup?.assetIds ?? [],
      })
      setSearch("")
    }
  }, [open, assetGroup, form])

  function onSubmit(values: AssetGroupFormValues) {
    const payload = {
      name: values.name,
      description: values.description || null,
      assetIds: values.assetIds,
    }

    if (assetGroup) {
      updateAssetGroup.mutate(
        { id: assetGroup.id, request: payload },
        {
          onSuccess: () => {
            toast.success(t("toast.updated"))
            onOpenChange(false)
          },
          onError: () => toast.error(t("toast.updateError")),
        }
      )
      return
    }

    createAssetGroup.mutate(payload, {
      onSuccess: () => {
        toast.success(t("toast.created"))
        onOpenChange(false)
      },
      onError: () => toast.error(t("toast.createError")),
    })
  }

  const isPending = createAssetGroup.isPending || updateAssetGroup.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("dialog.editTitle") : t("dialog.createTitle")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.name")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("dialog.fields.namePlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.description")}</FormLabel>
                  <FormControl>
                    <Textarea placeholder={t("dialog.fields.descriptionPlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="assetIds"
              render={() => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.assets")}</FormLabel>
                  <Input
                    placeholder={t("dialog.fields.assetsSearchPlaceholder")}
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                  <div className="max-h-64 space-y-2 overflow-y-auto rounded-lg border border-border/60 p-3">
                    {filteredAssets.length === 0 && (
                      <p className="text-sm text-muted-foreground">
                        {t("dialog.fields.noAssetsFound")}
                      </p>
                    )}
                    {filteredAssets.map((asset) => (
                      <FormField
                        key={asset.id}
                        control={form.control}
                        name="assetIds"
                        render={({ field }) => {
                          const checked = field.value.includes(asset.id)
                          return (
                            <FormItem className="flex flex-row items-center gap-2 space-y-0">
                              <FormControl>
                                <Checkbox
                                  checked={checked}
                                  onCheckedChange={(value) => {
                                    if (value) {
                                      field.onChange([...field.value, asset.id])
                                    } else {
                                      field.onChange(
                                        field.value.filter((id) => id !== asset.id)
                                      )
                                    }
                                  }}
                                />
                              </FormControl>
                              <FormLabel className="text-sm font-normal">
                                {asset.name}{" "}
                                <span className="text-xs text-muted-foreground">
                                  ({categoryNameById.get(asset.categoryId) ?? t("dialog.fields.noCategory")})
                                </span>
                              </FormLabel>
                            </FormItem>
                          )
                        }}
                      />
                    ))}
                  </div>
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
