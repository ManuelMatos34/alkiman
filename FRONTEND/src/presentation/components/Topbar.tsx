import { type ReactNode } from "react"
import { LayoutGrid, LogOut, Settings, UserCog, ShieldCheck } from "lucide-react"
import { useNavigate } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { MobileSidebar } from "@/presentation/components/MobileSidebar"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { PermissionCodes } from "@/domain/types/permission"

function getInitials(name: string) {
  return name
    .split(" ")
    .map((part) => part[0])
    .slice(0, 2)
    .join("")
    .toUpperCase()
}

interface TopbarProps {
  /**
   * Contenido del drawer móvil (nav del módulo actual). Si se omite, no se muestra el botón
   * hamburguesa -- ej: en el layout general/selector de módulos, que no tiene sidebar propio.
   */
  mobileNav?: (onNavigate: () => void) => ReactNode
}

export function Topbar({ mobileNav }: TopbarProps) {
  const { user, logout, hasPermission } = useAuth()
  const navigate = useNavigate()
  const { t } = useTranslation("nav")

  const displayName = user?.fullName ?? t("defaultUser")

  function handleLogout() {
    logout()
    navigate("/login", { replace: true })
  }

  function handleChangeModule() {
    navigate("/")
  }

  return (
    <header className="flex h-16 items-center justify-between gap-3 border-b border-border bg-card px-4 sm:px-6">
      <div className="flex min-w-0 items-center gap-2">
        {mobileNav && <MobileSidebar renderNav={mobileNav} />}
        <div className="min-w-0">
          <p className="truncate text-sm font-medium text-foreground">
            {displayName}
          </p>
          {user && (
            <p className="truncate text-xs text-muted-foreground">
              {user.email}
            </p>
          )}
        </div>
      </div>

      <DropdownMenu>
        <DropdownMenuTrigger className="rounded-full outline-none ring-offset-2 focus-visible:ring-2 focus-visible:ring-ring">
          <Avatar className="h-9 w-9">
            <AvatarFallback>{getInitials(displayName)}</AvatarFallback>
          </Avatar>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-48">
          <DropdownMenuLabel className="truncate">
            {user?.email}
          </DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={handleChangeModule}>
            <LayoutGrid className="h-4 w-4" />
            {t("changeModule")}
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => navigate("/ajustes")}>
            <Settings className="h-4 w-4" />
            {t("settings")}
          </DropdownMenuItem>
          {hasPermission(PermissionCodes.UsersManage) && (
            <DropdownMenuItem onClick={() => navigate("/usuarios")}>
              <UserCog className="h-4 w-4" />
              {t("users")}
            </DropdownMenuItem>
          )}
          {hasPermission(PermissionCodes.RolesManage) && (
            <DropdownMenuItem onClick={() => navigate("/roles")}>
              <ShieldCheck className="h-4 w-4" />
              {t("roles")}
            </DropdownMenuItem>
          )}
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={handleLogout}>
            <LogOut className="h-4 w-4" />
            {t("logout")}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </header>
  )
}
