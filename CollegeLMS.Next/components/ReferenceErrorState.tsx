"use client"

import { CircleAlert, RefreshCw } from "lucide-react"
import { Button } from "@/components/ui/button"

interface ReferenceErrorStateProps {
  error: Error & { digest?: string }
  reset: () => void
  title: string
}

/** Ошибка загрузки страницы справочника с кнопкой повтора. */
export default function ReferenceErrorState({
  error,
  reset,
  title,
}: ReferenceErrorStateProps) {
  return (
    <div className="mx-auto flex min-h-[60vh] max-w-lg flex-col items-center justify-center gap-4 px-4 py-12 text-center">
      <CircleAlert className="size-12 text-destructive" aria-hidden="true" />
      <h2 className="text-xl font-semibold">Не удалось загрузить раздел</h2>
      <p className="text-sm text-muted-foreground">
        {error.message ||
          `Произошла ошибка при загрузке раздела «${title}». Проверьте подключение к серверу.`}
      </p>
      <Button onClick={reset}>
        <RefreshCw className="size-4" aria-hidden="true" />
        Повторить
      </Button>
    </div>
  )
}
