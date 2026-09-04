/** Plantilla de contrato: un negocio puede tener varias por Categoría; solo una puede estar activa por categoría a la vez. */
export interface ContractTemplate {
  id: number
  categoryId: number
  categoryName: string
  name: string
  content: string
  isActive: boolean
  createdAt: string
}

export interface CreateContractTemplateRequest {
  categoryId: number
  name: string
  content: string
}

export interface UpdateContractTemplateRequest {
  name: string
  content: string
}
