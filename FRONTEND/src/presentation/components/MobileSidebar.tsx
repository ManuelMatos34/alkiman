import { useState, type ReactNode } from "react"
import { Menu } from "lucide-react"
import { useTranslation } from "react-i18next"
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"

interface MobileSidebarProps {
  /**
   * Contenido del drawer. Recibe `onNavigate`, que hay que engancharle a la navegación del nav
   * para que el drawer se cierre solo al elegir un ítem. Así este componente no depende del nav
   * de ningún módulo en particular -- cada layout le pasa el suyo.
   */
  renderNav: (onNavigate: () => void) => ReactNode
}

/** Drawer de navegación para pantallas menores a `md`, donde el sidebar fijo está oculto. */
export function MobileSidebar({ renderNav }: MobileSidebarProps) {
  const [open, setOpen] = useState(false)
  const { t } = useTranslation("nav")

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button variant="ghost" size="icon" className="md:hidden">
          <Menu className="h-5 w-5" />
          <span className="sr-only">{t("openMenu")}</span>
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-64 gap-0 bg-card p-0">
        <SheetTitle className="sr-only">{t("navigationMenu")}</SheetTitle>
        {renderNav(() => setOpen(false))}
      </SheetContent>
    </Sheet>
  )
}
