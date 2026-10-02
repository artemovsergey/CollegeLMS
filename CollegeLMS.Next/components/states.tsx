"use client"

import { cn } from "@/lib/utils"
import { AlertCircle, Inbox, RefreshCw } from "lucide-react"
import { Button } from "@/components/ui/button"

/**
 * Состояния страницы. Один набор на продукт.
 *
 * Компоненты существовали, но страницы их почти не использовали: на публичном
 * сайте `LoadingSpinner` и `ErrorBanner` не встречались ни разу, вместо них
 * лежали шесть одинаковых копий спиннера и три копии баннера ошибки с тремя
 * разными отступами. Причина была в самих компонентах — `EmptyState` был
 * голым абзацем без иконки и действия, и в полноценной пустой странице не
 * помещался, поэтому страницы писали своё.
 *
 * Объявление для программ экранного доступа обязательно (§6.1): загрузка
 * объявляется `role="status"` с `aria-live="polite"`, ошибка — `role="alert"`.
 * Раньше ни то, ни другое не было объявлено: `LoadingSpinner` использовался в
 * 24 файлах и был немым.
 */

const spinnerSize = { sm: "size-4 border-2", md: "size-8 border-4", lg: "size-12 border-4" }

interface LoadingSpinnerProps {
  size?: keyof typeof spinnerSize
  /** Что загружается. Читается программам экранного доступа и не виден глазом. */
  label?: string
  className?: string
  /** Отступ по вертикали: центрировать блок на всю доступную высоту. */
  fill?: boolean
}

export function LoadingSpinner({
  size = "md",
  label,
  className,
  fill = false,
}: LoadingSpinnerProps) {
  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        "flex items-center justify-center",
        fill && "min-h-[40vh]",
        className,
      )}
    >
      <span
        className={cn(
          "animate-spin rounded-full border-muted border-t-primary",
          spinnerSize[size],
        )}
      />
      {label && <span className="sr-only">{label}</span>}
    </div>
  )
}

interface EmptyStateProps {
  /** Короткое пояснение, что здесь должно быть. */
  message: string
  /** Действие, если пустое состояние чему-то предшествует. */
  action?: React.ReactNode
  className?: string
  /** Иконка состояния; по умолчанию — «входящие». */
  icon?: React.ReactNode
}

export function EmptyState({
  message,
  action,
  className,
  icon,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed py-12 text-center",
        className,
      )}
    >
      <span className="text-muted-fg" aria-hidden>
        {icon ?? <Inbox className="size-8" />}
      </span>
      <p className="text-sm text-muted-fg">{message}</p>
      {action}
    </div>
  )
}

interface ErrorStateProps {
  message: string
  className?: string
  /** Кнопка повтора или иного выхода из состояния. */
  action?: React.ReactNode
  /** Баннер внутри потока — без рамки и без большого отступа. */
  inline?: boolean
}

export function ErrorState({ message, className, action, inline }: ErrorStateProps) {
  if (inline) {
    return (
      <div
        role="alert"
        className={cn(
          "flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive-text",
          className,
        )}
      >
        <AlertCircle className="size-4 shrink-0" aria-hidden />
        <span className="min-w-0 flex-1">{message}</span>
        {action}
      </div>
    )
  }
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-center justify-center gap-4 rounded-lg border border-destructive/30 bg-destructive/5 py-12 text-center",
        className,
      )}
    >
      <AlertCircle className="size-8 text-destructive-text" aria-hidden />
      <p className="max-w-md text-sm text-destructive-text">{message}</p>
      {action}
    </div>
  )
}

/** Страница целиком не загрузилась. Для `error.tsx`. */
export function RouteErrorState({
  error,
  reset,
  title,
}: {
  error: Error & { digest?: string }
  reset: () => void
  title: string
}) {
  return (
    <div className="mx-auto flex max-w-lg flex-col items-center justify-center gap-4 px-4 py-16 text-center">
      <AlertCircle className="size-12 text-destructive-text" aria-hidden />
      <h2 className="text-xl font-semibold">Не удалось загрузить раздел</h2>
      <p className="text-sm text-muted-fg">
        {error.message ||
          `Произошла ошибка при загрузке раздела «${title}». Проверьте подключение к серверу.`}
      </p>
      <Button onClick={reset}>
        <RefreshCw className="size-4" aria-hidden />
        Повторить
      </Button>
    </div>
  )
}
/** Скелетон страницы справочника: шапка и строки таблицы. Для `loading.tsx`. */
export function RouteLoading({ title }: { title: string }) {
  return (
    <div
      role="status"
      aria-live="polite"
      aria-busy="true"
      aria-label={`Загрузка: ${title}`}
      className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8"
    >
      <div className="flex flex-col gap-2">
        <div className="h-7 w-56 animate-pulse rounded-md bg-muted" />
        <div className="h-4 w-80 max-w-full animate-pulse rounded bg-muted" />
      </div>
      <div className="rounded-lg border bg-card p-6">
        <div className="flex flex-col gap-3">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-10 w-full animate-pulse rounded bg-muted" />
          ))}
        </div>
      </div>
    </div>
  )
}
