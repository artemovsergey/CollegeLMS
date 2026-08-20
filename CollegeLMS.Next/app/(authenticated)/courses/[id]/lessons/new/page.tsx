"use client"

import { useParams } from "next/navigation"
import LessonForm from "@/components/LessonForm"

export default function CreateLessonPage() {
  const params = useParams()
  const courseId = params.id as string

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">
      <h2 className="text-xl font-semibold">Новое занятие</h2>
      <LessonForm courseId={courseId} />
    </div>
  )
}
