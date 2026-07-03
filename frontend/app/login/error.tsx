"use client"

import { Button } from "@/components/ui/button"

export default function ErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen gap-4">
      <h2 className="text-xl font-semibold">Что-то пошло не так</h2>
      <p className="text-muted-foreground">{error.message}</p>
      <Button onClick={reset}>Попробовать снова</Button>
    </div>
  )
}
