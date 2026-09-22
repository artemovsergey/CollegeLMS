"use client"

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react"
import { useRouter } from "next/navigation"
import api from "@/lib/api"
import { loginWithMax } from "@/api/auth"
import { saveMaxSelection } from "@/api/selection"
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
  // Что пользователь просматривает сейчас (в т.ч. без сохранения).
  viewContext: ViewContext
  // Что реально сохранено в боте (производное от профиля) — источник правды.
  currentSelection: ViewContext
  setViewContext: (ctx: ViewContext) => void
  makeCurrentSelection: (target: ViewContext) => Promise<void>
  deepLink: MaxDeepLink | null
  loading: boolean
  reload: () => void
}

const EMPTY: ViewContext = {}

const VIEW_CONTEXT_KEY = "max-view-context"

// MAX-JWT хранится отдельно от CRM-токена, чтобы не перетирать CRM-сессию.
const MAX_TOKEN_KEY = "max-token"

// MAX Bridge подключается асинхронно (next/script afterInteractive), поэтому
// initData может появиться уже после первого рендера. Ждём его с ограниченным
// таймаутом; в обычном браузере без моста window.WebApp не появится — по
// таймауту уходим в CRM-ветку, не дёргая /api/auth/max.
const MAX_BRIDGE_TIMEOUT_MS = 1500
const MAX_BRIDGE_POLL_MS = 50

function readMaxInitData(): string | undefined {
  if (typeof window === "undefined") return undefined
  return (window as unknown as { WebApp?: { initData?: string } }).WebApp?.initData
}

async function waitForMaxInitData(
  timeoutMs: number = MAX_BRIDGE_TIMEOUT_MS,
): Promise<string | null> {
  const immediate = readMaxInitData()
  if (immediate) return immediate

  const deadline = Date.now() + timeoutMs
  while (Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, MAX_BRIDGE_POLL_MS))
    const value = readMaxInitData()
    if (value) return value
  }
  return null
}

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
  currentSelection: EMPTY,
  setViewContext: () => {},
  makeCurrentSelection: async () => {},
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

  // Профиль бота — источник правды о сохранённом выборе. Если профиль пуст
  // (бот недоступен или выбор ещё не задан) — пустой объект.
  const currentSelection = useMemo<ViewContext>(() => {
    const ctx: ViewContext = {}
    if (profile?.groupId) ctx.groupId = profile.groupId
    if (profile?.groupName) ctx.groupName = profile.groupName
    if (profile?.teacherId) ctx.teacherId = profile.teacherId
    if (profile?.teacherName) ctx.teacherName = profile.teacherName
    return ctx
  }, [profile])

  const setViewContext = useCallback((ctx: ViewContext) => {
    storeViewContext(ctx)
    setViewContextState(ctx)
  }, [])

  // Сохраняет выбор группы/преподавателя в боте MAX. Оптимистично применяет
  // выбор локально, при ошибке — откатывает и пробрасывает её вызывающему,
  // чтобы компонент показал сообщение пользователю.
  const makeCurrentSelection = useCallback(
    async (target: ViewContext) => {
      const previous = viewContext
      setViewContext(target)
      try {
        // Ровно одна цель: группа приоритетнее, иначе преподаватель. Без имён
        // и без второго id — бэкенд принимает только один идентификатор.
        const payload: { groupId?: string; teacherId?: string } = {}
        if (target.groupId) payload.groupId = target.groupId
        else if (target.teacherId) payload.teacherId = target.teacherId

        const data = await saveMaxSelection(payload)
        localStorage.setItem(MAX_TOKEN_KEY, data.token)
        setProfile((prev) => ({
          ...prev,
          ...data.profile,
          // В selection-запросе нет initData: fullName приходит только если
          // бот его отдал — не затираем прежнее значение.
          fullName: data.profile.fullName ?? prev?.fullName,
        }))
      } catch (err) {
        setViewContext(previous)
        throw err
      }
    },
    [viewContext, setViewContext],
  )

  const reload = useCallback(() => {
    setLoading(true)

    void (async () => {
      const initData = await waitForMaxInitData()

      if (initData) {
        loginWithMax(initData)
          .then((res) => {
            localStorage.setItem(MAX_TOKEN_KEY, res.token)
            setIsAuthed(true)
            setProfile({
              id: String(res.profile.maxUserId),
              fullName: res.profile.fullName ?? null,
              role: res.profile.role,
              teacherId: res.profile.teacherId ?? null,
              teacherName: res.profile.teacherName ?? null,
              groupId: res.profile.groupId ?? null,
              groupName: res.profile.groupName ?? null,
            })
            const own: ViewContext = {}
            if (res.profile.groupId) own.groupId = res.profile.groupId
            if (res.profile.groupName) own.groupName = res.profile.groupName
            if (res.profile.teacherId) own.teacherId = res.profile.teacherId
            if (res.profile.teacherName) own.teacherName = res.profile.teacherName
            setViewContextState((prev) => {
              // Профиль бота — источник правды: выбор могли поменять в чате,
              // пока мини-апп был закрыт, поэтому свежий выбор перекрывает
              // устаревший локальный. Локальный остаётся только при пустом
              // профиле (бот недоступен или выбор ещё не задан).
              const next = Object.keys(own).length > 0 ? own : prev
              storeViewContext(next)
              return next
            })
          })
          .catch(() => {
            setIsAuthed(false)
            setProfile(null)
          })
          .finally(() => setLoading(false))
        return
      }

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
    })()
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
      value={{
        isAuthed,
        profile,
        viewContext,
        currentSelection,
        setViewContext,
        makeCurrentSelection,
        deepLink,
        loading,
        reload,
      }}
    >
      {children}
    </MaxContext.Provider>
  )
}

export function useMaxContext() {
  return useContext(MaxContext)
}