export interface Category {
  id: number
  name: string
  createdAt: string
}

export interface CreateCategoryRequest {
  name: string
}

export interface UpdateCategoryRequest {
  name: string
}
