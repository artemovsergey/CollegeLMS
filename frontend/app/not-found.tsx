import Link from "next/link"

export default function NotFound() {
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center px-4 text-center">
      <h1 className="mb-4 text-4xl font-bold text-[#152851]">404</h1>
      <p className="mb-6 text-lg text-[#5a6a8a]">Страница не найдена</p>
      <Link
        href="/"
        className="rounded-md bg-[#568cd6] px-6 py-2 text-sm font-medium text-white transition-colors hover:bg-[#3b6ea8]"
      >
        На главную
      </Link>
    </div>
  )
}
