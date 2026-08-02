import axios from "axios"
import { toast } from "sonner"

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "",
  headers: { "Content-Type": "application/json" },
})

api.interceptors.request.use(config => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("token")
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
  }
  return config
})

const TOAST_DEBOUNCE_MS = 5000
const lastShownAt: Record<number, number> = {}

function showDebouncedToast(status: number, message: string) {
  const now = Date.now()
  if (now - (lastShownAt[status] ?? 0) < TOAST_DEBOUNCE_MS) return
  lastShownAt[status] = now
  toast.error(message)
}

api.interceptors.response.use(
  response => response,
  error => {
    const status = error.response?.status as number | undefined
    if (typeof window !== "undefined" && status) {
      if (status === 401) {
        localStorage.removeItem("token")
        localStorage.removeItem("user")
        if (!window.location.pathname.startsWith("/login")) {
          window.location.href = "/login"
        }
      } else if (status === 500) {
        showDebouncedToast(500, "Внутренняя ошибка сервера")
      } else if (status === 504) {
        showDebouncedToast(504, "Проблемы на стороне провайдера (504). Попробуйте позже")
      }
    }
    return Promise.reject(error)
  },
)

export default api
