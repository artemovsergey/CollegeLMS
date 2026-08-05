"use client"

import { useRouter } from "next/navigation"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"

interface CourseCardProps {
  id: string
  title: string
  subtitle: string
  href: string
  progress?: {
    percent: number
    completed: number
    total: number
  }
}

export default function CourseCard({ id, title, subtitle, href, progress }: CourseCardProps) {
  const router = useRouter()

  const open = () => router.push(href)

  return (
    <Card
      key={id}
      className="cursor-pointer transition-colors hover:bg-muted/50 focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
      onClick={open}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault()
          open()
        }
      }}
      role="link"
      tabIndex={0}
      aria-label={title}
    >
      <CardHeader>
        <CardTitle className="text-base">{title}</CardTitle>
        <CardDescription>{subtitle}</CardDescription>
      </CardHeader>
      {progress && (
        <CardContent>
          <Progress value={progress.percent} className="h-2" />
          <p className="text-xs text-muted-foreground mt-1">
            {progress.completed} из {progress.total} выполнено ({progress.percent}%)
          </p>
        </CardContent>
      )}
    </Card>
  )
}
