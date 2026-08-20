import axios from "axios"
import { toast } from "sonner"
import type { Result, CourseDocumentResponse } from "@/types"

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

function unwrap<T>(res: { data: Result<T> }): T {
  if (!res.data.isSuccess || res.data.data === null) {
    throw new Error(res.data.errorMessage ?? "Ошибка запроса")
  }
  return res.data.data
}

export async function uploadCourseDocument(
  courseId: string,
  file: File,
): Promise<CourseDocumentResponse> {
  const form = new FormData()
  form.append("file", file)
  const res = await api.post<Result<CourseDocumentResponse>>(
    `/api/courses/${courseId}/documents`,
    form,
  )
  return unwrap(res)
}

export async function deleteCourseDocument(courseId: string, id: string): Promise<void> {
  const res = await api.delete<Result<null>>(`/api/courses/${courseId}/documents/${id}`)
  if (!res.data.isSuccess) throw new Error(res.data.errorMessage ?? "Ошибка удаления")
}

export async function downloadCourseDocument(
  courseId: string,
  id: string,
  fileName: string,
): Promise<void> {
  const res = await api.get<Blob>(`/api/courses/${courseId}/documents/${id}/download`, {
    responseType: "blob",
  })
  const url = URL.createObjectURL(res.data)
  const a = document.createElement("a")
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

export async function reorderLessons(courseId: string, lessonIds: string[]): Promise<void> {
  const res = await api.put<Result<null>>(`/api/courses/${courseId}/lessons/reorder`, {
    lessonIds,
  })
  if (!res.data.isSuccess) throw new Error(res.data.errorMessage ?? "Ошибка сохранения порядка")
}

export async function setCurrentLesson(
  courseId: string,
  id: string,
  isCurrent: boolean,
): Promise<void> {
  const res = await api.patch<Result<null>>(`/api/courses/${courseId}/lessons/${id}/current`, {
    isCurrent,
  })
  if (!res.data.isSuccess) throw new Error(res.data.errorMessage ?? "Ошибка обновления")
}
