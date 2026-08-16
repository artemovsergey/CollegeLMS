"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseResponse, LectureResponse, AssignmentResponse, MaterialResponse, CourseGroupResponse, GroupResponse, MyTestResultDto } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { LECTURE_TYPE_LABELS, LECTURE_TYPE_VARIANTS } from "@/lib/lectureTypes"
import { TEST_STATUS_LABELS, TEST_STATUS_VARIANTS, testStatusFor } from "@/lib/testStatus"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { roleLabels, roleVariants } from "@/lib/constants"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import { Label } from "@/components/ui/label"
import { toast } from "sonner"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
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
import { XIcon } from "lucide-react"

const statusLabels: Record<string, string> = {
  Active: "Активен",
  Completed: "Завершён",
  Draft: "Черновик",
}

const statusVariants: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  Active: "default",
  Completed: "secondary",
  Draft: "outline",
}

type Tab = "lessons" | "materials" | "groups"

export default function CourseDetailPage() {
  const { user } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [course, setCourse] = useState<CourseResponse | null>(null)
  const [lectures, setLectures] = useState<LectureResponse[]>([])
  const [assignments, setAssignments] = useState<AssignmentResponse[]>([])
  const [materials, setMaterials] = useState<MaterialResponse[]>([])
  const [courseGroups, setCourseGroups] = useState<CourseGroupResponse[]>([])
  const [availableGroups, setAvailableGroups] = useState<GroupResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>("lessons")

  const [showAddGroup, setShowAddGroup] = useState(false)
  const [addGroupSelectedId, setAddGroupSelectedId] = useState("")
  const [addGroupSubmitting, setAddGroupSubmitting] = useState(false)
  const [addGroupError, setAddGroupError] = useState<string | null>(null)
  const [myTestResults, setMyTestResults] = useState<Map<string, MyTestResultDto>>(new Map())

  const [removeGroupId, setRemoveGroupId] = useState<string | null>(null)
  const [removeSubmitting, setRemoveSubmitting] = useState(false)

  const isTeacher = user?.role === "Teacher" && course?.teacherId === user?.teacherId
  const isAdmin = user?.role === "Admin"
  const canEdit = isTeacher || isAdmin

  const fetchCourse = useCallback(async () => {
    try {
      const res = await api.get<Result<CourseResponse>>(`/api/courses/${courseId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setCourse(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки курса")
    }
  }, [courseId])

  const fetchLectures = useCallback(async () => {
    try {
      const res = await api.get<Result<LectureResponse[]>>(`/api/courses/${courseId}/lectures`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setLectures(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [courseId])

  const fetchAssignments = useCallback(async () => {
    try {
      const res = await api.get<Result<AssignmentResponse[]>>(`/api/courses/${courseId}/assignments`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setAssignments(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [courseId])

  const fetchMaterials = useCallback(async () => {
    try {
      const res = await api.get<Result<MaterialResponse[]>>(`/api/courses/${courseId}/materials`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setMaterials(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [courseId])

  const fetchCourseGroups = useCallback(async () => {
    try {
      const res = await api.get<Result<CourseGroupResponse[]>>(`/api/courses/${courseId}/groups`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setCourseGroups(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [courseId])

  const fetchAvailableGroups = useCallback(async () => {
    try {
      const res = await api.get<Result<GroupResponse[]>>("/api/groups")
      const body = res.data
      if (body.isSuccess && body.data) {
        setAvailableGroups(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [])

  useEffect(() => {
    setLoading(true)
    Promise.all([fetchCourse(), fetchLectures(), fetchAssignments(), fetchMaterials(), fetchCourseGroups(), fetchAvailableGroups()])
      .finally(() => setLoading(false))
  }, [fetchCourse, fetchLectures, fetchAssignments, fetchMaterials, fetchCourseGroups, fetchAvailableGroups])

  useEffect(() => {
    if (user?.role !== "Student") return
    api.get<Result<MyTestResultDto[]>>("/api/my/test-results").then(res => {
      if (res.data.isSuccess && res.data.data) {
        setMyTestResults(new Map(res.data.data.map(r => [r.testId, r])))
      }
    }).catch(() => {
      // ignore
    })
  }, [user?.role])

  const handleAddGroup = async (e: React.FormEvent) => {
    e.preventDefault()
    setAddGroupError(null)
    setAddGroupSubmitting(true)
    try {
      const res = await api.post<Result<void>>(`/api/courses/${courseId}/groups`, {
        groupIds: [addGroupSelectedId],
      })
      if (res.data.isSuccess) {
        toast.success("Группа добавлена")
        setShowAddGroup(false)
        setAddGroupSelectedId("")
        await Promise.all([fetchCourseGroups(), fetchAvailableGroups()])
      } else {
        setAddGroupError(res.data.errorMessage ?? "Ошибка добавления")
      }
    } catch {
      setAddGroupError("Ошибка добавления группы")
    } finally {
      setAddGroupSubmitting(false)
    }
  }

  const handleRemoveGroup = async () => {
    if (!removeGroupId) return
    setRemoveSubmitting(true)
    try {
      const res = await api.delete(`/api/courses/${courseId}/groups/${removeGroupId}`)
      if (res.data.isSuccess ?? true) {
        toast.success("Группа удалена")
        setRemoveGroupId(null)
        await Promise.all([fetchCourseGroups(), fetchAvailableGroups()])
      } else {
        toast.error(res.data.errorMessage ?? "Ошибка удаления")
      }
    } catch {
      toast.error("Ошибка удаления группы")
    } finally {
      setRemoveSubmitting(false)
    }
  }

  if (loading) return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <LoadingSpinner size="lg" />
    </div>
  )
  if (error) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-5xl mx-auto">
        <ErrorBanner message={error} />
        <Button variant="ghost" onClick={() => router.push("/courses")}>Назад к курсам</Button>
      </div>
    )
  }
  if (!course) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">

      <div className="flex items-start justify-between">
        <div className="flex flex-col gap-1">
          <h2 className="text-xl font-semibold">{course.title}</h2>
          <p className="text-sm text-muted-foreground">
            {course.teacherName} &middot; {course.groupNames}
          </p>
          {course.description && (
            <p className="text-sm text-muted-foreground mt-1">{course.description}</p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={statusVariants[course.status] ?? "outline"}>
            {statusLabels[course.status] ?? course.status}
          </Badge>
          {canEdit && (
            <Button variant="outline" size="sm" onClick={() => router.push(`/courses/${courseId}/edit`)}>
              Ред.
            </Button>
          )}
        </div>
      </div>

      <div className="flex gap-4 border-b">
        {(["lessons", "materials", "groups"] as Tab[]).map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`pb-2 text-sm font-medium border-b-2 transition-colors ${
              tab === t
                ? "border-primary text-primary"
                : "border-transparent text-muted-foreground hover:text-foreground"
            }`}
          >
            {t === "lessons" ? "Занятия" : t === "materials" ? "Материалы" : "Группы"}
          </button>
        ))}
      </div>

      {tab === "lessons" && (
        <div className="flex flex-col gap-3">
          {canEdit && (
            <div className="flex justify-end gap-2">
              <Button size="sm" onClick={() => router.push(`/courses/${courseId}/lectures/new`)}>
                + Лекция
              </Button>
              <Button size="sm" variant="outline" onClick={() => router.push(`/courses/${courseId}/assignments/new`)}>
                + Задание
              </Button>
            </div>
          )}
          {lectures.length === 0 && assignments.length === 0 ? (
            <p className="text-muted-foreground">Нет занятий</p>
          ) : (
            <div className="rounded-lg border bg-card divide-y">
              {lectures.map(l => (
                <div
                  key={l.id}
                  className="flex items-center gap-2 p-4 cursor-pointer hover:bg-muted/50"
                  onClick={() => router.push(`/courses/${courseId}/lectures/${l.id}`)}
                >
                  <span className="text-sm text-muted-foreground w-6 shrink-0">{l.order}</span>
                  <span className="font-medium min-w-0 flex-1">{l.title}</span>
                  <Badge className="shrink-0" variant={LECTURE_TYPE_VARIANTS[l.lectureType] ?? "outline"}>
                    {LECTURE_TYPE_LABELS[l.lectureType] ?? l.lectureType}
                  </Badge>
                  {l.testId && (
                    <Badge
                      className="shrink-0"
                      variant={
                        user?.role === "Student"
                          ? TEST_STATUS_VARIANTS[testStatusFor(l.testId, myTestResults)]
                          : "outline"
                      }
                    >
                      {user?.role === "Student"
                        ? TEST_STATUS_LABELS[testStatusFor(l.testId, myTestResults)]
                        : "Тест"}
                    </Badge>
                  )}
                </div>
              ))}
              {assignments.map(a => (
                <div
                  key={a.id}
                  className="flex items-center gap-2 p-4 cursor-pointer hover:bg-muted/50"
                  onClick={() => router.push(`/courses/${courseId}/assignments/${a.id}`)}
                >
                  <span className="text-sm text-muted-foreground w-6 shrink-0">{a.order}</span>
                  <span className="font-medium min-w-0 flex-1">{a.title}</span>
                  <Badge className="shrink-0" variant="secondary">Задание</Badge>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {tab === "materials" && (
        <div className="flex flex-col gap-3">
          {materials.length === 0 ? (
            <p className="text-muted-foreground">Нет материалов</p>
          ) : (
            <div className="rounded-lg border bg-card divide-y">
              {materials.map(m => (
                <div key={m.id} className="flex items-center justify-between p-4">
                  <div className="flex flex-col gap-1">
                    <span className="font-medium">{m.fileName}</span>
                    <span className="text-xs text-muted-foreground">
                      {(m.fileSize / 1024).toFixed(1)} KB
                    </span>
                  </div>
                  <Button variant="outline" size="sm" asChild>
                    <a href={`/api/courses/${courseId}/materials/${m.id}/download`} download>
                      Скачать
                    </a>
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {tab === "groups" && (
        <div className="flex flex-col gap-3">
          {canEdit && (
            <div className="flex justify-end">
              <Dialog open={showAddGroup} onOpenChange={setShowAddGroup}>
                <DialogTrigger asChild>
                  <Button size="sm">+ Добавить группу</Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Добавить группу</DialogTitle>
                  </DialogHeader>
                  <form onSubmit={handleAddGroup} className="flex flex-col gap-4">
                    {addGroupError && <ErrorBanner message={addGroupError} />}
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="add-group-select">Группа</Label>
                      <Select value={addGroupSelectedId} onValueChange={setAddGroupSelectedId} required>
                        <SelectTrigger id="add-group-select"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {availableGroups.filter(g => !courseGroups.some(cg => cg.groupId === g.id)).map(g => (
                            <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="flex gap-2 justify-end pt-2">
                      <Button type="button" variant="ghost" onClick={() => { setShowAddGroup(false); setAddGroupSelectedId(""); setAddGroupError(null) }}>Отмена</Button>
                      <Button type="submit" disabled={addGroupSubmitting || !addGroupSelectedId}>
                        {addGroupSubmitting ? "Добавление..." : "Добавить"}
                      </Button>
                    </div>
                  </form>
                </DialogContent>
              </Dialog>
            </div>
          )}
          {courseGroups.length === 0 ? (
            <p className="text-muted-foreground">Группы не назначены</p>
          ) : (
            <div className="rounded-lg border bg-card">
              <div className="divide-y">
                {courseGroups.map(cg => (
                  <div key={cg.groupId} className="flex items-center justify-between p-4">
                    <span className="font-medium">{cg.groupName}</span>
                    {canEdit && (
                      <Button variant="ghost" size="sm" onClick={() => setRemoveGroupId(cg.groupId)} className="text-muted-foreground hover:text-fg">
                        <XIcon className="size-4" />
                      </Button>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      <AlertDialog open={!!removeGroupId} onOpenChange={(o) => !o && setRemoveGroupId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить группу?</AlertDialogTitle>
            <AlertDialogDescription>Группа будет откреплена от курса.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleRemoveGroup} disabled={removeSubmitting} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              {removeSubmitting ? "Удаление..." : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
