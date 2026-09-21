export default function DispatcherLiveLoading() {
  return (
    <div
      className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8"
      aria-busy="true"
      aria-label="Загрузка: Текущие пары"
    >
      <div className="flex flex-col gap-3">
        <div className="h-7 w-56 max-w-full animate-pulse rounded-md bg-muted motion-reduce:animate-none" />
        <div className="h-4 w-80 max-w-full animate-pulse rounded bg-muted motion-reduce:animate-none" />
        <div className="flex flex-wrap gap-2">
          {Array.from({ length: 4 }).map((_, index) => (
            <div
              key={index}
              className="h-11 w-32 animate-pulse rounded-md bg-muted motion-reduce:animate-none sm:h-9"
            />
          ))}
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <div
            key={index}
            className="h-20 animate-pulse rounded-xl border bg-muted motion-reduce:animate-none"
          />
        ))}
      </div>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }).map((_, index) => (
          <div
            key={index}
            className="h-32 animate-pulse rounded-xl border bg-muted motion-reduce:animate-none"
          />
        ))}
      </div>
    </div>
  )
}
