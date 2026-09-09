import { useEffect, useState } from "react"
import { toast } from "sonner"
import { MessageCircle, Loader2, Eye, EyeOff } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { useWhatsAppConfig, useSaveWhatsAppConfig } from "@/application/whatsapp/useWhatsAppConfig"

export function WhatsAppConfigCard() {
  const { data: config, isLoading } = useWhatsAppConfig()
  const saveConfig = useSaveWhatsAppConfig()

  const [phoneNumberId, setPhoneNumberId] = useState("")
  const [accessToken, setAccessToken] = useState("")
  const [phoneNumber, setPhoneNumber] = useState("")
  const [displayName, setDisplayName] = useState("")
  const [showToken, setShowToken] = useState(false)

  useEffect(() => {
    if (!config) return
    setPhoneNumber(config.phoneNumber ?? "")
    setDisplayName(config.displayName ?? "")
    // PhoneNumberId and AccessToken are never returned from the API for security;
    // user must re-enter them when saving.
  }, [config])

  function handleSave() {
    if (!phoneNumberId.trim() || !accessToken.trim()) {
      toast.error("Phone Number ID y Access Token son obligatorios.")
      return
    }
    saveConfig.mutate(
      {
        phoneNumberId: phoneNumberId.trim(),
        accessToken: accessToken.trim(),
        phoneNumber: phoneNumber.trim() || null,
        displayName: displayName.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success("Configuración de WhatsApp guardada.")
          setPhoneNumberId("")
          setAccessToken("")
        },
        onError: () => toast.error("Error al guardar la configuración."),
      }
    )
  }

  if (isLoading) return null

  return (
    <Card className="border-border/60 shadow-sm">
      <CardHeader className="pb-4">
        <CardTitle className="flex items-center justify-between text-base">
          <span className="flex items-center gap-2">
            <MessageCircle className="h-4 w-4" />
            Configuración de WhatsApp
          </span>
          {config?.isConfigured ? (
            <Badge variant="default" className="bg-green-600 text-white">
              Configurado
            </Badge>
          ) : (
            <Badge variant="outline" className="border-yellow-500 text-yellow-600">
              No configurado
            </Badge>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Ingresa las credenciales de tu WhatsApp Business Account (WABA) para enviar mensajes
          desde tu número propio. El Access Token no se muestra una vez guardado.
        </p>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="wa-display-name">Nombre visible</Label>
            <Input
              id="wa-display-name"
              placeholder="Ej: Alkiman Rentals"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="wa-phone-number">Número de teléfono (referencia)</Label>
            <Input
              id="wa-phone-number"
              placeholder="Ej: +18091234567"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="wa-phone-number-id">Phone Number ID *</Label>
            <Input
              id="wa-phone-number-id"
              placeholder="ID del número en Meta (ej: 123456789)"
              value={phoneNumberId}
              onChange={(e) => setPhoneNumberId(e.target.value)}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="wa-access-token">Access Token *</Label>
            <div className="relative">
              <Input
                id="wa-access-token"
                type={showToken ? "text" : "password"}
                placeholder={config?.isConfigured ? "••••••••  (dejar en blanco no actualiza)" : "Token permanente de Meta"}
                value={accessToken}
                onChange={(e) => setAccessToken(e.target.value)}
                className="pr-10"
              />
              <button
                type="button"
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                onClick={() => setShowToken((v) => !v)}
                tabIndex={-1}
              >
                {showToken ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          </div>
        </div>

        <div className="flex justify-end">
          <Button
            onClick={handleSave}
            disabled={saveConfig.isPending}
          >
            {saveConfig.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Guardar configuración
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
