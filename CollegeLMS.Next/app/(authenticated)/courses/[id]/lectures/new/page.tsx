"use client"

import { useParams } from "next/navigation"
import LectureForm from "@/components/LectureForm"

export default function CreateLecturePage() {
  const params = useParams()
  const courseId = params.id as string

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">
      <h2 className="text-xl font-semibold">Новое занятие</h2>
      <LectureForm courseId={courseId} />
    </div>
  )
}
