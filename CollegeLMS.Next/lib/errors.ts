export interface ParsedErrors {
  fieldErrors: Record<string, string[]>
  message: string | null
}

function toCamelCase(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1)
}

export function parseErrors(err: unknown): ParsedErrors {
  const anyErr = err as {
    response?: {
      data?: {
        errors?: Record<string, string | string[]>
        errorMessage?: string
        message?: string
      }
    }
  }
  const data = anyErr?.response?.data
  const fieldErrors: Record<string, string[]> = {}
  let firstMessage: string | null = null
  if (data?.errors && typeof data.errors === "object") {
    for (const [key, value] of Object.entries(data.errors)) {
      const list = Array.isArray(value) ? value : [value]
      fieldErrors[toCamelCase(key)] = list
      if (firstMessage === null && list.length > 0) firstMessage = list[0]
    }
  }
  if (Object.keys(fieldErrors).length > 0) {
    return { fieldErrors, message: data?.errorMessage ?? firstMessage }
  }
  return {
    fieldErrors: {},
    message: data?.errorMessage ?? data?.message ?? "Ошибка сети. Попробуйте позже",
  }
}
