import { CalendarDays } from "lucide-react"

export default function ScheduleLoading() {
  return (
    <div className="flex flex-col gap-6 p-6 mx-auto min-h-screen max-w-7xl">
      <header className="flex items-center justify-between py-2">
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 animate-pulse rounded-md bg-muted" />
          <div className="h-5 w-32 animate-pulse rounded bg-muted" />
        </div>
        <div className="flex items-center gap-3">
          <div className="h-4 w-40 animate-pulse rounded bg-muted" />
          <div className="h-5 w-20 animate-pulse rounded bg-muted" />
          <div className="h-8 w-16 animate-pulse rounded bg-muted" />
        </div>
      </header>

      <div className="flex items-center gap-2">
        <CalendarDays className="size-5 text-muted-foreground" />
        <div className="h-7 w-28 animate-pulse rounded bg-muted" />
      </div>

      <div className="h-12 animate-pulse rounded-lg bg-muted" />

      <div className="rounded-lg border bg-card p-4">
        <div className="grid grid-cols-6 gap-2">
          {Array.from({ length: 30 }).map((_, i) => (
            <div
              key={i}
              className="h-20 animate-pulse rounded bg-muted"
              style={{ animationDelay: `${(i % 6) * 100}ms` }}
            />
          ))}
        </div>
      </div>
    </div>
  )
}
