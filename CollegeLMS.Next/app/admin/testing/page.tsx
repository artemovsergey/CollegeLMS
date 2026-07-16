"use client"

import { useEffect, useState, useCallback } from "react"
import type {
  Result,
  TestResponse,
  CreateTestRequest,
  UpdateTestRequest,
  TestQuestionResponse,
  CreateTestQuestionRequest,
  UpdateTestQuestionRequest,
  TestAssignmentResponse,
  CreateTestAssignmentRequest,
  GroupResponse,
  CourseResponse,
} from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
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
import { toast } from "sonner"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import EmptyState from "@/components/EmptyState"
import { ArrowLeft } from "lucide-react"

const typeLabels: Record<string, string> = {
  Control: "Контрольная",
  SelfStudy: "Самостоятельная",
  None: "Не указан",
}

const questionTypeLabels: Record<string, string> = {
  SingleChoice: "Один выбор",
  MultipleChoice: "Множественный выбор",
  Text: "Текст",
}

export default function AdminTestingPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === "Admin"

  const [selectedTest, setSelectedTest] = useState<TestResponse | null>(null)
  const [subTab, setSubTab] = useState<"questions" | "assignments">("questions")

  const [tests, setTests] = useState<TestResponse[]>([])
  const [testsLoading, setTestsLoading] = useState(true)
  const [testsError, setTestsError] = useState<string | null>(null)

  const [showCreateTest, setShowCreateTest] = useState(false)
  const [editingTestId, setEditingTestId] = useState<string | null>(null)
  const [deleteTestId, setDeleteTestId] = useState<string | null>(null)
  const [formTestTitle, setFormTestTitle] = useState("")
  const [formTestDescription, setFormTestDescription] = useState("")
  const [formTestCourseId, setFormTestCourseId] = useState("")
  const [formTestMaxAttempts, setFormTestMaxAttempts] = useState(3)
  const [formTestTimeLimit, setFormTestTimeLimit] = useState(60)
  const [formTestPassingScore, setFormTestPassingScore] = useState(60)
  const [formTestType, setFormTestType] = useState("None")
  const [formTestError, setFormTestError] = useState<string | null>(null)
  const [formTestSubmitting, setFormTestSubmitting] = useState(false)
  const [courses, setCourses] = useState<CourseResponse[]>([])

  const [questions, setQuestions] = useState<TestQuestionResponse[]>([])
  const [questionsLoading, setQuestionsLoading] = useState(false)
  const [questionsError, setQuestionsError] = useState<string | null>(null)

  const [showCreateQuestion, setShowCreateQuestion] = useState(false)
  const [editingQuestionId, setEditingQuestionId] = useState<string | null>(null)
  const [deleteQuestionId, setDeleteQuestionId] = useState<string | null>(null)
  const [formQText, setFormQText] = useState("")
  const [formQType, setFormQType] = useState("SingleChoice")
  const [formQOptions, setFormQOptions] = useState("")
  const [formQCorrect, setFormQCorrect] = useState("")
  const [formQPoints, setFormQPoints] = useState(1)
  const [formQOrder, setFormQOrder] = useState(0)
  const [formQError, setFormQError] = useState<string | null>(null)
  const [formQSubmitting, setFormQSubmitting] = useState(false)

  const [assignments, setAssignments] = useState<TestAssignmentResponse[]>([])
  const [assignmentsLoading, setAssignmentsLoading] = useState(false)
  const [assignmentsError, setAssignmentsError] = useState<string | null>(null)

  const [showCreateAssignment, setShowCreateAssignment] = useState(false)
  const [deleteAssignmentId, setDeleteAssignmentId] = useState<string | null>(null)
  const [formAGroupId, setFormAGroupId] = useState("")
  const [formAOpenDate, setFormAOpenDate] = useState("")
  const [formACloseDate, setFormACloseDate] = useState("")
  const [formAMaxAttempts, setFormAMaxAttempts] = useState(3)
  const [formAError, setFormAError] = useState<string | null>(null)
  const [formASubmitting, setFormASubmitting] = useState(false)
  const [groups, setGroups] = useState<GroupResponse[]>([])

  const fetchTests = useCallback(async () => {
    setTestsLoading(true)
    setTestsError(null)
    try {
      const res = await api.get<Result<TestResponse[]>>("/api/tests")
      const body = res.data
      if (body.isSuccess && body.data) {
        setTests(body.data)
      } else {
        setTestsError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setTestsError("Ошибка загрузки тестов")
    } finally {
      setTestsLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchTests()
  }, [fetchTests])

  useEffect(() => {
    api
      .get<Result<CourseResponse[]>>("/api/courses")
      .then(res => {
        if (res.data.isSuccess && res.data.data) setCourses(res.data.data)
      })
      .catch(() => {})
  }, [])

  useEffect(() => {
    api
      .get<Result<GroupResponse[]>>("/api/groups")
      .then(res => {
        if (res.data.isSuccess && res.data.data) setGroups(res.data.data)
      })
      .catch(() => {})
  }, [])

  const resetTestForm = () => {
    setFormTestTitle("")
    setFormTestDescription("")
    setFormTestCourseId("")
    setFormTestMaxAttempts(3)
    setFormTestTimeLimit(60)
    setFormTestPassingScore(60)
    setFormTestType("None")
    setFormTestError(null)
    setShowCreateTest(false)
    setEditingTestId(null)
  }

  const fillTestForm = (t: TestResponse) => {
    setEditingTestId(t.id)
    setFormTestTitle(t.title)
    setFormTestDescription(t.description)
    setFormTestCourseId(t.courseId)
    setFormTestMaxAttempts(t.maxAttempts)
    setFormTestTimeLimit(t.timeLimitMinutes)
    setFormTestPassingScore(t.passingScore)
    setFormTestType(t.type)
    setFormTestError(null)
  }

  const handleCreateTest = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormTestError(null)
    setFormTestSubmitting(true)
    try {
      const body: CreateTestRequest = {
        title: formTestTitle,
        description: formTestDescription,
        courseId: formTestCourseId,
        maxAttempts: formTestMaxAttempts,
        timeLimitMinutes: formTestTimeLimit,
        passingScore: formTestPassingScore,
        type: formTestType,
      }
      const res = await api.post<Result<TestResponse>>("/api/tests", body)
      if (res.data.isSuccess) {
        resetTestForm()
        await fetchTests()
        toast.success("Тест создан")
      } else {
        setFormTestError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormTestError("Ошибка создания теста")
    } finally {
      setFormTestSubmitting(false)
    }
  }

  const handleUpdateTest = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingTestId) return
    setFormTestError(null)
    setFormTestSubmitting(true)
    try {
      const body: UpdateTestRequest = {
        title: formTestTitle,
        description: formTestDescription,
        maxAttempts: formTestMaxAttempts,
        timeLimitMinutes: formTestTimeLimit,
        passingScore: formTestPassingScore,
        type: formTestType,
      }
      const res = await api.put<Result<TestResponse>>(`/api/tests/${editingTestId}`, body)
      if (res.data.isSuccess) {
        resetTestForm()
        await fetchTests()
        toast.success("Тест обновлён")
      } else {
        setFormTestError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormTestError("Ошибка обновления теста")
    } finally {
      setFormTestSubmitting(false)
    }
  }

  const handleDeleteTest = async () => {
    if (!deleteTestId) return
    try {
      await api.delete(`/api/tests/${deleteTestId}`)
      setDeleteTestId(null)
      await fetchTests()
      if (selectedTest?.id === deleteTestId) setSelectedTest(null)
      toast.success("Тест удалён")
    } catch {
      setTestsError("Ошибка удаления")
    }
  }

  const fetchQuestions = useCallback(async (testId: string) => {
    setQuestionsLoading(true)
    setQuestionsError(null)
    try {
      const res = await api.get<Result<TestQuestionResponse[]>>(`/api/tests/${testId}/questions`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setQuestions(body.data)
      } else {
        setQuestionsError(body.errorMessage ?? "Ошибка загрузки вопросов")
      }
    } catch {
      setQuestionsError("Ошибка загрузки вопросов")
    } finally {
      setQuestionsLoading(false)
    }
  }, [])

  const resetQuestionForm = () => {
    setFormQText("")
    setFormQType("SingleChoice")
    setFormQOptions("")
    setFormQCorrect("")
    setFormQPoints(1)
    setFormQOrder(0)
    setFormQError(null)
    setShowCreateQuestion(false)
    setEditingQuestionId(null)
  }

  const fillQuestionForm = (q: TestQuestionResponse) => {
    setEditingQuestionId(q.id)
    setFormQText(q.text)
    setFormQType(q.type)
    setFormQOptions(q.options)
    setFormQCorrect(q.correctAnswer)
    setFormQPoints(q.points)
    setFormQOrder(q.orderIndex)
    setFormQError(null)
  }

  const handleCreateQuestion = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedTest) return
    setFormQError(null)
    setFormQSubmitting(true)
    try {
      const body: CreateTestQuestionRequest = {
        text: formQText,
        type: formQType,
        options: formQOptions,
        correctAnswer: formQCorrect,
        points: formQPoints,
        orderIndex: formQOrder,
      }
      const res = await api.post<Result<TestQuestionResponse>>(
        `/api/tests/${selectedTest.id}/questions`,
        body
      )
      if (res.data.isSuccess) {
        resetQuestionForm()
        await fetchQuestions(selectedTest.id)
        toast.success("Вопрос создан")
      } else {
        setFormQError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormQError("Ошибка создания вопроса")
    } finally {
      setFormQSubmitting(false)
    }
  }

  const handleUpdateQuestion = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedTest || !editingQuestionId) return
    setFormQError(null)
    setFormQSubmitting(true)
    try {
      const body: UpdateTestQuestionRequest = {
        text: formQText,
        type: formQType,
        options: formQOptions,
        correctAnswer: formQCorrect,
        points: formQPoints,
        orderIndex: formQOrder,
      }
      const res = await api.put<Result<TestQuestionResponse>>(
        `/api/tests/${selectedTest.id}/questions/${editingQuestionId}`,
        body
      )
      if (res.data.isSuccess) {
        resetQuestionForm()
        await fetchQuestions(selectedTest.id)
        toast.success("Вопрос обновлён")
      } else {
        setFormQError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormQError("Ошибка обновления вопроса")
    } finally {
      setFormQSubmitting(false)
    }
  }

  const handleDeleteQuestion = async () => {
    if (!selectedTest || !deleteQuestionId) return
    try {
      await api.delete(`/api/tests/${selectedTest.id}/questions/${deleteQuestionId}`)
      setDeleteQuestionId(null)
      await fetchQuestions(selectedTest.id)
      toast.success("Вопрос удалён")
    } catch {
      setQuestionsError("Ошибка удаления вопроса")
    }
  }

  const fetchAssignments = useCallback(async (testId: string) => {
    setAssignmentsLoading(true)
    setAssignmentsError(null)
    try {
      const res = await api.get<Result<TestAssignmentResponse[]>>(
        `/api/tests/${testId}/assignments`
      )
      const body = res.data
      if (body.isSuccess && body.data) {
        setAssignments(body.data)
      } else {
        setAssignmentsError(body.errorMessage ?? "Ошибка загрузки назначений")
      }
    } catch {
      setAssignmentsError("Ошибка загрузки назначений")
    } finally {
      setAssignmentsLoading(false)
    }
  }, [])

  const resetAssignmentForm = () => {
    setFormAGroupId("")
    setFormAOpenDate("")
    setFormACloseDate("")
    setFormAMaxAttempts(3)
    setFormAError(null)
    setShowCreateAssignment(false)
  }

  const handleCreateAssignment = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedTest) return
    setFormAError(null)
    setFormASubmitting(true)
    try {
      const body: CreateTestAssignmentRequest = {
        groupId: formAGroupId,
        openDate: formAOpenDate,
        closeDate: formACloseDate,
        maxAttempts: formAMaxAttempts,
      }
      const res = await api.post<Result<TestAssignmentResponse>>(
        `/api/tests/${selectedTest.id}/assignments`,
        body
      )
      if (res.data.isSuccess) {
        resetAssignmentForm()
        await fetchAssignments(selectedTest.id)
        toast.success("Назначение создано")
      } else {
        setFormAError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormAError("Ошибка создания назначения")
    } finally {
      setFormASubmitting(false)
    }
  }

  const handleDeleteAssignment = async () => {
    if (!selectedTest || !deleteAssignmentId) return
    try {
      await api.delete(
        `/api/tests/${selectedTest.id}/assignments/${deleteAssignmentId}`
      )
      setDeleteAssignmentId(null)
      await fetchAssignments(selectedTest.id)
      toast.success("Назначение удалено")
    } catch {
      setAssignmentsError("Ошибка удаления назначения")
    }
  }

  const openDetail = (t: TestResponse) => {
    setSelectedTest(t)
    setSubTab("questions")
    fetchQuestions(t.id)
    fetchAssignments(t.id)
  }

  const testFormDialog = (
    <form
      onSubmit={editingTestId ? handleUpdateTest : handleCreateTest}
      className="flex flex-col gap-4"
    >
      {formTestError && <ErrorBanner message={formTestError} />}
      <div className="flex flex-col gap-2">
        <Label htmlFor="test-title">Название</Label>
        <Input
          id="test-title"
          required
          value={formTestTitle}
          onChange={e => setFormTestTitle(e.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="test-description">Описание</Label>
        <Textarea
          id="test-description"
          value={formTestDescription}
          onChange={e => setFormTestDescription(e.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="test-course">Курс</Label>
        {editingTestId ? (
          <Input disabled value={courses.find(c => c.id === formTestCourseId)?.title ?? ""} />
        ) : (
          <Select value={formTestCourseId} onValueChange={setFormTestCourseId}>
            <SelectTrigger id="test-course">
              <SelectValue placeholder="Выберите курс" />
            </SelectTrigger>
            <SelectContent>
              {courses.map(c => (
                <SelectItem key={c.id} value={c.id}>
                  {c.title}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="test-type">Тип</Label>
        <Select value={formTestType} onValueChange={setFormTestType}>
          <SelectTrigger id="test-type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {Object.entries(typeLabels).map(([key, label]) => (
              <SelectItem key={key} value={key}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="grid grid-cols-3 gap-4">
        <div className="flex flex-col gap-2">
          <Label htmlFor="test-max-attempts">Попыток</Label>
          <Input
            id="test-max-attempts"
            type="number"
            min={1}
            required
            value={formTestMaxAttempts}
            onChange={e => setFormTestMaxAttempts(Number(e.target.value))}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="test-time-limit">Время (мин)</Label>
          <Input
            id="test-time-limit"
            type="number"
            min={0}
            required
            value={formTestTimeLimit}
            onChange={e => setFormTestTimeLimit(Number(e.target.value))}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="test-passing-score">Балл прохода</Label>
          <Input
            id="test-passing-score"
            type="number"
            min={0}
            max={100}
            required
            value={formTestPassingScore}
            onChange={e => setFormTestPassingScore(Number(e.target.value))}
          />
        </div>
      </div>
      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={resetTestForm}>
          Отмена
        </Button>
        <Button type="submit" disabled={formTestSubmitting}>
          {formTestSubmitting
            ? "Сохранение..."
            : editingTestId
              ? "Сохранить"
              : "Создать"}
        </Button>
      </div>
    </form>
  )

  const questionFormDialog = (
    <form
      onSubmit={editingQuestionId ? handleUpdateQuestion : handleCreateQuestion}
      className="flex flex-col gap-4"
    >
      {formQError && <ErrorBanner message={formQError} />}
      <div className="flex flex-col gap-2">
        <Label htmlFor="q-text">Текст вопроса</Label>
        <Textarea
          id="q-text"
          required
          value={formQText}
          onChange={e => setFormQText(e.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="q-type">Тип</Label>
        <Select value={formQType} onValueChange={setFormQType}>
          <SelectTrigger id="q-type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {Object.entries(questionTypeLabels).map(([key, label]) => (
              <SelectItem key={key} value={key}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="q-options">Варианты ответов (через точку с запятой)</Label>
        <Textarea
          id="q-options"
          value={formQOptions}
          onChange={e => setFormQOptions(e.target.value)}
          placeholder="Вариант А; Вариант Б; Вариант В"
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="q-correct">Правильный ответ</Label>
        <Textarea
          id="q-correct"
          value={formQCorrect}
          onChange={e => setFormQCorrect(e.target.value)}
        />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-2">
          <Label htmlFor="q-points">Баллы</Label>
          <Input
            id="q-points"
            type="number"
            min={0}
            required
            value={formQPoints}
            onChange={e => setFormQPoints(Number(e.target.value))}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="q-order">Порядок</Label>
          <Input
            id="q-order"
            type="number"
            min={0}
            required
            value={formQOrder}
            onChange={e => setFormQOrder(Number(e.target.value))}
          />
        </div>
      </div>
      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={resetQuestionForm}>
          Отмена
        </Button>
        <Button type="submit" disabled={formQSubmitting}>
          {formQSubmitting
            ? "Сохранение..."
            : editingQuestionId
              ? "Сохранить"
              : "Создать"}
        </Button>
      </div>
    </form>
  )

  const assignmentFormDialog = (
    <form onSubmit={handleCreateAssignment} className="flex flex-col gap-4">
      {formAError && <ErrorBanner message={formAError} />}
      <div className="flex flex-col gap-2">
        <Label htmlFor="a-group">Группа</Label>
        <Select value={formAGroupId} onValueChange={setFormAGroupId}>
          <SelectTrigger id="a-group">
            <SelectValue placeholder="Выберите группу" />
          </SelectTrigger>
          <SelectContent>
            {groups.map(g => (
              <SelectItem key={g.id} value={g.id}>
                {g.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="a-open">Дата открытия</Label>
        <Input
          id="a-open"
          type="datetime-local"
          required
          value={formAOpenDate}
          onChange={e => setFormAOpenDate(e.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="a-close">Дата закрытия</Label>
        <Input
          id="a-close"
          type="datetime-local"
          required
          value={formACloseDate}
          onChange={e => setFormACloseDate(e.target.value)}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="a-max-attempts">Макс. попыток</Label>
        <Input
          id="a-max-attempts"
          type="number"
          min={1}
          required
          value={formAMaxAttempts}
          onChange={e => setFormAMaxAttempts(Number(e.target.value))}
        />
      </div>
      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={resetAssignmentForm}>
          Отмена
        </Button>
        <Button type="submit" disabled={formASubmitting}>
          {formASubmitting ? "Сохранение..." : "Создать"}
        </Button>
      </div>
    </form>
  )

  if (selectedTest) {
    return (
      <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="sm" onClick={() => setSelectedTest(null)}>
              <ArrowLeft className="size-4 mr-1" />
              Назад к списку
            </Button>
            <h2 className="text-xl font-semibold">{selectedTest.title}</h2>
            <Badge variant="outline">{typeLabels[selectedTest.type] ?? selectedTest.type}</Badge>
          </div>
        </div>

        <div className="flex gap-4 border-b">
          <button
            onClick={() => setSubTab("questions")}
            className={`pb-2 text-sm font-medium transition-colors ${
              subTab === "questions"
                ? "border-b-2 border-primary text-foreground"
                : "text-muted-foreground hover:text-foreground"
            }`}
          >
            Вопросы
          </button>
          <button
            onClick={() => setSubTab("assignments")}
            className={`pb-2 text-sm font-medium transition-colors ${
              subTab === "assignments"
                ? "border-b-2 border-primary text-foreground"
                : "text-muted-foreground hover:text-foreground"
            }`}
          >
            Назначения
          </button>
        </div>

        {subTab === "questions" && (
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-medium">Вопросы</h3>
              <Dialog
                open={showCreateQuestion}
                onOpenChange={o => {
                  if (o) resetQuestionForm()
                  setShowCreateQuestion(o)
                }}
              >
                <DialogTrigger asChild>
                  <Button size="sm">+ Добавить вопрос</Button>
                </DialogTrigger>
                <DialogContent className="max-w-lg">
                  <DialogHeader>
                    <DialogTitle>Создать вопрос</DialogTitle>
                  </DialogHeader>
                  {questionFormDialog}
                </DialogContent>
              </Dialog>
            </div>

            {questionsError && <ErrorBanner message={questionsError} />}

            {questionsLoading ? (
              <LoadingSpinner className="py-12" />
            ) : questions.length === 0 ? (
              <EmptyState message="Нет вопросов" />
            ) : (
              <div className="rounded-lg border bg-card">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-12">№</TableHead>
                      <TableHead>Текст</TableHead>
                      <TableHead>Тип</TableHead>
                      <TableHead className="w-20">Баллы</TableHead>
                      {isAdmin && <TableHead className="w-32">Действия</TableHead>}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {questions.map(q => (
                      <TableRow key={q.id}>
                        <TableCell className="text-muted-foreground">{q.orderIndex}</TableCell>
                        <TableCell className="max-w-md truncate font-medium">
                          {q.text}
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline">
                            {questionTypeLabels[q.type] ?? q.type}
                          </Badge>
                        </TableCell>
                        <TableCell>{q.points}</TableCell>
                        {isAdmin && (
                          <TableCell>
                            <div className="flex gap-1">
                              <Dialog>
                                <DialogTrigger asChild>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => fillQuestionForm(q)}
                                  >
                                    Ред.
                                  </Button>
                                </DialogTrigger>
                                <DialogContent className="max-w-lg">
                                  <DialogHeader>
                                    <DialogTitle>Редактировать вопрос</DialogTitle>
                                  </DialogHeader>
                                  {questionFormDialog}
                                </DialogContent>
                              </Dialog>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="text-destructive hover:text-destructive"
                                onClick={() => setDeleteQuestionId(q.id)}
                              >
                                Удал.
                              </Button>
                            </div>
                          </TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </div>
        )}

        {subTab === "assignments" && (
          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-medium">Назначения</h3>
              <Dialog
                open={showCreateAssignment}
                onOpenChange={o => {
                  if (o) resetAssignmentForm()
                  setShowCreateAssignment(o)
                }}
              >
                <DialogTrigger asChild>
                  <Button size="sm">+ Назначить тест</Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Назначить тест группе</DialogTitle>
                  </DialogHeader>
                  {assignmentFormDialog}
                </DialogContent>
              </Dialog>
            </div>

            {assignmentsError && <ErrorBanner message={assignmentsError} />}

            {assignmentsLoading ? (
              <LoadingSpinner className="py-12" />
            ) : assignments.length === 0 ? (
              <EmptyState message="Нет назначений" />
            ) : (
              <div className="rounded-lg border bg-card">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Группа</TableHead>
                      <TableHead>Дата открытия</TableHead>
                      <TableHead>Дата закрытия</TableHead>
                      <TableHead>Макс. попыток</TableHead>
                      {isAdmin && <TableHead className="w-24">Действия</TableHead>}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {assignments.map(a => (
                      <TableRow key={a.id}>
                        <TableCell className="font-medium">{a.groupName}</TableCell>
                        <TableCell>
                          {new Date(a.openDate).toLocaleString("ru-RU")}
                        </TableCell>
                        <TableCell>
                          {new Date(a.closeDate).toLocaleString("ru-RU")}
                        </TableCell>
                        <TableCell>{a.maxAttempts}</TableCell>
                        {isAdmin && (
                          <TableCell>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-destructive hover:text-destructive"
                              onClick={() => setDeleteAssignmentId(a.id)}
                            >
                              Удал.
                            </Button>
                          </TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </div>
        )}

        <AlertDialog
          open={!!deleteQuestionId}
          onOpenChange={() => setDeleteQuestionId(null)}
        >
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Удалить вопрос?</AlertDialogTitle>
              <AlertDialogDescription>
                Это действие нельзя отменить.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Отмена</AlertDialogCancel>
              <AlertDialogAction
                onClick={handleDeleteQuestion}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              >
                Удалить
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>

        <AlertDialog
          open={!!deleteAssignmentId}
          onOpenChange={() => setDeleteAssignmentId(null)}
        >
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Отменить назначение?</AlertDialogTitle>
              <AlertDialogDescription>
                Студенты потеряют доступ к тесту.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Отмена</AlertDialogCancel>
              <AlertDialogAction
                onClick={handleDeleteAssignment}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              >
                Удалить
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Тестирование</h2>
        {isAdmin && (
          <Dialog
            open={showCreateTest}
            onOpenChange={o => {
              if (o) resetTestForm()
              setShowCreateTest(o)
            }}
          >
            <DialogTrigger asChild>
              <Button size="sm">+ Создать тест</Button>
            </DialogTrigger>
            <DialogContent className="max-w-lg">
              <DialogHeader>
                <DialogTitle>Создать тест</DialogTitle>
              </DialogHeader>
              {testFormDialog}
            </DialogContent>
          </Dialog>
        )}
      </div>

      {testsError && <ErrorBanner message={testsError} />}

      {testsLoading ? (
        <LoadingSpinner className="py-16" />
      ) : tests.length === 0 ? (
        <EmptyState message="Нет тестов" />
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Название</TableHead>
                <TableHead>Курс</TableHead>
                <TableHead>Тип</TableHead>
                <TableHead>Попыток</TableHead>
                <TableHead>Балл прохода</TableHead>
                {isAdmin && <TableHead>Действия</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {tests.map(t => (
                <TableRow key={t.id}>
                  <TableCell className="font-medium">{t.title}</TableCell>
                  <TableCell>{t.courseName}</TableCell>
                  <TableCell>
                    <Badge variant="outline">{typeLabels[t.type] ?? t.type}</Badge>
                  </TableCell>
                  <TableCell>{t.maxAttempts}</TableCell>
                  <TableCell>{t.passingScore}</TableCell>
                  {isAdmin && (
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="sm" onClick={() => openDetail(t)}>
                          Вопросы
                        </Button>
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => fillTestForm(t)}
                            >
                              Ред.
                            </Button>
                          </DialogTrigger>
                          <DialogContent className="max-w-lg">
                            <DialogHeader>
                              <DialogTitle>Редактировать тест</DialogTitle>
                            </DialogHeader>
                            {testFormDialog}
                          </DialogContent>
                        </Dialog>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-destructive hover:text-destructive"
                          onClick={() => setDeleteTestId(t.id)}
                        >
                          Удал.
                        </Button>
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <AlertDialog open={!!deleteTestId} onOpenChange={() => setDeleteTestId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить тест?</AlertDialogTitle>
            <AlertDialogDescription>
              Все связанные вопросы и назначения будут удалены.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteTest}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Удалить
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
