"use client"

import { AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"

export default function ChangesError({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <div className="mx-auto flex min-h-[50vh] max-w-7xl flex-col items-center justify-center gap-3 p-6 text-center">
      <AlertCircle className="size-10 text-destructive" aria-hidden />
      <h2 className="text-lg font-semibold">Не удалось открыть раздел изменений</h2>
      <p className="max-w-md text-sm text-muted-foreground">
        {error.message || "Произошла непредвиденная ошибка. Попробуйте ещё раз."}
      </p>
      <Button onClick={reset}>Повторить</Button>
    </div>
  )
}
