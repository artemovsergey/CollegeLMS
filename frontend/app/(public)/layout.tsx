import type { ReactNode } from "react"
import Link from "next/link"

export default function PublicLayout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-50 border-b border-border bg-white">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <Link href="/" className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-md bg-[#568cd6]">
              <span className="text-xs font-bold text-white">СКС</span>
            </div>
            <div className="hidden flex-col sm:flex">
              <span className="text-sm font-semibold leading-tight text-[#152851]">
                Ставропольский колледж связи
              </span>
              <span className="text-xs text-[#5a6a8a]">
                им. Героя Советского Союза В.А. Петрова
              </span>
            </div>
          </Link>
          <nav className="flex items-center gap-6">
            <Link
              href="/"
              className="text-sm font-medium text-[#5a6a8a] transition-colors hover:text-[#568cd6]"
            >
              Главная
            </Link>
            <Link
              href="/news"
              className="text-sm font-medium text-[#5a6a8a] transition-colors hover:text-[#568cd6]"
            >
              Новости
            </Link>
            <Link
              href="/login"
              className="rounded-md bg-[#568cd6] px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-[#3b6ea8]"
            >
              Войти
            </Link>
          </nav>
        </div>
      </header>
      <main className="flex-1">{children}</main>
      <footer className="border-t border-border bg-white">
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
            <div>
              <h3 className="mb-3 text-sm font-semibold text-[#152851]">Контакты</h3>
              <ul className="space-y-2 text-sm text-[#5a6a8a]">
                <li>г. Ставрополь, ул. Петрова, 123</li>
                <li>+7 (8652) 00-00-00</li>
                <li>info@stvcc.ru</li>
              </ul>
            </div>
            <div>
              <h3 className="mb-3 text-sm font-semibold text-[#152851]">Разделы</h3>
              <ul className="space-y-2 text-sm text-[#5a6a8a]">
                <li>
                  <Link href="/" className="hover:text-[#568cd6]">
                    Главная
                  </Link>
                </li>
                <li>
                  <Link href="/news" className="hover:text-[#568cd6]">
                    Новости
                  </Link>
                </li>
                <li>
                  <Link href="/login" className="hover:text-[#568cd6]">
                    Вход в систему
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h3 className="mb-3 text-sm font-semibold text-[#152851]">О колледже</h3>
              <p className="text-sm leading-relaxed text-[#5a6a8a]">
                Государственное бюджетное профессиональное образовательное учреждение
                «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»
              </p>
            </div>
          </div>
          <div className="mt-8 border-t border-border pt-6 text-center text-xs text-[#5a6a8a]">
            &copy; {new Date().getFullYear()} ГБПОУ СКСС. Все права защищены.
          </div>
        </div>
      </footer>
    </div>
  )
}
