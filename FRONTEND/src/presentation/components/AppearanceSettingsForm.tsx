import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Check } from "lucide-react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { cn } from "@/lib/utils"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"
import { useUpdateAppearance } from "@/application/landlords/useUpdateAppearance"
import { ACCENT_COLORS, type AccentColor, type ThemeMode } from "@/domain/types/landlord"

/**
 * La bolita que se ve en la paleta. Es el mismo hex que index.css le da a
 * --primary en modo claro, escrito de nuevo acá porque Tailwind necesita la
 * clase literal para generarla: un `bg-[var(--primary)]` pintaría las doce
 * bolitas del color actualmente elegido, que es justo lo que no queremos.
 */
const accentSwatchClasses: Record<AccentColor, string> = {
  blue: "bg-[#4f46e5]",
  sky: "bg-[#0284c7]",
  cyan: "bg-[#0891b2]",
  teal: "bg-[#0d9488]",
  green: "bg-[#16a34a]",
  orange: "bg-[#ea580c]",
  red: "bg-[#dc2626]",
  rose: "bg-[#e11d48]",
  pink: "bg-[#db2777]",
  fuchsia: "bg-[#c026d3]",
  violet: "bg-[#7c3aed]",
  slate: "bg-[#475569]",
}

function buildAccentLabels(t: TFunction): Record<AccentColor, string> {
  return {
    blue: t("appearance.accentLabels.blue"),
    sky: t("appearance.accentLabels.sky"),
    cyan: t("appearance.accentLabels.cyan"),
    teal: t("appearance.accentLabels.teal"),
    green: t("appearance.accentLabels.green"),
    orange: t("appearance.accentLabels.orange"),
    red: t("appearance.accentLabels.red"),
    rose: t("appearance.accentLabels.rose"),
    pink: t("appearance.accentLabels.pink"),
    fuchsia: t("appearance.accentLabels.fuchsia"),
    violet: t("appearance.accentLabels.violet"),
    slate: t("appearance.accentLabels.slate"),
  }
}

function buildAppearanceFormSchema(t: TFunction) {
  return z.object({
    appName: z.string().trim().min(1, t("appearance.validation.appNameRequired")).max(100),
    themeMode: z.enum(["light", "dark"]),
    accentColor: z.enum(ACCENT_COLORS),
  })
}

type AppearanceFormValues = z.infer<ReturnType<typeof buildAppearanceFormSchema>>

/** Formulario de apariencia: nombre de la app, modo de tema y color de acento. */
export function AppearanceSettingsForm() {
  const { t } = useTranslation(["settingsForms", "common"])
  const { data: landlord } = useCurrentLandlord()
  const updateAppearance = useUpdateAppearance()

  const accentLabels = useMemo(() => buildAccentLabels(t), [t])
  const appearanceFormSchema = useMemo(() => buildAppearanceFormSchema(t), [t])

  const form = useForm<AppearanceFormValues>({
    resolver: zodResolver(appearanceFormSchema),
    defaultValues: {
      appName: "Alkiman",
      themeMode: "light",
      accentColor: "blue",
    },
  })

  useEffect(() => {
    if (!landlord) return
    form.reset({
      appName: landlord.appName,
      themeMode: landlord.themeMode,
      accentColor: landlord.accentColor,
    })
  }, [landlord, form])

  function onSubmit(values: AppearanceFormValues) {
    updateAppearance.mutate(values, {
      onSuccess: () => toast.success(t("appearance.toastSuccess")),
      onError: () => toast.error(t("appearance.toastError")),
    })
  }

  return (
    <Card>
      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)}>
          <CardHeader>
            <CardTitle>{t("appearance.cardTitle")}</CardTitle>
            <CardDescription>
              {t("appearance.cardDescription")}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <FormField
              control={form.control}
              name="appName"
              render={({ field }) => (
                <FormItem className="max-w-sm">
                  <FormLabel>{t("appearance.appNameLabel")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("appearance.appNamePlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="themeMode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("appearance.themeLabel")}</FormLabel>
                  <FormControl>
                    <div className="flex gap-3">
                      {(["light", "dark"] as ThemeMode[]).map((mode) => (
                        <button
                          key={mode}
                          type="button"
                          onClick={() => field.onChange(mode)}
                          className={cn(
                            "rounded-lg border px-4 py-2 text-sm font-medium transition-colors",
                            field.value === mode
                              ? "border-primary bg-accent text-accent-foreground"
                              : "border-border text-muted-foreground hover:bg-muted"
                          )}
                        >
                          {mode === "light" ? t("appearance.themeLight") : t("appearance.themeDark")}
                        </button>
                      ))}
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="accentColor"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("appearance.accentColorLabel")}</FormLabel>
                  <FormControl>
                    <div className="flex flex-wrap gap-3">
                      {ACCENT_COLORS.map((accent) => (
                        <button
                          key={accent}
                          type="button"
                          title={accentLabels[accent]}
                          onClick={() => field.onChange(accent)}
                          className={cn(
                            "flex h-9 w-9 items-center justify-center rounded-full ring-offset-2 transition-shadow",
                            accentSwatchClasses[accent],
                            field.value === accent
                              ? "ring-2 ring-foreground"
                              : "hover:ring-2 hover:ring-muted-foreground/50"
                          )}
                        >
                          {field.value === accent && (
                            <Check className="h-4 w-4 text-white" />
                          )}
                          <span className="sr-only">{accentLabels[accent]}</span>
                        </button>
                      ))}
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </CardContent>
          <CardFooter className="justify-end">
            <Button type="submit" disabled={updateAppearance.isPending}>
              {updateAppearance.isPending ? t("common:status.saving") : t("appearance.saveButton")}
            </Button>
          </CardFooter>
        </form>
      </Form>
    </Card>
  )
}
