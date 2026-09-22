"use client"

import { useCallback, useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { AlertCircle, Star, Users, GraduationCap } from "lucide-react"
import {
  Button,
  CellHeader,
  CellList,
  CellSimple,
  MaxUI,
  Typography,
} from "@maxhub/max-ui"
import { useMaxContext, type ViewContext } from "@/lib/max-context"
import {
  FAVORITES_EVENT,
  listLocalFavorites,
  removeLocalFavorite,
  type LocalFavorite,
} from "@/lib/max-favorites"

interface LocalItem {
  key: string
  targetType: "Group" | "Teacher"
  id: string
  name: string
  context: ViewContext
  isFavorite: boolean
}

function toLocal(fav: LocalFavorite): LocalItem {
  const isGroup = fav.targetType === "Group"
  return {
    key: fav.id,
    targetType: fav.targetType,
    id: fav.targetId,
    name: fav.name,
    context: isGroup
      ? { groupId: fav.targetId, groupName: fav.name }
      : { teacherId: fav.targetId, teacherName: fav.name },
    isFavorite: true,
  }
}

export default function FavoritesView() {
  const { viewContext, makeCurrentSelection } = useMaxContext()
  const router = useRouter()
  const [items, setItems] = useState<LocalItem[]>([])
  const [pendingId, setPendingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(() => {
    const local = listLocalFavorites().map(toLocal)
    if (
      viewContext.groupId &&
      !local.some((i) => i.id === viewContext.groupId)
    ) {
      local.unshift({
        key: `context:group:${viewContext.groupId}`,
        targetType: "Group",
        id: viewContext.groupId,
        name: viewContext.groupName ?? "Группа",
        context: {
          groupId: viewContext.groupId,
          groupName: viewContext.groupName ?? undefined,
        },
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
        context: {
          teacherId: viewContext.teacherId,
          teacherName: viewContext.teacherName ?? undefined,
        },
        isFavorite: false,
      })
    }
    setItems(local)
  }, [viewContext])

  useEffect(() => {
    load()
    window.addEventListener(FAVORITES_EVENT, load)
    window.addEventListener("storage", load)
    return () => {
      window.removeEventListener(FAVORITES_EVENT, load)
      window.removeEventListener("storage", load)
    }
  }, [load])

  const groups = items.filter((i) => i.targetType === "Group")
  const teachers = items.filter((i) => i.targetType === "Teacher")

  const drop = (item: LocalItem) => {
    if (!item.isFavorite) return
    removeLocalFavorite(item.targetType, item.id)
  }

  const open = async (item: LocalItem) => {
    if (pendingId !== null) return
    setPendingId(item.key)
    setError(null)
    try {
      await makeCurrentSelection(item.context)
      router.push("/max/schedule")
    } catch {
      setError("Не удалось сохранить выбор. Попробуйте позже.")
    } finally {
      setPendingId(null)
    }
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
            onClick={() => drop(item)}
            iconBefore={<Star size={18} fill="currentColor" aria-hidden />}
          />
        ) : item.targetType === "Teacher" ? (
          <GraduationCap size={18} aria-hidden />
        ) : (
          <Users size={18} aria-hidden />
        )
      }
      title={item.name}
      subtitle={
        item.isFavorite
          ? item.targetType === "Teacher"
            ? "Преподаватель"
            : "Группа"
          : "Текущий выбор"
      }
      after={
        <Button
          size="small"
          variant="secondary"
          disabled={pendingId !== null}
          onClick={() => open(item)}
        >
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
        {error ? (
          <div className="max-app__confirm-error" role="alert">
            <AlertCircle
              size={18}
              className="max-app__confirm-error-icon"
              aria-hidden
            />
            <Typography.Body>{error}</Typography.Body>
          </div>
        ) : null}
        {items.length === 0 ? (
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
                  <CellHeader titleStyle="normal">Преподаватели</CellHeader>
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
                header={<CellHeader titleStyle="normal">Группы</CellHeader>}
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