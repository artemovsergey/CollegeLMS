"use client"

import { PageTitle } from "@/components/ui/heading"
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
      <PageTitle>Что-то пошло не так</PageTitle>
      <p className="text-muted-foreground">{error.message}</p>
      <Button onClick={reset}>Попробовать снова</Button>
    </div>
  )
}
