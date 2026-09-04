import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"
import { useUpdateLandlordProfile } from "@/application/landlords/useUpdateLandlordProfile"
import { useCountries } from "@/application/locations/useCountries"
import { useStates } from "@/application/locations/useStates"
import { useCities } from "@/application/locations/useCities"

function buildProfileFormSchema(t: TFunction) {
  return z.object({
    businessName: z.string().trim().min(1, t("profile.validation.businessNameRequired")).max(150),
    countryId: z.string().optional(),
    stateId: z.string().optional(),
    cityId: z.string().optional(),
    address: z.string().trim().max(255).optional(),
    phone1: z.string().trim().max(30).optional(),
    phone2: z.string().trim().max(30).optional(),
    taxId: z.string().trim().max(50).optional(),
  })
}

type ProfileFormValues = z.infer<ReturnType<typeof buildProfileFormSchema>>

/** Formulario de datos de perfil/negocio del landlord autenticado. */
export function ProfileSettingsForm() {
  const { t } = useTranslation(["settingsForms", "common"])
  const { data: landlord } = useCurrentLandlord()
  const updateProfile = useUpdateLandlordProfile()

  const profileFormSchema = useMemo(() => buildProfileFormSchema(t), [t])

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: {
      businessName: "",
      countryId: "",
      stateId: "",
      cityId: "",
      address: "",
      phone1: "",
      phone2: "",
      taxId: "",
    },
  })

  useEffect(() => {
    if (!landlord) return
    form.reset({
      businessName: landlord.businessName,
      countryId: landlord.countryId != null ? String(landlord.countryId) : "",
      stateId: landlord.stateId != null ? String(landlord.stateId) : "",
      cityId: landlord.cityId != null ? String(landlord.cityId) : "",
      address: landlord.address ?? "",
      phone1: landlord.phone1 ?? "",
      phone2: landlord.phone2 ?? "",
      taxId: landlord.taxId ?? "",
    })
  }, [landlord, form])

  const countryId = form.watch("countryId")
  const stateId = form.watch("stateId")
  const countryIdNum = countryId ? Number(countryId) : null
  const stateIdNum = stateId ? Number(stateId) : null

  const { data: countries } = useCountries()
  const { data: states } = useStates(countryIdNum)
  const { data: cities } = useCities(stateIdNum)

  function onSubmit(values: ProfileFormValues) {
    updateProfile.mutate(
      {
        businessName: values.businessName,
        countryId: values.countryId?.length ? Number(values.countryId) : null,
        stateId: values.stateId?.length ? Number(values.stateId) : null,
        cityId: values.cityId?.length ? Number(values.cityId) : null,
        address: values.address?.length ? values.address : null,
        phone1: values.phone1?.length ? values.phone1 : null,
        phone2: values.phone2?.length ? values.phone2 : null,
        taxId: values.taxId?.length ? values.taxId : null,
      },
      {
        onSuccess: () => toast.success(t("profile.toastSuccess")),
        onError: () => toast.error(t("profile.toastError")),
      }
    )
  }

  return (
    <div className="space-y-6">
      <Card>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)}>
            <CardHeader>
              <CardTitle>{t("profile.cardTitle")}</CardTitle>
              <CardDescription>
                {t("profile.cardDescription")}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormField
                  control={form.control}
                  name="businessName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.businessNameLabel")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("profile.businessNamePlaceholder")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <FormField
                  control={form.control}
                  name="countryId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.countryLabel")}</FormLabel>
                      <Select
                        onValueChange={(value) => {
                          // Radix Select puede disparar onValueChange("") de forma espuria
                          // mientras el SelectContent todavía no registró el item que
                          // coincide con el valor controlado (p. ej. justo después de un
                          // form.reset() con datos que cargan de forma asíncrona). Como
                          // estos selects no tienen una opción vacía/"limpiar", un valor
                          // real elegido por el usuario nunca es "".
                          if (!value) return
                          field.onChange(value)
                          form.setValue("stateId", "")
                          form.setValue("cityId", "")
                        }}
                        value={field.value}
                      >
                        <FormControl>
                          <SelectTrigger className="w-full">
                            <SelectValue placeholder={t("profile.countryPlaceholder")} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {countries?.map((country) => (
                            <SelectItem key={country.id} value={String(country.id)}>
                              {country.name}
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
                  name="stateId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.stateLabel")}</FormLabel>
                      <Select
                        onValueChange={(value) => {
                          if (!value) return
                          field.onChange(value)
                          form.setValue("cityId", "")
                        }}
                        value={field.value}
                        disabled={!countryIdNum}
                      >
                        <FormControl>
                          <SelectTrigger className="w-full">
                            <SelectValue
                              placeholder={
                                countryIdNum
                                  ? t("profile.statePlaceholder")
                                  : t("profile.statePlaceholderNoCountry")
                              }
                            />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {states?.map((state) => (
                            <SelectItem key={state.id} value={String(state.id)}>
                              {state.name}
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
                  name="cityId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.cityLabel")}</FormLabel>
                      <Select
                        onValueChange={(value) => {
                          if (!value) return
                          field.onChange(value)
                        }}
                        value={field.value}
                        disabled={!stateIdNum}
                      >
                        <FormControl>
                          <SelectTrigger className="w-full">
                            <SelectValue
                              placeholder={
                                stateIdNum ? t("profile.cityPlaceholder") : t("profile.cityPlaceholderNoState")
                              }
                            />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {cities?.map((city) => (
                            <SelectItem key={city.id} value={String(city.id)}>
                              {city.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid grid-cols-1 gap-4">
                <FormField
                  control={form.control}
                  name="address"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.addressLabel")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("profile.addressPlaceholder")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <FormField
                  control={form.control}
                  name="phone1"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.phone1Label")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("profile.phone1Placeholder")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="phone2"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.phone2Label")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("profile.phone2Placeholder")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="taxId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("profile.taxIdLabel")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("profile.taxIdPlaceholder")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </CardContent>
            <CardFooter className="justify-end">
              <Button type="submit" disabled={updateProfile.isPending}>
                {updateProfile.isPending ? t("common:status.saving") : t("profile.saveButton")}
              </Button>
            </CardFooter>
          </form>
        </Form>
      </Card>

    </div>
  )
}
