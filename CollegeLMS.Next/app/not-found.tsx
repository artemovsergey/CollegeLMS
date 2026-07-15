import Link from "next/link"

export default function NotFound() {
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center px-4 text-center">
      <h1 className="mb-4 text-4xl font-bold text-primary">404</h1>
      <p className="mb-6 text-lg text-muted-foreground">Страница не найдена</p>
      <Link
        href="/"
        className="rounded-md bg-accent px-6 py-2 text-sm font-medium text-accent-foreground transition-colors hover:bg-accent/90"
      >
        На главную
      </Link>
    </div>
  )
}
