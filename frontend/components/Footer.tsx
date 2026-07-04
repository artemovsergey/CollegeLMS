import Link from "next/link"
import { siteNavigation } from "@/data/site-content"

export default function Footer() {
  return (
    <footer className="border-t border-border bg-white">
      <div className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <h3 className="mb-3 text-sm font-semibold text-[#152851]">Контакты</h3>
            <ul className="space-y-2 text-sm text-[#5a6a8a]">
              <li>Ставропольский край, г. Ставрополь</li>
              <li>пр-д Черняховского, 3</li>
              <li>+7 (8652) 24-25-27</li>
              <li>
                <a href="mailto:college@stvcc.ru" className="hover:text-[#568cd6] transition-colors">
                  college@stvcc.ru
                </a>
              </li>
            </ul>
          </div>
          <div>
            <h3 className="mb-3 text-sm font-semibold text-[#152851]">Разделы</h3>
            <ul className="space-y-2 text-sm text-[#5a6a8a]">
              {siteNavigation.map((section) => (
                <li key={section.slug}>
                  <Link href={section.href} className="hover:text-[#568cd6] transition-colors">
                    {section.title}
                  </Link>
                </li>
              ))}
              <li>
                <Link href="/news" className="hover:text-[#568cd6] transition-colors">
                  Новости
                </Link>
              </li>
              <li>
                <Link href="/partners" className="hover:text-[#568cd6] transition-colors">
                  Наши партнёры
                </Link>
              </li>
            </ul>
          </div>
          <div>
            <h3 className="mb-3 text-sm font-semibold text-[#152851]">Учредитель</h3>
            <p className="text-sm leading-relaxed text-[#5a6a8a]">
              Министерство энергетики, промышленности и связи Ставропольского края
            </p>
            <h3 className="mb-3 mt-6 text-sm font-semibold text-[#152851]">Мы в соцсетях</h3>
            <a
              href="https://vk.com/stvcc_official"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 text-sm text-[#5a6a8a] hover:text-[#568cd6] transition-colors"
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M15.684 0H8.316C2.816 0 0 2.816 0 8.316v7.368C0 21.184 2.816 24 8.316 24h7.368C21.184 24 24 21.184 24 15.684V8.316C24 2.816 21.184 0 15.684 0zm3.189 16.4h-1.6c-.6 0-.8-.4-1.6-1.2-.8-.8-1.2-.8-1.6-.4-.8.4-.8 1.2-.8 2 0 .4-.4.8-.8.8h-1.2c-1.2 0-2.8-.4-4-1.6-2-2-4-5.6-4-6 0-.4 0-.8.4-.8h1.6c.4 0 .8.4.8.8.4.8 1.2 2 2 2.8.4.4.8.4 1.2 0 .4-.4.4-.8.4-1.6 0-.4.4-.8.8-.8h2.8c.4 0 .8.4.8.8v.4c0 .4-.4.8-.8.8h-.8c-.4 0-.8.4-.8.8s.4.8.8 1.2c.4.4.8.8 1.2 1.2.4.4.8.8 1.2.8.4 0 .8.4.8.8v1.6c0 .4-.4.8-.8.8z"/>
              </svg>
              ВКонтакте
            </a>
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
          &copy; {new Date().getFullYear()} ГБПОУ «Ставропольский колледж связи
          имени Героя Советского Союза В.А. Петрова». Все права защищены.
        </div>
      </div>
    </footer>
  )
}
