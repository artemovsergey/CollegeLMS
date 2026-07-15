import { type ClassValue, clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function extractErrorMessage(err: unknown): string | null {
  const body = (err as { response?: { data?: Record<string, unknown> } })?.response?.data
  if (!body) return null

  // Our Result<T> format: { errorMessage: "..." }
  if (typeof body.errorMessage === "string") return body.errorMessage

  // ErrorResponse from middleware: { message: "..." }
  if (typeof body.message === "string") return body.message

  // ProblemDetails from FluentValidation: { title: "...", errors: { field: ["msg"] } }
  if (body.errors && typeof body.errors === "object") {
    const errors = body.errors as Record<string, string[]>
    const firstField = Object.keys(errors)[0]
    if (firstField && errors[firstField]?.[0]) return errors[firstField][0]
  }

  // ProblemDetails title fallback
  if (typeof body.title === "string") return body.title

  return null
}
