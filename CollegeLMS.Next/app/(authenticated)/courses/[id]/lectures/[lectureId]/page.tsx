"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import ReactMarkdown from "react-markdown"
import type {
  Result,
  LectureResponse,
  CourseResponse,
  TestResponse,
  CreateTestRequest,
  TestQuestionResponse,
  CreateTestQuestionRequest,
  UpdateTestQuestionRequest,
  TestStatsResponse,
  TestResultResponse,
  TestAttemptResponse,
} from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { parseErrors } from "@/lib/errors"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import { toast } from "sonner"
import { LECTURE_TYPE_LABELS, LECTURE_TYPE_VARIANTS } from "@/lib/lectureTypes"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import FormField from "@/components/FormField"
import EmptyState from "@/components/EmptyState"
import { ClipboardList, BookOpenText, FileQuestion, Plus } from "lucide-react"
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

export default function LectureViewPage() {
  const { user } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string
  const lectureId = params.lectureId as string

  const [lecture, setLecture] = useState<LectureResponse | null>(null)
  const [course, setCourse] = useState<CourseResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleting, setDeleting] = useState(false)

  const [test, setTest] = useState<TestResponse | null>(null)
  const [questions, setQuestions] = useState<TestQuestionResponse[]>([])
  const [showCreateTest, setShowCreateTest] = useState(false)
  const [showQuestions, setShowQuestions] = useState(false)
  const [showCreateQuestion, setShowCreateQuestion] = useState(false)
  const [editingQuestionId, setEditingQuestionId] = useState<string | null>(null)
  const [deleteQuestionId, setDeleteQuestionId] = useState<string | null>(null)
  const [stats, setStats] = useState<TestStatsResponse | null>(null)
  const [showStats, setShowStats] = useState(false)
  const [statsLoading, setStatsLoading] = useState(false)
  const [testLoading, setTestLoading] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [formFieldErrors, setFormFieldErrors] = useState<Record<string, string>>({})
  const [submitting, setSubmitting] = useState(false)

  const [formTestTitle, setFormTestTitle] = useState("")
  const [formTestDescription, setFormTestDescription] = useState("")
  const [formTestTimeLimit, setFormTestTimeLimit] = useState(30)
  const [formTestMaxAttempts, setFormTestMaxAttempts] = useState(1)
  const [formTestPassingScore, setFormTestPassingScore] = useState(60)
  const [formQText, setFormQText] = useState("")
  const [formQType, setFormQType] = useState("SingleChoice")
  const [formQOptions, setFormQOptions] = useState("")
  const [formQCorrect, setFormQCorrect] = useState("")
  const [formQPoints, setFormQPoints] = useState(1)

  const [studentResult, setStudentResult] = useState<TestResultResponse | null>(null)
  const [attemptCount, setAttemptCount] = useState(0)

  const canManage = user?.role === "Admin" || (user?.role === "Teacher" && course?.teacherId === user?.id)
  const isStudent = user?.role === "Student"

  const fetchLecture = useCallback(async () => {
    try {
      const res = await api.get<Result<LectureResponse>>(`/api/courses/${courseId}/lectures/${lectureId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setLecture(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки лекции")
    } finally {
      setLoading(false)
    }
  }, [courseId, lectureId])

  const fetchCourse = useCallback(async () => {
    try {
      const res = await api.get<Result<CourseResponse>>(`/api/courses/${courseId}`)
      const body = res.data
      if (body.isSuccess && body.data) setCourse(body.data)
    } catch {
      // silently ignore
    }
  }, [courseId])

  useEffect(() => {
    Promise.all([fetchLecture(), fetchCourse()])
  }, [fetchLecture, fetchCourse])

  const fetchTest = useCallback(async () => {
    if (!lecture?.testId) return
    try {
      const res = await api.get<Result<TestResponse>>(`/api/tests/${lecture.testId}`)
      if (res.data.isSuccess && res.data.data) setTest(res.data.data)
    } catch {
      // ignore
    }
  }, [lecture?.testId])

  const fetchQuestions = useCallback(async () => {
    if (!test) return
    try {
      const res = await api.get<Result<TestQuestionResponse[]>>(`/api/tests/${test.id}/questions`)
      if (res.data.isSuccess && res.data.data) setQuestions(res.data.data)
    } catch {
      // ignore
    }
  }, [test])

  useEffect(() => {
    if (lecture?.testId) fetchTest()
  }, [lecture?.testId, fetchTest])

  useEffect(() => {
    if (test && canManage) fetchQuestions()
  }, [test, canManage, fetchQuestions])

  const fetchStudentData = useCallback(async () => {
    if (!lecture?.testId || !isStudent) return
    try {
      const [resultRes, attemptsRes] = await Promise.all([
        api.get<Result<TestResultResponse>>(`/api/tests/${lecture.testId}/results`),
        api.get<Result<TestAttemptResponse[]>>(`/api/tests/${lecture.testId}/attempts`),
      ])
      if (resultRes.data.isSuccess && resultRes.data.data) setStudentResult(resultRes.data.data)
      if (attemptsRes.data.isSuccess && attemptsRes.data.data) {
        setAttemptCount(attemptsRes.data.data.length)
      }
    } catch {
      setStudentResult(null)
    }
  }, [lecture?.testId, isStudent])

  useEffect(() => {
    if (lecture?.testId && isStudent) fetchStudentData()
  }, [lecture?.testId, isStudent, fetchStudentData])

  const handleDelete = async () => {
    if (!lecture) return
    setDeleting(true)
    try {
      await api.delete(`/api/courses/${courseId}/lectures/${lecture.id}`)
      toast.success("Занятие удалено")
      router.push(`/courses/${courseId}`)
    } catch {
      toast.error("Ошибка удаления занятия")
      setDeleteOpen(false)
    } finally {
      setDeleting(false)
    }
  }

  const resetTestForm = () => {
    setFormTestTitle("")
    setFormTestDescription("")
    setFormTestTimeLimit(30)
    setFormTestMaxAttempts(1)
    setFormTestPassingScore(60)
    setFormError(null)
    setFormFieldErrors({})
  }

  const handleCreateTest = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!lecture) return
    setSubmitting(true)
    setFormError(null)
    setFormFieldErrors({})
    try {
      const body: CreateTestRequest = {
        title: formTestTitle,
        description: formTestDescription,
        courseId: lecture.courseId,
        maxAttempts: formTestMaxAttempts,
        timeLimitMinutes: formTestTimeLimit,
        passingScore: formTestPassingScore,
        type: "SelfStudy",
        lectureId: lecture.id,
      }
      await api.post<Result<TestResponse>>("/api/tests", body)
      toast.success("Тест создан")
      setShowCreateTest(false)
      await fetchLecture()
      await fetchTest()
      setShowQuestions(true)
    } catch (err) {
      const parsed = parseErrors(err)
      setFormFieldErrors(
        Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0]])),
      )
      if (parsed.message) setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  const resetQuestionForm = () => {
    setFormQText("")
    setFormQType("SingleChoice")
    setFormQOptions("")
    setFormQCorrect("")
    setFormQPoints(1)
    setEditingQuestionId(null)
    setFormError(null)
    setFormFieldErrors({})
  }

  const handleCreateQuestion = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!test) return
    setSubmitting(true)
    setFormError(null)
    setFormFieldErrors({})
    try {
      const body: CreateTestQuestionRequest = {
        text: formQText,
        type: formQType,
        options: formQOptions,
        correctAnswer: formQCorrect,
        points: formQPoints,
        orderIndex: questions.length + 1,
      }
      await api.post<Result<TestQuestionResponse>>(`/api/tests/${test.id}/questions`, body)
      toast.success("Вопрос добавлен")
      setShowCreateQuestion(false)
      resetQuestionForm()
      await fetchQuestions()
      await fetchTest()
    } catch (err) {
      const parsed = parseErrors(err)
      setFormFieldErrors(
        Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0]])),
      )
      if (parsed.message) setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  const handleUpdateQuestion = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!test || !editingQuestionId) return
    setSubmitting(true)
    setFormError(null)
    setFormFieldErrors({})
    try {
      const body: UpdateTestQuestionRequest = {
        text: formQText,
        type: formQType,
        options: formQOptions,
        correctAnswer: formQCorrect,
        points: formQPoints,
        orderIndex: questions.find(q => q.id === editingQuestionId)?.orderIndex ?? questions.length + 1,
      }
      await api.put(`/api/tests/${test.id}/questions/${editingQuestionId}`, body)
      toast.success("Вопрос обновлён")
      setShowCreateQuestion(false)
      resetQuestionForm()
      await fetchQuestions()
    } catch (err) {
      const parsed = parseErrors(err)
      setFormFieldErrors(
        Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0]])),
      )
      if (parsed.message) setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  const handleDeleteQuestion = async (id: string) => {
    if (!test) return
    try {
      await api.delete(`/api/tests/${test.id}/questions/${id}`)
      toast.success("Вопрос удалён")
      setDeleteQuestionId(null)
      await fetchQuestions()
      await fetchTest()
    } catch {
      toast.error("Ошибка удаления вопроса")
    }
  }

  const fillQuestionForm = (q: TestQuestionResponse) => {
    setEditingQuestionId(q.id)
    setFormQText(q.text)
    setFormQType(q.type)
    setFormQOptions(q.options)
    setFormQCorrect(q.correctAnswer)
    setFormQPoints(q.points)
    setFormError(null)
    setFormFieldErrors({})
  }

  const openStats = async () => {
    if (!test) return
    setShowStats(true)
    setStatsLoading(true)
    try {
      const res = await api.get<Result<TestStatsResponse>>(`/api/tests/${test.id}/stats`)
      if (res.data.isSuccess && res.data.data) setStats(res.data.data)
      else setStats(null)
    } catch {
      setStats(null)
    } finally {
      setStatsLoading(false)
    }
  }

  if (loading) return <LoadingSpinner size="lg" className="py-20" />

  if (error) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-3xl mx-auto">
        <ErrorBanner message={error} />
        <Button variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>Назад к курсу</Button>
      </div>
    )
  }
  if (!lecture) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">

      <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/courses/${courseId}`)}>
        &larr; Назад к курсу
      </Button>

      <div className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h2 className="text-xl font-semibold">
            {lecture.order}. {lecture.title}
          </h2>
          <Badge variant={LECTURE_TYPE_VARIANTS[lecture.lectureType] ?? "outline"} className="w-fit">
            {LECTURE_TYPE_LABELS[lecture.lectureType] ?? lecture.lectureType}
          </Badge>
        </div>
        {canManage && (
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={() => router.push(`/courses/${courseId}/lectures/${lecture.id}/edit`)}>
              Редактировать
            </Button>
            <Button variant="outline" size="sm" className="text-muted-foreground hover:text-fg" onClick={() => setDeleteOpen(true)}>
              Удалить
            </Button>
          </div>
        )}
      </div>

      {canManage && lecture.testId && (
        <div className="rounded-lg border bg-card p-6 flex flex-col gap-4">
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <ClipboardList className="h-5 w-5 text-primary" />
              <div className="flex flex-col gap-0.5">
                <span className="font-medium">{test?.title ?? "Тест к лекции"}</span>
                <span className="text-xs text-muted-foreground">
                  Вопросов: {test?.questionCount ?? questions.length} · Проходной балл:{" "}
                  {test?.passingScore ?? "-"}%
                </span>
              </div>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowQuestions(true)}>
                <FileQuestion className="size-4 mr-1" />
                Вопросы
              </Button>
              <Button variant="outline" size="sm" onClick={openStats}>
                <BookOpenText className="size-4 mr-1" />
                Статистика
              </Button>
            </div>
          </div>
          {questions.length === 0 && (
            <p className="text-sm text-muted-foreground">
              Вопросов ещё нет — добавьте хотя бы один, чтобы студенты могли пройти тест.
            </p>
          )}
        </div>
      )}

      {canManage && !lecture.testId && (
        <Dialog open={showCreateTest} onOpenChange={o => { if (o) resetTestForm(); setShowCreateTest(o) }}>
          <DialogTrigger asChild>
            <Button className="self-start">
              <Plus className="size-4 mr-1" />
              Создать тест к лекции
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-lg">
            <DialogHeader>
              <DialogTitle>Создать тест к лекции</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleCreateTest} className="flex flex-col gap-4">
              {formError && <ErrorBanner message={formError} />}
              <FormField id="t-title" label="Название теста" required error={formFieldErrors.title}>
                <Input
                  id="t-title"
                  value={formTestTitle}
                  onChange={e => setFormTestTitle(e.target.value)}
                  placeholder="Тест по лекции 1"
                />
              </FormField>
              <FormField id="t-desc" label="Описание" error={formFieldErrors.description}>
                <Textarea
                  id="t-desc"
                  value={formTestDescription}
                  onChange={e => setFormTestDescription(e.target.value)}
                />
              </FormField>
              <div className="grid grid-cols-3 gap-4">
                <FormField id="t-max" label="Попыток" required error={formFieldErrors.maxAttempts}>
                  <Input
                    id="t-max"
                    type="number"
                    min={1}
                    value={formTestMaxAttempts}
                    onChange={e => setFormTestMaxAttempts(Number(e.target.value))}
                  />
                </FormField>
                <FormField id="t-time" label="Время (мин)" required error={formFieldErrors.timeLimitMinutes}>
                  <Input
                    id="t-time"
                    type="number"
                    min={1}
                    value={formTestTimeLimit}
                    onChange={e => setFormTestTimeLimit(Number(e.target.value))}
                  />
                </FormField>
                <FormField id="t-pass" label="Проходной %" required error={formFieldErrors.passingScore}>
                  <Input
                    id="t-pass"
                    type="number"
                    min={0}
                    max={100}
                    value={formTestPassingScore}
                    onChange={e => setFormTestPassingScore(Number(e.target.value))}
                  />
                </FormField>
              </div>
              <div className="flex gap-2 justify-end pt-2">
                <Button type="button" variant="ghost" onClick={() => setShowCreateTest(false)}>
                  Отмена
                </Button>
                <Button type="submit" disabled={submitting}>
                  {submitting ? "Создание..." : "Создать"}
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      )}

      {isStudent && lecture.testId && (
        <div className="rounded-lg border bg-card p-6 flex items-center justify-between gap-4">
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-3">
              <ClipboardList className="h-5 w-5 text-primary" />
              <span className="font-medium">{test?.title ?? lecture.testTitle ?? "Тест к лекции"}</span>
            </div>
            {studentResult ? (
              <p className="text-sm text-muted-foreground">
                {studentResult.passed ? (
                  <span className="text-emerald-600 font-medium">
                    Пройден: {studentResult.percentage}% ({studentResult.score}/{studentResult.maxScore})
                  </span>
                ) : (
                  <span className="text-orange-600 font-medium">
                    Не пройден: {studentResult.percentage}% ({studentResult.score}/{studentResult.maxScore})
                  </span>
                )}
                {" "}· {new Date(studentResult.completedAt).toLocaleDateString("ru-RU")}
              </p>
            ) : (
              <p className="text-sm text-muted-foreground">
                {test?.questionCount && test.questionCount > 0
                  ? `Вопросов: ${test.questionCount} · Проходной балл: ${test.passingScore}%`
                  : "Тест ещё не содержит вопросов"}
              </p>
            )}
          </div>
          {studentResult ? (
            attemptCount < (test?.maxAttempts ?? 1) ? (
              <Button onClick={() => router.push(`/courses/${courseId}/lectures/${lecture.id}/test`)}>
                Пересдать
              </Button>
            ) : (
              <Badge variant="outline">Попытки исчерпаны</Badge>
            )
          ) : (
            <Button
              disabled={!(test?.questionCount && test.questionCount > 0)}
              onClick={() => router.push(`/courses/${courseId}/lectures/${lecture.id}/test`)}
            >
              Пройти тест
            </Button>
          )}
        </div>
      )}

      <div className="rounded-lg border bg-card p-6">
        <div className="prose max-w-none">
          <ReactMarkdown>{lecture.content}</ReactMarkdown>
        </div>
      </div>

      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
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

      <Dialog open={showQuestions} onOpenChange={setShowQuestions}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Вопросы теста «{test?.title ?? ""}»</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Вопросов: {questions.length}</span>
              <Dialog open={showCreateQuestion} onOpenChange={o => { if (o) resetQuestionForm(); setShowCreateQuestion(o) }}>
                <DialogTrigger asChild>
                  <Button size="sm">
                    <Plus className="size-4 mr-1" />
                    Добавить вопрос
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-lg">
                  <DialogHeader>
                    <DialogTitle>{editingQuestionId ? "Редактировать вопрос" : "Новый вопрос"}</DialogTitle>
                  </DialogHeader>
                  <form
                    onSubmit={editingQuestionId ? handleUpdateQuestion : handleCreateQuestion}
                    className="flex flex-col gap-4"
                  >
                    {formError && <ErrorBanner message={formError} />}
                    <FormField id="q-text" label="Текст вопроса" required error={formFieldErrors.text}>
                      <Textarea id="q-text" value={formQText} onChange={e => setFormQText(e.target.value)} />
                    </FormField>
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="q-type">Тип</Label>
                      <Select value={formQType} onValueChange={setFormQType}>
                        <SelectTrigger id="q-type">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="SingleChoice">Один вариант</SelectItem>
                          <SelectItem value="MultipleChoice">Несколько вариантов</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <FormField
                      id="q-options"
                      label="Варианты ответов"
                      hint="По одному варианту на строку"
                      required
                      error={formFieldErrors.options}
                    >
                      <Textarea
                        id="q-options"
                        value={formQOptions}
                        onChange={e => setFormQOptions(e.target.value)}
                        placeholder={"Вариант А\nВариант Б\nВариант В"}
                      />
                    </FormField>
                    <FormField
                      id="q-correct"
                      label="Правильный ответ"
                      hint={
                        formQType === "MultipleChoice"
                          ? "Несколько вариантов — по одному на строку, в порядке списка"
                          : "Текст варианта, который считается верным"
                      }
                      required
                      error={formFieldErrors.correctAnswer}
                    >
                      <Textarea
                        id="q-correct"
                        value={formQCorrect}
                        onChange={e => setFormQCorrect(e.target.value)}
                      />
                    </FormField>
                    <FormField id="q-points" label="Баллы" required error={formFieldErrors.points}>
                      <Input
                        id="q-points"
                        type="number"
                        min={1}
                        value={formQPoints}
                        onChange={e => setFormQPoints(Number(e.target.value))}
                      />
                    </FormField>
                    <div className="flex gap-2 justify-end pt-2">
                      <Button type="button" variant="ghost" onClick={() => setShowCreateQuestion(false)}>
                        Отмена
                      </Button>
                      <Button type="submit" disabled={submitting}>
                        {submitting ? "Сохранение..." : "Сохранить"}
                      </Button>
                    </div>
                  </form>
                </DialogContent>
              </Dialog>
            </div>
            {questions.length === 0 ? (
              <EmptyState message="Вопросов пока нет" />
            ) : (
              <div className="rounded-lg border bg-card">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-12">№</TableHead>
                      <TableHead>Текст</TableHead>
                      <TableHead>Тип</TableHead>
                      <TableHead className="w-20">Баллы</TableHead>
                      <TableHead className="w-32">Действия</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {questions.map(q => (
                      <TableRow key={q.id}>
                        <TableCell className="text-muted-foreground">{q.orderIndex}</TableCell>
                        <TableCell className="max-w-md truncate font-medium">{q.text}</TableCell>
                        <TableCell>
                          <Badge variant="outline">
                            {q.type === "MultipleChoice" ? "Несколько вариантов" : "Один вариант"}
                          </Badge>
                        </TableCell>
                        <TableCell>{q.points}</TableCell>
                        <TableCell>
                          <div className="flex gap-1">
                            <Button variant="ghost" size="sm" onClick={() => fillQuestionForm(q)}>
                              Ред.
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-muted-foreground hover:text-fg"
                              onClick={() => setDeleteQuestionId(q.id)}
                            >
                              Удал.
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <AlertDialog open={deleteQuestionId !== null} onOpenChange={o => !o && setDeleteQuestionId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить вопрос?</AlertDialogTitle>
            <AlertDialogDescription>Действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteQuestionId && handleDeleteQuestion(deleteQuestionId)}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Удалить
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <Dialog open={showStats} onOpenChange={setShowStats}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Статистика теста «{test?.title ?? ""}»</DialogTitle>
          </DialogHeader>
          {statsLoading ? (
            <LoadingSpinner size="lg" className="py-10" />
          ) : stats ? (
            <div className="flex flex-col gap-4">
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Попыток</p>
                  <p className="text-lg font-semibold">{stats.totalAttempts}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Пройдено</p>
                  <p className="text-lg font-semibold text-emerald-600">{stats.passedCount}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Не пройдено</p>
                  <p className="text-lg font-semibold text-orange-600">{stats.failedCount}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Средний балл</p>
                  <p className="text-lg font-semibold">{stats.averageScore.toFixed(1)}</p>
                </div>
              </div>
              <div className="rounded-lg border bg-card">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Студент</TableHead>
                      <TableHead>Группа</TableHead>
                      <TableHead>Баллы</TableHead>
                      <TableHead>Статус</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {stats.studentResults.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={4} className="text-center text-muted-foreground">
                          Пока никто не проходил тест
                        </TableCell>
                      </TableRow>
                    ) : (
                      stats.studentResults.map((r, i) => (
                        <TableRow key={i}>
                          <TableCell className="font-medium">{r.studentName}</TableCell>
                          <TableCell>{r.groupName}</TableCell>
                          <TableCell>
                            {r.score} / {r.maxScore}
                          </TableCell>
                          <TableCell>
                            <Badge variant={r.passed ? "default" : "destructive"}>
                              {r.passed ? "Пройден" : "Не пройден"}
                            </Badge>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>
            </div>
          ) : (
            <p className="text-muted-foreground">Статистика недоступна</p>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
