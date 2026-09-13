"use client"

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import api from "@/lib/api"

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
  loading: boolean
  reload: () => void
}

const EMPTY: ViewContext = {}

const MaxContext = createContext<MaxContextValue>({
  isAuthed: false,
  profile: null,
  viewContext: EMPTY,
  setViewContext: () => {},
  loading: true,
  reload: () => {},
})

export function MaxContextProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState<MaxProfile | null>(null)
  const [isAuthed, setIsAuthed] = useState(false)
  const [loading, setLoading] = useState(true)
  const [viewContext, setViewContext] = useState<ViewContext>(EMPTY)

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
          setViewContext((prev) =>
            Object.keys(prev).length === 0 ? own : prev,
          )
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

  return (
    <MaxContext.Provider
      value={{ isAuthed, profile, viewContext, setViewContext, loading, reload }}
    >
      {children}
    </MaxContext.Provider>
  )
}

export function useMaxContext() {
  return useContext(MaxContext)
}