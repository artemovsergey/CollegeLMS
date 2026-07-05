import Link from "next/link"
import AccessibilityToggle from "./AccessibilityToggle"

export default function Footer() {
  return (
    <footer className="bg-[#0D1B2A] text-white/70">
      <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
        <div className="grid gap-12 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">О колледже</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/about" className="hover:text-white transition-colors">
                  Основные сведения
                </Link>
              </li>
              <li>
                <Link href="/about/rukovodstvo" className="hover:text-white transition-colors">
                  Руководство
                </Link>
              </li>
              <li>
                <Link href="/about/document" className="hover:text-white transition-colors">
                  Документы
                </Link>
              </li>
              <li>
                <Link href="/about/vacant" className="hover:text-white transition-colors">
                  Вакантные места
                </Link>
              </li>
              <li>
                <Link href="/about/objects" className="hover:text-white transition-colors">
                  Материально-техническая база
                </Link>
              </li>
            </ul>
          </div>

          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">Абитуриенту</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/admissions" className="hover:text-white transition-colors">
                  Приемная комиссия
                </Link>
              </li>
              <li>
                <Link href="/education/perechen-spetsialnostey" className="hover:text-white transition-colors">
                  Специальности
                </Link>
              </li>
              <li>
                <Link href="/admissions/kakie-dokumentyi-neobhodimo-prinesti" className="hover:text-white transition-colors">
                  Документы для приема
                </Link>
              </li>
              <li>
                <Link href="/admissions/den-otkrytyh-dverej-2026" className="hover:text-white transition-colors">
                  Дни открытых дверей
                </Link>
              </li>
            </ul>
          </div>

          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">Студенту</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/student-life/raspisanie-zanyatij-po-ochnoj-forme-obucheniya" className="hover:text-white transition-colors">
                  Расписание занятий
                </Link>
              </li>
              <li>
                <Link href="/student-life/raspisanie-ekzamenov" className="hover:text-white transition-colors">
                  Расписание экзаменов
                </Link>
              </li>
              <li>
                <Link href="/student-life/biblioteka" className="hover:text-white transition-colors">
                  Библиотека
                </Link>
              </li>
              <li>
                <Link href="/student-life/distancionnoeobuch" className="hover:text-white transition-colors">
                  Дистанционное обучение
                </Link>
              </li>
            </ul>
          </div>

          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">Мы в соцсетях</h4>
            <div className="flex gap-3 mb-6">
              <a
                href="https://vk.com/stvcc_official"
                target="_blank"
                rel="noopener noreferrer"
                className="flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white/70 transition-colors hover:bg-[#0077FF] hover:text-white"
                aria-label="ВКонтакте"
              >
                <svg width="20" height="20" viewBox="0 0 48 48" fill="currentColor">
                  <path d="M25.54 34.58c-10.94 0-17.18-7.5-17.44-19.98h5.48c.18 8.56 3.94 12.18 6.92 12.92V14.6h4.82v7.38c2.96-.32 6.06-3.68 7.12-7.38h4.82c-.8 4.58-4.16 7.94-6.56 9.32 2.4 1.12 6.24 4.06 7.72 9.32h-5.32c-1.14-3.56-4-6.18-7.72-6.52v6.52h-.82z"/>
                </svg>
              </a>
              <a
                href="https://t.me/stvcc_official"
                target="_blank"
                rel="noopener noreferrer"
                className="flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white/70 transition-colors hover:bg-[#08C] hover:text-white"
                aria-label="Telegram"
              >
                <svg width="20" height="20" viewBox="0 0 48 48" fill="currentColor">
                  <path d="M9.5 23.5L20.5 28.5L26 40L40 8L9.5 23.5Z"/>
                </svg>
              </a>
              <a
                href="https://web.max.ru/"
                target="_blank"
                rel="noopener noreferrer"
                className="flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white/70 transition-colors hover:bg-[#471AFF] hover:text-white"
                aria-label="Max"
              >
                <svg width="20" height="20" viewBox="0 0 40 40" fill="none">
                  <rect width="40" height="40" rx="12" fill="url(#maxGrad)"/>
                  <rect width="40" height="40" rx="12" fill="url(#maxOverlay)"/>
                  <path fillRule="evenodd" clipRule="evenodd" d="M14.5146 31.4294C14.3778 31.3326 14.1894 31.3589 14.0748 31.4811C12.5428 33.1151 8.62204 34.2613 8.44245 32.0311C8.44245 30.2838 8.04999 28.8114 7.61789 27.1903C7.08868 25.2048 6.5 22.9961 6.5 19.7861C6.5 12.1316 12.7777 6.375 20.2209 6.375C27.6642 6.375 33.5 12.4127 33.5 19.8605C33.5 27.3083 27.4784 33.1973 20.2914 33.1973C17.7416 33.1973 16.5042 32.8382 14.5146 31.4294ZM20.4297 13.0119C16.8974 12.8267 14.1409 15.2777 13.533 19.114C13.0297 22.291 13.9221 26.1621 14.6855 26.357C15.0093 26.4396 15.7885 25.8436 16.3553 25.3091C16.4619 25.2086 16.6231 25.1917 16.748 25.2681C17.6317 25.8084 18.6322 26.2145 19.7352 26.2724C23.3615 26.4625 26.5751 23.6244 26.7651 19.9977C26.955 16.3711 24.0561 13.2021 20.4297 13.0119Z" fill="white"/>
                  <defs>
                    <linearGradient id="maxGrad" x1="4.5" y1="36" x2="49" y2="4.5" gradientUnits="userSpaceOnUse">
                      <stop stopColor="#52C5FE"/>
                      <stop offset="0.389823" stopColor="#3948EC"/>
                      <stop offset="0.946965" stopColor="#9A40DA"/>
                    </linearGradient>
                    <linearGradient id="maxOverlay" x1="3" y1="3" x2="38.5" y2="32.5" gradientUnits="userSpaceOnUse">
                      <stop offset="0.580435" stopColor="#9A40DA" stopOpacity="0"/>
                      <stop offset="0.782473" stopColor="#9A40DA" stopOpacity="0.4"/>
                      <stop offset="1" stopColor="#9A40DA"/>
                    </linearGradient>
                  </defs>
                </svg>
              </a>
            </div>
            <h4 className="mb-3 text-sm font-semibold text-white">Подписаться на новости</h4>
            <form className="flex gap-2">
              <input
                type="email"
                placeholder="Ваш email"
                className="flex-1 rounded-lg bg-white/10 px-3 py-2 text-sm text-white placeholder:text-white/40 border border-white/20 focus:outline-none focus:ring-2 focus:ring-white/30"
              />
              <button
                type="submit"
                className="rounded-lg bg-white px-4 py-2 text-sm font-medium text-[#0D1B2A] hover:bg-white/90 transition-colors"
              >
                Подписаться
              </button>
            </form>
          </div>
        </div>

        <div className="mt-12 border-t border-white/10 pt-8 text-center text-sm">
          <p className="mb-1">Ставропольский край, г. Ставрополь, пр-д Черняховского, 3</p>
          <p className="mb-1">
            +7 (8652) 24-25-27 |{" "}
            <a href="mailto:college@stvcc.ru" className="hover:text-white transition-colors">
              college@stvcc.ru
            </a>
          </p>
          <div className="mt-4 flex justify-center">
            <AccessibilityToggle />
          </div>
          <p className="mt-4 text-xs text-white/50">
            ГБПОУ «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»
          </p>
          <p className="mt-2 text-xs text-white/40">
            &copy; {new Date().getFullYear()} Все права защищены |{" "}
            <Link href="/privacy" className="hover:text-white transition-colors">
              Политика конфиденциальности
            </Link>
          </p>
        </div>
      </div>
    </footer>
  )
}
