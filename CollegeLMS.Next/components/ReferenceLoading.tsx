interface ReferenceLoadingProps {
  title: string
}

/** Скелетон загрузки для страниц справочников (шапка + таблица). */
export default function ReferenceLoading({ title }: ReferenceLoadingProps) {
  return (
    <div
      className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8"
      aria-busy="true"
      aria-label={`Загрузка: ${title}`}
    >
      <div className="flex flex-col gap-2">
        <div className="h-7 w-56 animate-pulse rounded-md bg-muted" />
        <div className="h-4 w-80 max-w-full animate-pulse rounded bg-muted" />
      </div>
      <div className="rounded-xl border bg-card p-6">
        <div className="flex flex-col gap-3">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-10 w-full animate-pulse rounded bg-muted" />
          ))}
        </div>
      </div>
    </div>
  )
}
