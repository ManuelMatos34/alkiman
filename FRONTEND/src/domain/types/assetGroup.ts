export interface AssetGroup {
  id: number
  name: string
  description: string | null
  assetIds: string[]
  assetsCount: number
  createdAt: string
}

export interface CreateAssetGroupRequest {
  name: string
  description?: string | null
  assetIds: string[]
}

export interface UpdateAssetGroupRequest {
  name: string
  description?: string | null
  assetIds: string[]
}
