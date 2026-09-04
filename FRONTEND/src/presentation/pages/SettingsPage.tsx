import { useTranslation } from "react-i18next"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { MyAccountSettingsForm } from "@/presentation/components/MyAccountSettingsForm"
import { ProfileSettingsForm } from "@/presentation/components/ProfileSettingsForm"
import { AppearanceSettingsForm } from "@/presentation/components/AppearanceSettingsForm"
import { SignatureSettingsForm } from "@/presentation/components/SignatureSettingsForm"
import { LanguageSettingsForm } from "@/presentation/components/LanguageSettingsForm"
import { SecuritySettingsPanel } from "@/presentation/components/SecuritySettingsPanel"

export function SettingsPage() {
  const { t } = useTranslation("settings")

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t("description")}</p>
      </div>

      <Tabs defaultValue="cuenta">
        <TabsList>
          <TabsTrigger value="cuenta">{t("tabs.account")}</TabsTrigger>
          <TabsTrigger value="perfil">{t("tabs.profile")}</TabsTrigger>
          <TabsTrigger value="apariencia">{t("tabs.appearance")}</TabsTrigger>
          <TabsTrigger value="firma">{t("tabs.signature")}</TabsTrigger>
          <TabsTrigger value="idioma">{t("tabs.language")}</TabsTrigger>
          <TabsTrigger value="seguridad">{t("tabs.security")}</TabsTrigger>
        </TabsList>

        <TabsContent value="cuenta">
          <MyAccountSettingsForm />
        </TabsContent>

        <TabsContent value="perfil">
          <ProfileSettingsForm />
        </TabsContent>

        <TabsContent value="apariencia">
          <AppearanceSettingsForm />
        </TabsContent>

        <TabsContent value="firma">
          <SignatureSettingsForm />
        </TabsContent>

        <TabsContent value="idioma">
          <LanguageSettingsForm />
        </TabsContent>

        <TabsContent value="seguridad">
          <SecuritySettingsPanel />
        </TabsContent>
      </Tabs>
    </div>
  )
}
