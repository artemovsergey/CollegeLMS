"use client"

import { useEffect, useState } from "react"
import type { Result, CourseResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { SkeletonCardGrid } from "@/components/SkeletonCardGrid"
import ErrorBanner from "@/components/ErrorBanner"
import Link from "next/link"
import { BookOpen } from "lucide-react"

export default function LmsHomePage() {
  const { token, user } = useAuth()
  const [courses, setCourses] = useState<CourseResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token) return
    setLoading(true)
    api
      .get<Result<CourseResponse[]>>("/api/courses")
      .then((data) => {
        const courses = data.data ?? data
        setCourses(Array.isArray(courses) ? courses : [])
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [token])

  if (loading) return <SkeletonCardGrid count={6} />

  if (error) return <ErrorBanner message={error} />

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold">Добро пожаловать в LMS</h1>
        <p className="mt-2 text-muted-foreground">
          {user?.role === "Admin"
            ? "Управляйте курсами, расписанием и пользователями"
            : user?.role === "Teacher"
              ? "Управляйте своими курсами и студентами"
              : "Просматривайте курсы и отслеживайте успеваемость"}
        </p>
      </div>

      {courses.length === 0 ? (
        <div className="flex flex-col items-center gap-4 py-16">
          <BookOpen className="h-16 w-16 text-muted-foreground" />
          <p className="text-lg text-muted-foreground">Нет доступных курсов</p>
        </div>
      ) : (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {courses.map((course) => (
            <Link key={course.id} href={`/courses/${course.id}`} className="group">
              <Card className="h-full transition-shadow hover:shadow-lg">
                <CardHeader>
                  <CardTitle className="text-lg">{course.title}</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground line-clamp-2">{course.description}</p>
                </CardContent>
                <CardFooter>
                  <Button variant="outline" size="sm" className="group-hover:bg-primary group-hover:text-primary-foreground">
                    Перейти
                  </Button>
                </CardFooter>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
