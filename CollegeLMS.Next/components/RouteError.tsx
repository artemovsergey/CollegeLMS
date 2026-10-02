"use client"

import { RouteErrorState } from "@/components/states"

/**
 * Страница ошибки для `error.tsx`. Одна реализация на продукт: раньше их было
 * восемь, и заголовок в них стоял то `<h1>` с `text-3xl`, то `<h2>` с `text-lg`,
 * а подпись под ним оставалась без размера и читалась как основной текст.
 */
export default function RouteError({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <RouteErrorState
      error={error}
      reset={reset}
      title="раздел"
    />
  )
}
