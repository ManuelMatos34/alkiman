export interface Me {
  id: string
  fullName: string
  email: string
  role: string
  isOwner: boolean
  permissions: string[]
  twoFactorEnabled: boolean
  landlordId: string
  businessName: string
  createdAt: string
}

export interface UpdateMeRequest {
  fullName: string
  email: string
}

export interface ChangeMyPasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface UpdateMyTwoFactorRequest {
  enabled: boolean
}
