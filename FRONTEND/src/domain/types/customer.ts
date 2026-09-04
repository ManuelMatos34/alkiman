export interface Customer {
  id: string
  fullName: string
  identityNumber: string | null
  phone: string | null
  email: string | null
  address: string | null
  country: string | null
  createdAt: string
}

export interface CreateCustomerRequest {
  fullName: string
  identityNumber: string
  phone?: string | null
  email?: string | null
}

export interface UpdateCustomerRequest {
  fullName: string
  identityNumber: string
  phone?: string | null
  email?: string | null
}
