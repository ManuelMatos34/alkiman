export interface Landlord {
  id: string
  businessName: string
  email: string
  createdAt: string
}

export interface RegisterLandlordRequest {
  businessName: string
  email: string
}
