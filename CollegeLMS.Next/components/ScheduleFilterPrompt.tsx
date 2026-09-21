import { CalendarRange } from "lucide-react"
import { cn } from "@/lib/utils"

interface ScheduleFilterPromptProps {
  className?: string
}

export default function ScheduleFilterPrompt({
  className,
}: ScheduleFilterPromptProps) {
  return (
    <div
      role="status"
      className={cn(
        "flex min-h-[40vh] flex-col items-center justify-center gap-3 rounded-lg border bg-card p-10 text-center text-muted-foreground",
        className,
      )}
    >
      <CalendarRange className="size-10 opacity-40" aria-hidden />
      <p>Выберите группу или преподавателя</p>
      <p className="max-w-md text-sm">
        Расписание строится для одной группы или одного преподавателя — выберите
        нужный вариант в фильтрах выше.
      </p>
    </div>
  )
}
