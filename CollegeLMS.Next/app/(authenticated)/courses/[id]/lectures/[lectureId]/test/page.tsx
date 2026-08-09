"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type {
  Result,
  LectureResponse,
  StartTestResponse,
  TestQuestionDto,
  TestResultResponse,
} from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import { toast } from "sonner"

const questionTypeLabels: Record<string, string> = {
  SingleChoice: "Один вариант",
  MultipleChoice: "Несколько вариантов",
}

export default function LectureTestPage() {
  const { user } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string
  const lectureId = params.lectureId as string

  const [lecture, setLecture] = useState<LectureResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [test, setTest] = useState<StartTestResponse | null>(null)
  const [starting, setStarting] = useState(false)
  const [answers, setAnswers] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<TestResultResponse | null>(null)
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null)

  useEffect(() => {
    if (user?.role !== "Student") {
      router.replace(`/courses/${courseId}/lectures/${lectureId}`)
    }
  }, [user, courseId, lectureId, router])

  const fetchLecture = useCallback(async () => {
    try {
      const res = await api.get<Result<LectureResponse>>(`/api/courses/${courseId}/lectures/${lectureId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setLecture(body.data)
        if (!body.data.testId) {
          setError("У этой лекции нет теста")
          setLoading(false)
        }
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
        setLoading(false)
      }
    } catch {
      setError("Ошибка загрузки лекции")
      setLoading(false)
    }
  }, [courseId, lectureId])

  useEffect(() => {
    fetchLecture()
  }, [fetchLecture])

  const handleStart = async () => {
    if (!lecture?.testId) return
    setStarting(true)
    setError(null)
    try {
      const res = await api.get<Result<StartTestResponse>>(`/api/tests/${lecture.testId}/start`)
      if (res.data.isSuccess && res.data.data) {
        setTest(res.data.data)
        setSecondsLeft(res.data.data.timeLimitMinutes * 60)
      } else {
        setError(res.data.errorMessage ?? "Не удалось начать тест")
      }
    } catch {
      setError("Не удалось начать тест")
    } finally {
      setStarting(false)
    }
  }

  const toggleOption = (questionId: string, option: string, single: boolean) => {
    setAnswers(prev => {
      const current = prev[questionId] ?? []
      if (single) return { ...prev, [questionId]: [option] }
      return {
        ...prev,
        [questionId]: current.includes(option)
          ? current.filter(o => o !== option)
          : [...current, option],
      }
    })
  }

  const doSubmit = useCallback(async () => {
    if (!test || !lecture?.testId) return
    setSubmitting(true)
    try {
      const body = {
        answers: Object.entries(answers).map(([questionId, options]) => ({
          questionId,
          givenAnswer: options.join("\n"),
        })),
      }
      const subRes = await api.post(`/api/tests/${lecture.testId}/attempt/${test.attemptId}/submit`, body)
      if (!subRes.data.isSuccess) {
        toast.error(subRes.data.errorMessage ?? "Ошибка отправки")
        setSubmitting(false)
        return
      }
      const res = await api.get<Result<TestResultResponse>>(`/api/tests/${lecture.testId}/results`)
      if (res.data.isSuccess && res.data.data) {
        setResult(res.data.data)
        setTest(null)
        setSecondsLeft(null)
      } else {
        toast.error("Не удалось получить результат")
        setSubmitting(false)
      }
    } catch {
      toast.error("Ошибка отправки ответов")
      setSubmitting(false)
    }
  }, [test, lecture?.testId, answers])

  useEffect(() => {
    if (secondsLeft === null) return
    if (secondsLeft <= 0) {
      doSubmit()
      return
    }
    const timer = setTimeout(() => setSecondsLeft(s => (s ?? 0) - 1), 1000)
    return () => clearTimeout(timer)
  }, [secondsLeft, doSubmit])

  if (loading) return <LoadingSpinner size="lg" className="py-20" />

  if (error && !test && !result) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-3xl mx-auto">
        <ErrorBanner message={error} />
        <Button variant="ghost" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
      </div>
    )
  }

  if (result) {
    return (
      <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
        <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
        <div className="rounded-lg border bg-card p-6 flex flex-col items-center gap-3">
          <Badge variant={result.passed ? "default" : "destructive"} className="text-base px-4 py-1">
            {result.passed ? "Пройден" : "Не пройден"}
          </Badge>
          <p className="text-3xl font-bold">{result.percentage}%</p>
          <p className="text-sm text-muted-foreground">
            {result.score} из {result.maxScore} баллов
          </p>
          <p className="text-xs text-muted-foreground">
            Завершён: {new Date(result.completedAt).toLocaleString("ru-RU")}
          </p>
        </div>
        <div className="flex flex-col gap-3">
          {result.answerReviews.map(r => (
            <div key={r.questionId} className="rounded-lg border bg-card p-4 flex flex-col gap-2">
              <div className="flex items-start justify-between gap-3">
                <p className="font-medium">{r.questionText}</p>
                <Badge variant={r.isCorrect ? "default" : "destructive"}>
                  {r.isCorrect ? `+${r.points}` : "0"}
                </Badge>
              </div>
              <p className="text-sm">
                Ваш ответ: <span className={r.isCorrect ? "text-emerald-600" : "text-destructive"}>{r.givenAnswer || "—"}</span>
              </p>
              {!r.isCorrect && r.correctAnswer && (
                <p className="text-sm text-muted-foreground">Правильный ответ: {r.correctAnswer}</p>
              )}
            </div>
          ))}
        </div>
      </div>
    )
  }

  if (!test) {
    return (
      <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
        <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
        <div className="rounded-lg border bg-card p-8 flex flex-col items-center gap-4">
          <h2 className="text-xl font-semibold">Тест по лекции «{lecture?.title ?? ""}»</h2>
          <p className="text-sm text-muted-foreground">
            Отвечайте на вопросы по материалу лекции. После отправки вы увидите результат.
          </p>
          {error && <ErrorBanner message={error} />}
          <Button onClick={handleStart} disabled={starting}>
            {starting ? "Начинаем..." : "Начать тест"}
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => router.push(`/courses/${courseId}/lectures/${lectureId}`)}>
          &larr; Назад к лекции
        </Button>
        {secondsLeft !== null && (
          <Badge variant="outline" className="tabular-nums">
            {Math.floor(secondsLeft / 60)}:{String(secondsLeft % 60).padStart(2, "0")}
          </Badge>
        )}
      </div>

      <div className="flex flex-col gap-4">
        {test.questions.map(q => (
          <div key={q.id} className="rounded-lg border bg-card p-4 flex flex-col gap-3">
            <div className="flex items-start justify-between gap-3">
              <p className="font-medium">{q.text}</p>
              <Badge variant="outline">{questionTypeLabels[q.type] ?? q.type}</Badge>
            </div>
            <div className="flex flex-col gap-2">
              {q.options.split("\n").filter(o => o.trim()).map(option => {
                const single = q.type === "SingleChoice"
                const checked = (answers[q.id] ?? []).includes(option)
                return (
                  <label
                    key={option}
                    className={`flex items-start gap-3 rounded-md border p-3 cursor-pointer transition-colors ${
                      checked ? "border-primary bg-primary/5" : "hover:bg-muted/50"
                    }`}
                  >
                    <input
                      type={single ? "radio" : "checkbox"}
                      name={single ? q.id : undefined}
                      checked={checked}
                      onChange={() => toggleOption(q.id, option, single)}
                      className="mt-1 accent-primary"
                    />
                    <span className="text-sm">{option}</span>
                  </label>
                )
              })}
            </div>
          </div>
        ))}
      </div>

      <Button onClick={doSubmit} disabled={submitting} className="self-end">
        {submitting ? "Отправка..." : "Завершить тест"}
      </Button>
    </div>
  )
}
