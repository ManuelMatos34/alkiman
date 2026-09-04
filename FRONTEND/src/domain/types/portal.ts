/** Link público del Portal de Rentas (mantenimiento autenticado). */
export interface PortalLink {
  id: string
  assetGroupId: number
  assetGroupName: string
  title: string
  slug: string
  isActive: boolean
  createdAt: string
}

export interface CreatePortalLinkRequest {
  title: string
  assetGroupId: number
}

export interface UpdatePortalLinkRequest {
  title: string
  assetGroupId: number
  isActive: boolean
}
