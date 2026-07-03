export interface User {
  id: string
  email: string
  fullName: string
  role: string
  isActive: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

export interface CreateUserRequest {
  email: string
  password: string
  fullName: string
  role: string
}

export interface UpdateUserRequest {
  email: string
  fullName: string
  role: string
}

export interface ChangeRoleRequest {
  role: string
}

export interface Result<T> {
  isSuccess: boolean
  data: T | null
  errorMessage: string | null
  statusCode: number
}
