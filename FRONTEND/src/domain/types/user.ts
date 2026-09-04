export interface User {
  id: string
  fullName: string
  email: string
  isActive: boolean
  isOwner: boolean
  twoFactorEnabled: boolean
  roleId: number
  roleName: string
  createdAt: string
}

export interface CreateUserRequest {
  fullName: string
  email: string
  roleId: number
}

/**
 * Respuesta del alta. La contraseña temporal no viene acá a propósito: sólo la
 * recibe su dueño por email. `welcomeEmailSent` en false significa que el usuario
 * quedó creado pero el correo no salió, y hay que avisarle al admin.
 */
export interface CreateUserResponse extends User {
  welcomeEmailSent: boolean
}

export interface UpdateUserRequest {
  fullName: string
  roleId: number
  isActive: boolean
}
