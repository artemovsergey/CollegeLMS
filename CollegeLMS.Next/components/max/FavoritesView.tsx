"use client"

import { useCallback, useEffect, useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { Star, Users, GraduationCap } from "lucide-react"
import {
  Button,
  CellHeader,
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import { listFavorites, removeFavorite } from "@/api/favorites"
import type { Favorite } from "@/api/favorites"
import { useMaxContext, type ViewContext } from "@/lib/max-context"
import ScheduleError from "@/components/max/ScheduleError"

interface LocalItem {
  key: string
  targetType: "Group" | "Teacher"
  id: string
  name: string
  context: ViewContext
  isFavorite: boolean
}

function toLocal(fav: Favorite): LocalItem {
  const isGroup = fav.targetType === "Group"
  return {
    key: fav.id,
    targetType: fav.targetType,
    id: fav.targetId,
    name: isGroup
      ? (fav.groupName ?? "Группа")
      : (fav.teacherName ?? "Преподаватель"),
    context: isGroup
      ? { groupId: fav.targetId, groupName: fav.groupName ?? undefined }
      : { teacherId: fav.targetId, teacherName: fav.teacherName ?? undefined },
    isFavorite: true,
  }
}

export default function FavoritesView() {
  const { isAuthed, viewContext, setViewContext } = useMaxContext()
  const router = useRouter()
  const [items, setItems] = useState<LocalItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const favorites = await listFavorites()
      const local = favorites.map(toLocal)
      if (
        viewContext.groupId &&
        !local.some((i) => i.id === viewContext.groupId)
      ) {
        local.unshift({
          key: `context:group:${viewContext.groupId}`,
          targetType: "Group",
          id: viewContext.groupId,
          name: viewContext.groupName ?? "Группа",
          context: { groupId: viewContext.groupId, groupName: viewContext.groupName ?? undefined },
          isFavorite: false,
        })
      }
      if (
        viewContext.teacherId &&
        !local.some((i) => i.id === viewContext.teacherId)
      ) {
        local.unshift({
          key: `context:teacher:${viewContext.teacherId}`,
          targetType: "Teacher",
          id: viewContext.teacherId,
          name: viewContext.teacherName ?? "Преподаватель",
          context: { teacherId: viewContext.teacherId, teacherName: viewContext.teacherName ?? undefined },
          isFavorite: false,
        })
      }
      setItems(local)
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Не удалось загрузить избранное",
      )
    } finally {
      setLoading(false)
    }
  }, [viewContext])

  useEffect(() => {
    void load()
  }, [load])

  if (!isAuthed) {
    return (
      <MaxUI>
        <main className="max-app__page max-app__login-prompt">
          <Star size={32} className="max-app__state-icon" aria-hidden />
          <Typography.Title>
            Войдите, чтобы сохранять избранное
          </Typography.Title>
          <Typography.Body className="max-app__muted">
            Избранное хранится в вашем аккаунте
          </Typography.Body>
          <Link href="/login" passHref>
            <Button>Войти</Button>
          </Link>
        </main>
      </MaxUI>
    )
  }

  const groups = items.filter((i) => i.targetType === "Group")
  const teachers = items.filter((i) => i.targetType === "Teacher")

  const drop = async (item: LocalItem) => {
    setItems((prev) => prev.filter((i) => i.key !== item.key))
    if (!item.isFavorite) return
    const favs = await listFavorites().catch(() => [])
    const fav = favs.find((f) => f.targetId === item.id)
    if (fav) await removeFavorite(fav.id).catch(() => undefined)
  }

  const open = (item: LocalItem) => {
    setViewContext(item.context)
    router.push("/max/schedule")
  }

  const renderCell = (item: LocalItem) => (
    <CellSimple
      key={item.key}
      separator
      before={
        item.isFavorite ? (
          <Button
            size="small"
            variant="ghost"
            aria-label="Убрать из избранного"
            className="max-app__favorite max-app__favorite--on"
            onClick={() => void drop(item)}
            iconBefore={
              <Star size={18} fill="currentColor" aria-hidden />
            }
          />
        ) : (
          item.targetType === "Teacher" ? (
            <GraduationCap size={18} aria-hidden />
          ) : (
            <Users size={18} aria-hidden />
          )
        )
      }
      title={item.name}
      subtitle={
        item.isFavorite
          ? item.targetType === "Teacher"
            ? "Преподаватель"
            : "Группа"
          : "Текущий просмотр"
      }
      after={
        <Button size="small" variant="secondary" onClick={() => open(item)}>
          Открыть
        </Button>
      }
    />
  )

  return (
    <MaxUI>
      <main className="max-app__page">
        <header className="max-app__page-title">
          <Typography.Title>Избранное</Typography.Title>
        </header>
        {loading ? (
          <div className="max-app__state">
            <Spinner size={24} />
          </div>
        ) : error ? (
          <ScheduleError message={error} onRetry={() => void load()} />
        ) : items.length === 0 ? (
          <div className="max-app__state">
            <Star size={32} className="max-app__state-icon" aria-hidden />
            <Typography.Title>Пока пусто</Typography.Title>
            <Typography.Body className="max-app__muted">
              Отмечайте группы и преподавателей звёздочкой в поиске
            </Typography.Body>
          </div>
        ) : (
          <>
            {teachers.length > 0 ? (
              <CellList
                mode="island"
                header={
                  <CellHeader titleStyle="normal">
                    Преподаватели
                  </CellHeader>
                }
              >
                {teachers.map((item) => (
                  <div key={item.key}>{renderCell(item)}</div>
                ))}
              </CellList>
            ) : null}
            {groups.length > 0 ? (
              <CellList
                mode="island"
                header={
                  <CellHeader titleStyle="normal">Группы</CellHeader>
                }
              >
                {groups.map((item) => (
                  <div key={item.key}>{renderCell(item)}</div>
                ))}
              </CellList>
            ) : null}
          </>
        )}
      </main>
    </MaxUI>
  )
}