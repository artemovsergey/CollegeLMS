"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseResponse, CreateCourseRequest, GroupResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

export default function EditCoursePage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")
  const [groupId, setGroupId] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchData = useCallback(async () => {
    try {
      const [courseRes, groupsRes] = await Promise.all([
        api.get<Result<CourseResponse>>(`/api/courses/${courseId}`),
        api.get<Result<GroupResponse[]>>("/api/groups"),
      ])
      const courseBody = courseRes.data
      const groupsBody = groupsRes.data
      if (courseBody.isSuccess && courseBody.data) {
        setTitle(courseBody.data.title)
        setDescription(courseBody.data.description)
        setGroupId(courseBody.data.groupId)
      } else {
        setError(courseBody.errorMessage ?? "Ошибка загрузки")
      }
      if (groupsBody.isSuccess && groupsBody.data) {
        setGroups(groupsBody.data)
      }
    } catch {
      setError("Ошибка загрузки данных")
    } finally {
      setLoading(false)
    }
  }, [courseId])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      fetchData()
    }
  }, [token, fetchData])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const body: CreateCourseRequest = { title, description, groupId }
      const res = await api.put<Result<CourseResponse>>(`/api/courses/${courseId}`, body)
      if (res.data.isSuccess) {
        router.push(`/courses/${courseId}`)
      } else {
        setError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setError("Ошибка обновления курса")
    } finally {
      setSubmitting(false)
    }
  }

  if (authLoading) return <Loading />
  if (!token) return null
  if (loading) return <Loading />

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto min-h-screen">
      <header className="flex items-center justify-between py-2">
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            CL
          </div>
          <h1 className="text-lg font-semibold">CollegeLMS</h1>
        </div>
        <div className="flex items-center gap-3">
          <span className="hidden sm:block text-sm text-muted-foreground">{user?.email}</span>
          <Button variant="ghost" size="sm" onClick={() => router.push(`/courses/${courseId}`)}>
            Назад
          </Button>
        </div>
      </header>

      <h2 className="text-xl font-semibold">Редактировать курс</h2>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && (
          <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
            {error}
          </div>
        )}
        <div className="flex flex-col gap-2">
          <Label htmlFor="title">Название курса</Label>
          <Input id="title" required value={title} onChange={e => setTitle(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="description">Описание</Label>
          <Textarea id="description" value={description} onChange={e => setDescription(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="group">Группа</Label>
          <Select value={groupId} onValueChange={setGroupId} required>
            <SelectTrigger id="group"><SelectValue /></SelectTrigger>
            <SelectContent>
              {groups.map(g => (
                <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex gap-2 justify-end pt-2">
          <Button type="button" variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>
            Отмена
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? "Сохранение..." : "Сохранить"}
          </Button>
        </div>
      </form>
    </div>
  )
}

function Loading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
    </div>
  )
}
