export interface Role {
  id: number
  name: string
  description: string | null
  isSystem: boolean
  permissions: string[]
  usersCount: number
  createdAt: string
}

export interface CreateRoleRequest {
  name: string
  description?: string | null
  permissions: string[]
}

export interface UpdateRoleRequest {
  name: string
  description?: string | null
  permissions: string[]
}
