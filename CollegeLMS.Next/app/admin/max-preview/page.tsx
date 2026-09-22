"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import {
  GraduationCap,
  RefreshCw,
  Search,
  Smartphone,
  Users,
  X,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
import {
  searchSchedule,
  type ScheduleSearchGroup,
  type ScheduleSearchResponse,
  type ScheduleSearchTeacher,
} from "@/api/schedule"

const SECTIONS = [
  { value: "schedule", label: "Расписание" },
  { value: "favorites", label: "Избранное" },
  { value: "changes", label: "Изменения" },
  { value: "journal", label: "Журнал" },
] as const

type SectionValue = (typeof SECTIONS)[number]["value"]

interface PreviewContext {
  groupId?: string
  groupName?: string
  teacherId?: string
  teacherName?: string
}

export default function MaxPreviewPage() {
  const frameRef = useRef<HTMLIFrameElement>(null)
  const [section, setSection] = useState<SectionValue>("schedule")
  const [context, setContext] = useState<PreviewContext>({})
  const [query, setQuery] = useState("")
  const [results, setResults] = useState<ScheduleSearchResponse | null>(null)
  const [searching, setSearching] = useState(false)

  // Выбор группы/преподавателя на время тестирования: мини-апп читает
  // max-view-context из localStorage, поэтому пишем туда напрямую.
  const navigate = useCallback(
    (nextSection: SectionValue, nextContext: PreviewContext) => {
      const frame = frameRef.current
      if (!frame) return
      frame.onload = () => {
        try {
          const storage = frame.contentWindow?.localStorage
          if (nextContext.groupId || nextContext.teacherId) {
            storage?.setItem("max-view-context", JSON.stringify(nextContext))
          } else {
            storage?.removeItem("max-view-context")
          }
        } catch {
          // Доступ к storage может быть закрыт — превью продолжит работать
        }
        frame.onload = null
        frame.src = `/max/${nextSection}`
      }
      frame.src = "about:blank"
    },
    [],
  )

  useEffect(() => {
    navigate(section, context)
    // Первичная загрузка: контекст берётся из профиля CRM-пользователя
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [navigate])

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
    const next: PreviewContext = { groupId: group.id, groupName: group.name }
    setContext(next)
    setQuery("")
    setResults(null)
    navigate(section, next)
  }

  const pickTeacher = (teacher: ScheduleSearchTeacher) => {
    const next: PreviewContext = {
      teacherId: teacher.id,
      teacherName: teacher.fullName,
    }
    setContext(next)
    setQuery("")
    setResults(null)
    navigate(section, next)
  }

  const clearContext = () => {
    setContext({})
    navigate(section, {})
  }

  const changeSection = (value: string) => {
    const next = value as SectionValue
    setSection(next)
    navigate(next, context)
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
          Мини-приложение открывается в рамке, как в чате MAX. Вход выполняется
          текущим пользователем CRM, поэтому доступны свои разделы и данные.
          Кнопка «Сделать текущим» внутри превью не сохраняет выбор — для неё
          нужен токен MAX.
        </p>
      </header>

      <div className="flex flex-col gap-3 rounded-lg border bg-card p-4 lg:flex-row lg:flex-wrap lg:items-center">
        <NativeSelect
          value={section}
          onValueChange={changeSection}
          aria-label="Раздел мини-приложения"
          className="w-44 [&>select]:h-11"
        >
          {SECTIONS.map((item) => (
            <NativeSelectItem key={item.value} value={item.value}>
              {item.label}
            </NativeSelectItem>
          ))}
        </NativeSelect>

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
            onClick={() => navigate(section, context)}
          >
            <RefreshCw className="size-4" aria-hidden />
            Обновить
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
          ref={frameRef}
          title="Мини-апп MAX"
          className="h-[800px] w-full max-w-[400px] rounded-xl border bg-background"
        />
      </div>
    </div>
  )
}
