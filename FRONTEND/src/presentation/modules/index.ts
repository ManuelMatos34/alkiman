import { alquileresModule } from "@/presentation/modules/alquileres"
import { carwashModule } from "@/presentation/modules/carwash"
import type { ModuleDefinition } from "@/presentation/modules/types"

/**
 * Registro de los módulos con UI en esta app. `App.tsx` lo recorre para montar, por cada
 * uno: el guard de módulo habilitado, el layout con su sidebar y sus rutas.
 *
 * Dar de alta el módulo N+1 son tres pasos y ninguno toca el ruteo:
 *   1. backend: `ModuleCodes` + entradas en `PermissionCatalog` + fila en `CFG_Modules`
 *      (y, si trae roles propios, un plan en `ModuleProvisioningCatalog`);
 *   2. front: `presentation/modules/<codigo>.tsx` con su `ModuleDefinition`;
 *   3. sumarlo a esta lista.
 *
 * El `code` tiene que coincidir con `CFG_Modules.Code`: es lo que compara `ModuleRoute`
 * contra el catálogo del backend y lo que usa `ModuleSelectorPage` para linkear.
 */
export const APP_MODULES: ModuleDefinition[] = [alquileresModule, carwashModule]

export type { ModuleDefinition } from "@/presentation/modules/types"
