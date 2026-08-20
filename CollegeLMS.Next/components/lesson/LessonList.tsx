"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import {
  DndContext,
  closestCenter,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core"
import {
  SortableContext,
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import type { LessonResponse } from "@/types"
import { reorderLessons, setCurrentLesson } from "@/lib/api"
import { LESSON_KIND_LABELS, LESSON_KIND_VARIANTS } from "@/lib/lessonTypes"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { toast } from "sonner"
import { GripVertical, FileCheck, Pencil, Trash2, BookOpenCheck } from "lucide-react"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import api from "@/lib/api"

function SortableRow({
  lesson,
  canManage,
  onOpen,
  onToggleCurrent,
  onEdit,
  onAskDelete,
}: {
  lesson: LessonResponse
  canManage: boolean
  onOpen: (id: string) => void
  onToggleCurrent: (id: string, value: boolean) => void
  onEdit: (id: string) => void
  onAskDelete: (id: string) => void
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: lesson.id,
    disabled: !canManage,
  })
  const style = { transform: CSS.Transform.toString(transform), transition }
  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`flex items-center gap-2 p-4 cursor-pointer hover:bg-muted/50 ${isDragging ? "opacity-50" : ""}`}
      onClick={() => onOpen(lesson.id)}
    >
      {canManage && (
        <button
          type="button"
          className="cursor-grab touch-none p-1 text-muted-foreground hover:text-foreground"
          {...attributes}
          {...listeners}
          aria-label="Перетащить занятие"
          onClick={e => e.stopPropagation()}
        >
          <GripVertical className="size-4" />
        </button>
      )}
      <span className="text-sm text-muted-foreground w-6 shrink-0">{lesson.order}</span>
      <span className="font-medium min-w-0 flex-1 truncate">{lesson.title}</span>
      <Badge className="shrink-0" variant={LESSON_KIND_VARIANTS[lesson.kind] ?? "outline"}>
        {LESSON_KIND_LABELS[lesson.kind] ?? lesson.kind}
      </Badge>
      {lesson.testId && (
        <Badge variant="outline" className="shrink-0">
          <FileCheck className="size-3 mr-1" />
          Тест
        </Badge>
      )}
      {canManage ? (
        <div className="flex items-center gap-1 shrink-0" onClick={e => e.stopPropagation()}>
          <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Switch
              checked={lesson.isCurrent}
              onCheckedChange={v => onToggleCurrent(lesson.id, v)}
              aria-label="Сейчас идёт"
            />
            <span className="hidden sm:inline">Сейчас идёт</span>
          </label>
          <Button variant="ghost" size="sm" onClick={() => onEdit(lesson.id)}>
            <Pencil className="size-4" />
          </Button>
          <Button variant="ghost" size="sm" className="text-muted-foreground hover:text-fg" onClick={() => onAskDelete(lesson.id)}>
            <Trash2 className="size-4" />
          </Button>
        </div>
      ) : (
        lesson.isCurrent && (
          <Badge className="shrink-0" variant="default">
            <BookOpenCheck className="size-3 mr-1" />
            Сейчас идёт
          </Badge>
        )
      )}
    </div>
  )
}

export default function LessonList({
  courseId,
  lessons,
  canManage,
  onChanged,
}: {
  courseId: string
  lessons: LessonResponse[]
  canManage: boolean
  onChanged: () => void
}) {
  const router = useRouter()
  const [localLessons, setLocalLessons] = useState<LessonResponse[]>(lessons)
  const [reordering, setReordering] = useState(false)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }))

  if (localLessons !== lessons && !reordering) {
    setLocalLessons(lessons)
  }

  const sorted = [...localLessons].sort((a, b) => a.order - b.order)

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event
    if (!over || active.id === over.id) return
    const oldIndex = sorted.findIndex(l => l.id === active.id)
    const newIndex = sorted.findIndex(l => l.id === over.id)
    if (oldIndex === -1 || newIndex === -1) return
    const next = arrayMove(sorted, oldIndex, newIndex).map((l, i) => ({ ...l, order: i + 1 }))
    setLocalLessons(next)
    setReordering(true)
    try {
      await reorderLessons(courseId, next.map(l => l.id))
      toast.success("Порядок занятий сохранён")
      onChanged()
    } catch {
      toast.error("Не удалось сохранить порядок")
      setLocalLessons(lessons)
    } finally {
      setReordering(false)
    }
  }

  const handleToggleCurrent = async (id: string, value: boolean) => {
    setLocalLessons(prev =>
      prev.map(l => (l.id === id ? { ...l, isCurrent: value } : { ...l, isCurrent: value ? false : l.isCurrent })),
    )
    try {
      await setCurrentLesson(courseId, id, value)
      toast.success(value ? "Занятие отмечено как текущее" : "Текущее занятие снято")
      onChanged()
    } catch {
      toast.error("Не удалось обновить текущее занятие")
      setLocalLessons(lessons)
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setDeleting(true)
    try {
      await api.delete(`/api/courses/${courseId}/lessons/${deleteId}`)
      toast.success("Занятие удалено")
      setDeleteId(null)
      onChanged()
    } catch {
      toast.error("Ошибка удаления занятия")
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="flex flex-col gap-3">
      {sorted.length === 0 ? (
        <p className="text-muted-foreground">Нет занятий</p>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={sorted.map(l => l.id)} strategy={verticalListSortingStrategy}>
            <div className="rounded-lg border bg-card divide-y">
              {sorted.map(l => (
                <SortableRow
                  key={l.id}
                  lesson={l}
                  canManage={canManage}
                  onOpen={id => router.push(`/courses/${courseId}/lessons/${id}`)}
                  onToggleCurrent={handleToggleCurrent}
                  onEdit={id => router.push(`/courses/${courseId}/lessons/${id}/edit`)}
                  onAskDelete={setDeleteId}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      )}

      <AlertDialog open={deleteId !== null} onOpenChange={o => !o && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить занятие?</AlertDialogTitle>
            <AlertDialogDescription>Действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={deleting} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              {deleting ? "Удаление..." : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
