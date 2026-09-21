"use client"

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { useRouter } from "next/navigation"
import api from "@/lib/api"
import { resolveMaxDeepLink, type MaxDeepLink } from "@/lib/max-deeplink"

export interface ViewContext {
  groupId?: string
  groupName?: string
  teacherId?: string
  teacherName?: string
}

export type MaxRole = "Student" | "Teacher" | "Other"

export interface MaxProfile {
  id?: string | null
  fullName?: string | null
  role: MaxRole
  teacherId?: string | null
  teacherName?: string | null
  groupId?: string | null
  groupName?: string | null
}

interface ScheduleContextDto {
  role: string
  groupId?: string | null
  groupName?: string | null
  teacherId?: string | null
  teacherName?: string | null
}

interface MaxContextValue {
  isAuthed: boolean
  profile: MaxProfile | null
  viewContext: ViewContext
  setViewContext: (ctx: ViewContext) => void
  deepLink: MaxDeepLink | null
  loading: boolean
  reload: () => void
}

const EMPTY: ViewContext = {}

const VIEW_CONTEXT_KEY = "max-view-context"

function readStoredViewContext(): ViewContext {
  if (typeof window === "undefined") return EMPTY
  try {
    const raw = localStorage.getItem(VIEW_CONTEXT_KEY)
    if (!raw) return EMPTY
    const parsed = JSON.parse(raw) as ViewContext
    const ctx: ViewContext = {}
    if (typeof parsed.groupId === "string" && parsed.groupId) ctx.groupId = parsed.groupId
    if (typeof parsed.groupName === "string" && parsed.groupName) ctx.groupName = parsed.groupName
    if (typeof parsed.teacherId === "string" && parsed.teacherId) ctx.teacherId = parsed.teacherId
    if (typeof parsed.teacherName === "string" && parsed.teacherName) ctx.teacherName = parsed.teacherName
    return ctx
  } catch {
    return EMPTY
  }
}

function storeViewContext(ctx: ViewContext): void {
  if (typeof window === "undefined") return
  try {
    if (Object.keys(ctx).length === 0) localStorage.removeItem(VIEW_CONTEXT_KEY)
    else localStorage.setItem(VIEW_CONTEXT_KEY, JSON.stringify(ctx))
  } catch {
    // приватный режим — выбор просто не сохранится
  }
}

const MaxContext = createContext<MaxContextValue>({
  isAuthed: false,
  profile: null,
  viewContext: EMPTY,
  setViewContext: () => {},
  deepLink: null,
  loading: true,
  reload: () => {},
})

export function MaxContextProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState<MaxProfile | null>(null)
  const [isAuthed, setIsAuthed] = useState(false)
  const [loading, setLoading] = useState(true)
  const [viewContext, setViewContextState] = useState<ViewContext>(() => readStoredViewContext())
  const [deepLink, setDeepLink] = useState<MaxDeepLink | null>(null)
  const router = useRouter()

  const setViewContext = useCallback((ctx: ViewContext) => {
    storeViewContext(ctx)
    setViewContextState(ctx)
  }, [])

  const reload = useCallback(() => {
    setLoading(true)
    const token =
      typeof window !== "undefined" ? localStorage.getItem("token") : null
    if (!token) {
      setIsAuthed(false)
      setProfile(null)
      setLoading(false)
      return
    }
    api
      .get<{ data: ScheduleContextDto | null }>("/api/schedule/context")
      .then((res) => {
        const ctx = res.data?.data
        if (ctx) {
          setIsAuthed(true)
          setProfile({
            role: (ctx.role as MaxRole) ?? "Other",
            teacherId: ctx.teacherId ?? null,
            teacherName: ctx.teacherName ?? null,
            groupId: ctx.groupId ?? null,
            groupName: ctx.groupName ?? null,
          })
          const own: ViewContext = {}
          if (ctx.groupId) own.groupId = ctx.groupId
          if (ctx.groupName) own.groupName = ctx.groupName
          if (ctx.teacherId) own.teacherId = ctx.teacherId
          if (ctx.teacherName) own.teacherName = ctx.teacherName
          setViewContextState((prev) => {
            const next = Object.keys(prev).length === 0 ? own : prev
            storeViewContext(next)
            return next
          })
        } else {
          setIsAuthed(false)
        }
      })
      .catch(() => setIsAuthed(false))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    reload()
  }, [reload])

  useEffect(() => {
    // Deep-link из кнопки open_app: MAX Bridge передаёт payload в start_param.
    const startParam = (
      window as unknown as {
        WebApp?: { initDataUnsafe?: { start_param?: string } }
      }
    ).WebApp?.initDataUnsafe?.start_param
    if (!startParam) return

    const link = resolveMaxDeepLink(window.location.search, startParam)
    setDeepLink(link)

    if (link.groupId || link.teacherId) {
      setViewContext({ groupId: link.groupId, teacherId: link.teacherId })
    }

    if (link.route === "changes") {
      const qs = link.id ? `?route=changes&id=${link.id}` : "?route=changes"
      router.replace(`/max/changes${qs}`)
      return
    }

    const params = new URLSearchParams()
    params.set("route", link.route)
    if (link.date) params.set("date", link.date)
    router.replace(`/max/schedule?${params.toString()}`)
  }, [router, setViewContext])

  return (
    <MaxContext.Provider
      value={{ isAuthed, profile, viewContext, setViewContext, deepLink, loading, reload }}
    >
      {children}
    </MaxContext.Provider>
  )
}

export function useMaxContext() {
  return useContext(MaxContext)
}