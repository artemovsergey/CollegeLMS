"use client"

import { PageTitle } from "@/components/ui/heading"
import { AlertCircle, RefreshCw } from "lucide-react"
import { Button } from "@/components/ui/button"

export default function ScheduleError({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen gap-4 p-6">
      <AlertCircle className="size-12 text-destructive" />
      <PageTitle>Ошибка загрузки расписания</PageTitle>
      <p className="text-sm text-muted-foreground max-w-md text-center">
        {error.message || "Не удалось загрузить данные расписания. Проверьте подключение к серверу."}
      </p>
      <Button onClick={reset}>
        <RefreshCw className="size-4" />
        Попробовать снова
      </Button>
    </div>
  )
}
