import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"

export function NotFoundPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background text-center">
      <div>
        <h1 className="text-4xl font-semibold tracking-tight">404</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          La página que buscás no existe.
        </p>
      </div>
      <Button asChild size="sm">
        <Link to="/">Volver al inicio</Link>
      </Button>
    </div>
  )
}
