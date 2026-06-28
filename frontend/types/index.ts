export interface User {
  id: string
  email: string
  fullName: string
  role: string
}

export interface Result<T> {
  isSuccess: boolean
  data: T | null
  errorMessage: string | null
  statusCode: number
}
