import { useState, type FormEvent } from "react"
import { useAuth0 } from "@auth0/auth0-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { useRegisterLandlord } from "@/application/landlords/useRegisterLandlord"

export function OnboardingForm() {
  const { user } = useAuth0()
  const registerLandlord = useRegisterLandlord()

  const [businessName, setBusinessName] = useState("")
  const [email, setEmail] = useState(user?.email ?? "")

  function handleSubmit(event: FormEvent) {
    event.preventDefault()

    registerLandlord.mutate(
      { businessName, email },
      {
        onError: () => {
          toast.error("No pudimos completar el registro. Probá de nuevo.")
        },
      }
    )
  }

  return (
    <div className="flex min-h-[70vh] items-center justify-center px-4">
      <Card className="w-full max-w-md border-border/60 shadow-sm">
        <CardHeader>
          <CardTitle className="text-xl">Completá tu negocio</CardTitle>
          <CardDescription>
            Ya iniciaste sesión. Antes de continuar, contanos sobre tu negocio
            de alquileres.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={handleSubmit}>
            <div className="space-y-2">
              <Label htmlFor="businessName">Nombre del negocio</Label>
              <Input
                id="businessName"
                placeholder="Ej: Alquileres del Sur"
                value={businessName}
                onChange={(event) => setBusinessName(event.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">Email de contacto</Label>
              <Input
                id="email"
                type="email"
                placeholder="contacto@negocio.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </div>
            <Button
              type="submit"
              className="w-full"
              disabled={registerLandlord.isPending}
            >
              {registerLandlord.isPending ? "Guardando..." : "Continuar"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
