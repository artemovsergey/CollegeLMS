export interface ParsedErrors {
  fieldErrors: Record<string, string[]>
  message: string | null
}

export function parseErrors(err: unknown): ParsedErrors {
  const anyErr = err as {
    response?: {
      data?: {
        errors?: Record<string, string[]>
        errorMessage?: string
      }
    }
  }
  const data = anyErr?.response?.data
  if (data?.errors && Object.keys(data.errors).length > 0) {
    return { fieldErrors: data.errors, message: null }
  }
  return { fieldErrors: {}, message: data?.errorMessage ?? "Ошибка сети. Попробуйте позже" }
}
