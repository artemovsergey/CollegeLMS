"use client"

import { useEffect, useMemo, useState } from "react"
import {
  ExternalLink,
  GraduationCap,
  RefreshCw,
  Search,
  Smartphone,
  Users,
  X,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  searchSchedule,
  type ScheduleSearchGroup,
  type ScheduleSearchResponse,
  type ScheduleSearchTeacher,
} from "@/api/schedule"

interface PreviewContext {
  groupId?: string
  groupName?: string
  teacherId?: string
  teacherName?: string
}

/**
 * Превью расписания в рамке телефона: та же страница, что в веб-версии
 * (`/schedule`), но в embed-режиме — без шапки CRM. Так проверяют, как
 * экран расписания будет выглядеть на телефоне.
 */
export default function MaxPreviewPage() {
  const [context, setContext] = useState<PreviewContext>({})
  const [query, setQuery] = useState("")
  const [results, setResults] = useState<ScheduleSearchResponse | null>(null)
  const [searching, setSearching] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)

  const src = useMemo(() => {
    const params = new URLSearchParams({ embed: "1" })
    if (context.groupId) params.set("groupId", context.groupId)
    else if (context.teacherId) params.set("teacherId", context.teacherId)
    return `/schedule?${params.toString()}`
  }, [context])

  useEffect(() => {
    const q = query.trim()
    if (q.length === 0) {
      setResults(null)
      setSearching(false)
      return
    }
    const timer = window.setTimeout(async () => {
      setSearching(true)
      try {
        const res = await searchSchedule(q)
        setResults(res.data ?? null)
      } catch {
        setResults(null)
      } finally {
        setSearching(false)
      }
    }, 300)
    return () => window.clearTimeout(timer)
  }, [query])

  const pickGroup = (group: ScheduleSearchGroup) => {
    setContext({ groupId: group.id, groupName: group.name })
    setQuery("")
    setResults(null)
  }

  const pickTeacher = (teacher: ScheduleSearchTeacher) => {
    setContext({ teacherId: teacher.id, teacherName: teacher.fullName })
    setQuery("")
    setResults(null)
  }

  const clearContext = () => {
    setContext({})
  }

  const activeName = context.groupName ?? context.teacherName

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-4 p-6">
      <header className="flex flex-col gap-1">
        <h2 className="flex items-center gap-2 text-xl font-semibold">
          <Smartphone className="size-5" aria-hidden />
          Мини-апп MAX — превью
        </h2>
        <p className="text-sm text-muted-foreground">
          Расписание в рамке телефона: та же страница и логика, что в
          веб-версии, только без шапки CRM. Вход выполняется текущим
          пользователем. Выберите группу или преподавателя, чтобы посмотреть
          расписание на телефоне.
        </p>
      </header>

      <div className="flex flex-col gap-3 rounded-lg border bg-card p-4 lg:flex-row lg:flex-wrap lg:items-center">
        <div className="relative min-w-56 flex-1">
          <Input
            type="search"
            value={query}
            placeholder="Группа или преподаватель"
            aria-label="Поиск группы или преподавателя"
            onChange={(e) => setQuery(e.target.value)}
            className="h-11"
          />
          {query.trim().length > 0 ? (
            <div className="absolute z-20 mt-1 max-h-72 w-full overflow-auto rounded-md border bg-popover p-2 shadow-md">
              {searching ? (
                <p className="px-2 py-1 text-sm text-muted-foreground">
                  Поиск…
                </p>
              ) : (results?.groups.length ?? 0) +
                  (results?.teachers.length ?? 0) ===
                0 ? (
                <p className="px-2 py-1 text-sm text-muted-foreground">
                  Ничего не найдено
                </p>
              ) : (
                <>
                  {results!.groups.map((group) => (
                    <button
                      key={group.id}
                      type="button"
                      onClick={() => pickGroup(group)}
                      className="flex w-full items-center gap-2 rounded-md px-2 py-2 text-left text-sm hover:bg-accent"
                    >
                      <Users className="size-4 text-muted-foreground" aria-hidden />
                      {group.name}
                    </button>
                  ))}
                  {results!.teachers.map((teacher) => (
                    <button
                      key={teacher.id}
                      type="button"
                      onClick={() => pickTeacher(teacher)}
                      className="flex w-full items-center gap-2 rounded-md px-2 py-2 text-left text-sm hover:bg-accent"
                    >
                      <GraduationCap
                        className="size-4 text-muted-foreground"
                        aria-hidden
                      />
                      {teacher.fullName}
                    </button>
                  ))}
                </>
              )}
            </div>
          ) : null}
        </div>

        <div className="flex flex-wrap items-center gap-2 lg:ml-auto">
          <Button
            variant="outline"
            size="sm"
            className="h-11"
            onClick={() => setRefreshKey((key) => key + 1)}
          >
            <RefreshCw className="size-4" aria-hidden />
            Обновить
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="h-11"
            onClick={() => window.open(src, "_blank", "noopener")}
          >
            <ExternalLink className="size-4" aria-hidden />
            Открыть в новой вкладке
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2 text-sm">
        <span className="text-muted-foreground">Цель просмотра:</span>
        {activeName ? (
          <span className="flex items-center gap-2 rounded-full border border-primary/30 bg-primary/5 px-3 py-1">
            {activeName}
            <Button
              variant="ghost"
              size="sm"
              className="size-6 p-0"
              aria-label="Сбросить выбор"
              onClick={clearContext}
            >
              <X className="size-3.5" aria-hidden />
            </Button>
          </span>
        ) : (
          <span className="flex items-center gap-1.5 text-muted-foreground">
            <Search className="size-3.5" aria-hidden />
            своя группа или преподаватель из профиля
          </span>
        )}
      </div>

      <div className="flex justify-center">
        <iframe
          key={`${src}-${refreshKey}`}
          title="Мини-апп MAX"
          src={src}
          className="h-[852px] w-[393px] max-w-full rounded-[2rem] border bg-background shadow-sm"
        />
      </div>
    </div>
  )
}
