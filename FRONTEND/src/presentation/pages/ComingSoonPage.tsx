import { Link } from "react-router-dom"
import { ArrowLeft } from "lucide-react"
import { Button } from "@/components/ui/button"

export function ComingSoonPage({ title }: { title: string }) {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 text-center">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Estamos trabajando en esta sección. Pronto vas a poder gestionar
          {" "}
          {title.toLowerCase()} desde acá.
        </p>
      </div>
      <Button variant="outline" size="sm" asChild>
        <Link to="/">
          <ArrowLeft className="h-4 w-4" />
          Volver al dashboard
        </Link>
      </Button>
    </div>
  )
}
