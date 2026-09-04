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
import { useCategories } from "@/application/categories/useCategories"
import { useCreateAsset } from "@/application/assets/useCreateAsset"
import { useUpdateAsset } from "@/application/assets/useUpdateAsset"
import { rentalTypeLabels } from "@/domain/types/asset"
import type { Asset } from "@/domain/types/asset"

function buildAssetFormSchema(t: TFunction) {
  return z.object({
    categoryId: z.string().min(1, t("validation.categoryRequired")),
    name: z
      .string()
      .trim()
      .min(1, t("validation.nameRequired"))
      .max(200, t("validation.nameMax")),
    description: z.string().trim().optional(),
    imageUrl: z.string().trim().optional(),
    rentalType: z.enum(["Daily", "Weekly", "Biweekly", "Monthly", "Annual"]),
    basePrice: z
      .string()
      .min(1, t("validation.priceRequired"))
      .refine(
        (val) => !Number.isNaN(Number(val)),
        t("validation.priceInvalid")
      )
      .refine((val) => Number(val) >= 0, t("validation.priceNegative")),
    stock: z
      .string()
      .min(1, t("validation.stockRequired"))
      .refine(
        (val) => !Number.isNaN(Number(val)),
        t("validation.stockInvalid")
      )
      .refine(
        (val) => Number.isInteger(Number(val)),
        t("validation.stockInteger")
      )
      .refine((val) => Number(val) >= 0, t("validation.stockNegative")),
  })
}

type AssetFormValues = z.infer<ReturnType<typeof buildAssetFormSchema>>

interface AssetFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  asset?: Asset | null
}

/** Diálogo de creación/edición de activos (inventario). */
export function AssetFormDialog({
  open,
  onOpenChange,
  asset,
}: AssetFormDialogProps) {
  const { t } = useTranslation(["assets", "common"])
  const isEditing = !!asset
  const { data: categories } = useCategories()
  const createAsset = useCreateAsset()
  const updateAsset = useUpdateAsset()

  const assetFormSchema = useMemo(() => buildAssetFormSchema(t), [t])

  const form = useForm<AssetFormValues>({
    resolver: zodResolver(assetFormSchema),
    defaultValues: {
      categoryId: "",
      name: "",
      description: "",
      imageUrl: "",
      rentalType: "Monthly",
      basePrice: "0",
      stock: "0",
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        categoryId: asset ? String(asset.categoryId) : "",
        name: asset?.name ?? "",
        description: asset?.description ?? "",
        imageUrl: asset?.imageUrl ?? "",
        rentalType: asset?.rentalType ?? "Monthly",
        basePrice: asset ? String(asset.basePrice) : "0",
        stock: asset ? String(asset.stock) : "0",
      })
    }
  }, [open, asset, form])

  function onSubmit(values: AssetFormValues) {
    const common = {
      categoryId: Number(values.categoryId),
      name: values.name,
      description: values.description?.length ? values.description : null,
      imageUrl: values.imageUrl?.length ? values.imageUrl : null,
      basePrice: Number(values.basePrice),
      stock: Number(values.stock),
    }

    if (asset) {
      updateAsset.mutate(
        { id: asset.id, request: common },
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

    createAsset.mutate(
      { ...common, rentalType: values.rentalType },
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

  const isPending = createAsset.isPending || updateAsset.isPending
  const hasCategories = (categories?.length ?? 0) > 0

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
                    <Input
                      placeholder={t("dialog.fields.namePlaceholder")}
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
                name="categoryId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.category")}</FormLabel>
                    <Select
                      onValueChange={field.onChange}
                      value={field.value}
                      disabled={!hasCategories}
                    >
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue
                            placeholder={
                              hasCategories
                                ? t("dialog.fields.categoryPlaceholder")
                                : t("dialog.fields.categoryPlaceholderEmpty")
                            }
                          />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {categories?.map((category) => (
                          <SelectItem
                            key={category.id}
                            value={String(category.id)}
                          >
                            {category.name}
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
                name="rentalType"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.rentalType")}</FormLabel>
                    <Select
                      onValueChange={field.onChange}
                      value={field.value}
                      disabled={isEditing}
                    >
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {(
                          Object.keys(rentalTypeLabels) as Array<
                            AssetFormValues["rentalType"]
                          >
                        ).map((value) => (
                          <SelectItem key={value} value={value}>
                            {rentalTypeLabels[value]}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="basePrice"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.basePrice")}</FormLabel>
                    <FormControl>
                      <Input type="number" step="0.01" min="0" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="stock"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("dialog.fields.stock")}</FormLabel>
                    <FormControl>
                      <Input type="number" step="1" min="0" {...field} />
                    </FormControl>
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
                  <FormLabel>{t("dialog.fields.description")}</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder={t("dialog.fields.descriptionPlaceholder")}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="imageUrl"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("dialog.fields.imageUrl")}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder={t("dialog.fields.imageUrlPlaceholder")}
                      {...field}
                    />
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
              <Button type="submit" disabled={isPending || !hasCategories}>
                {isPending ? t("common:status.saving") : t("common:buttons.save")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
