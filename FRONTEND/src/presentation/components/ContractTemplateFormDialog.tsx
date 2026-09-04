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
import { useCreateContractTemplate } from "@/application/contractTemplates/useCreateContractTemplate"
import { useUpdateContractTemplate } from "@/application/contractTemplates/useUpdateContractTemplate"
import type { ContractTemplate } from "@/domain/types/contractTemplate"

/** Tokens soportados por ContractService.MergeContent en el backend; se reemplazan automáticamente al generar cada contrato. */
const PLACEHOLDER_TOKENS: { token: string; descriptionKey: string }[] = [
  { token: "{{NombreNegocio}}", descriptionKey: "formDialog.tokens.businessName" },
  { token: "{{NombreCliente}}", descriptionKey: "formDialog.tokens.customerName" },
  { token: "{{IdentificacionCliente}}", descriptionKey: "formDialog.tokens.customerId" },
  { token: "{{TelefonoCliente}}", descriptionKey: "formDialog.tokens.customerPhone" },
  { token: "{{EmailCliente}}", descriptionKey: "formDialog.tokens.customerEmail" },
  { token: "{{DireccionCliente}}", descriptionKey: "formDialog.tokens.customerAddress" },
  { token: "{{NombreActivo}}", descriptionKey: "formDialog.tokens.assetName" },
  { token: "{{DescripcionActivo}}", descriptionKey: "formDialog.tokens.assetDescription" },
  { token: "{{CategoriaActivo}}", descriptionKey: "formDialog.tokens.assetCategory" },
  { token: "{{FechaInicio}}", descriptionKey: "formDialog.tokens.startDate" },
  { token: "{{FechaFin}}", descriptionKey: "formDialog.tokens.endDate" },
  { token: "{{TipoRenta}}", descriptionKey: "formDialog.tokens.rentalType" },
  { token: "{{PrecioBase}}", descriptionKey: "formDialog.tokens.basePrice" },
  { token: "{{PrecioTotal}}", descriptionKey: "formDialog.tokens.totalPrice" },
  { token: "{{FechaHoy}}", descriptionKey: "formDialog.tokens.todayDate" },
]

function buildContractTemplateFormSchema(t: TFunction) {
  return z.object({
    categoryId: z.string().min(1, t("validation.categoryRequired")),
    name: z
      .string()
      .trim()
      .min(1, t("validation.nameRequired"))
      .max(150, t("validation.nameMax")),
    content: z.string().trim().min(1, t("validation.contentRequired")),
  })
}

type ContractTemplateFormValues = z.infer<ReturnType<typeof buildContractTemplateFormSchema>>

interface ContractTemplateFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  template?: ContractTemplate | null
  /** Categoría preseleccionada al crear (ej: desde el filtro activo de la página). */
  defaultCategoryId?: number | null
}

/** Diálogo de creación/edición de plantillas de contrato. */
export function ContractTemplateFormDialog({
  open,
  onOpenChange,
  template,
  defaultCategoryId,
}: ContractTemplateFormDialogProps) {
  const { t } = useTranslation(["contractTemplates", "common"])
  const isEditing = !!template
  const { data: categories } = useCategories()
  const createTemplate = useCreateContractTemplate()
  const updateTemplate = useUpdateContractTemplate()

  const contractTemplateFormSchema = useMemo(() => buildContractTemplateFormSchema(t), [t])

  const form = useForm<ContractTemplateFormValues>({
    resolver: zodResolver(contractTemplateFormSchema),
    defaultValues: { categoryId: "", name: "", content: "" },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        categoryId: template
          ? String(template.categoryId)
          : defaultCategoryId
            ? String(defaultCategoryId)
            : "",
        name: template?.name ?? "",
        content: template?.content ?? "",
      })
    }
  }, [open, template, defaultCategoryId, form])

  function onSubmit(values: ContractTemplateFormValues) {
    if (template) {
      updateTemplate.mutate(
        { id: template.id, request: { name: values.name, content: values.content } },
        {
          onSuccess: () => {
            toast.success(t("toast.updateSuccess"))
            onOpenChange(false)
          },
          onError: () => toast.error(t("toast.updateError")),
        }
      )
      return
    }

    createTemplate.mutate(
      { categoryId: Number(values.categoryId), name: values.name, content: values.content },
      {
        onSuccess: () => {
          toast.success(t("toast.createSuccess"))
          onOpenChange(false)
        },
        onError: () => toast.error(t("toast.createError")),
      }
    )
  }

  const isPending = createTemplate.isPending || updateTemplate.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("formDialog.titleEdit") : t("formDialog.titleNew")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="categoryId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.fields.category")}</FormLabel>
                    <Select
                      value={field.value}
                      onValueChange={field.onChange}
                      disabled={isEditing}
                    >
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder={t("formDialog.placeholders.category")} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {categories?.map((category) => (
                          <SelectItem key={category.id} value={String(category.id)}>
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
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.fields.name")}</FormLabel>
                    <FormControl>
                      <Input placeholder={t("formDialog.placeholders.name")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="content"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.content")}</FormLabel>
                  <FormControl>
                    <Textarea
                      rows={12}
                      placeholder={t("formDialog.placeholders.content")}
                      className="font-mono text-xs"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="rounded-lg border border-border/60 bg-muted/40 p-3 text-xs text-muted-foreground">
              <p className="mb-1.5 font-medium text-foreground">{t("formDialog.tokensHelp")}</p>
              <div className="grid grid-cols-1 gap-x-4 gap-y-1 sm:grid-cols-2">
                {PLACEHOLDER_TOKENS.map(({ token, descriptionKey }) => (
                  <div key={token} className="flex gap-1.5">
                    <code className="shrink-0 rounded bg-background px-1 py-0.5 text-[11px] text-foreground">
                      {token}
                    </code>
                    <span className="truncate">{t(descriptionKey)}</span>
                  </div>
                ))}
              </div>
            </div>

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
