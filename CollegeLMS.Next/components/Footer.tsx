"use client"

import Link from "next/link"

const infoLinks = [
  { title: "Расписание занятий", href: "/schedule" },
  { title: "Платное обучение", href: "/paid-education" },
  { title: "Общежития", href: "/dormitories" },
  { title: "Стипендии", href: "/scholarships" },
  { title: "Электронная информационно-образовательная среда", href: "/eios" },
  { title: "Противодействие коррупции", href: "/anti-corruption" },
  { title: "Доступная среда", href: "/accessible-environment" },
  { title: "Вакансии", href: "/vacancies" },
  { title: "Телефонный справочник", href: "/phonebook" },
  { title: "Образовательные кредиты", href: "/educational-loans" },
  { title: "Планы финансово-хозяйственной деятельности", href: "/financial-plans" },
  { title: "Обработка персональных данных", href: "/personal-data" },
  { title: "Охрана здоровья", href: "/health-protection" },
  { title: "Закупки", href: "/procurement" },
  { title: "Отчётность", href: "/reports" },
  { title: "Оценка условий труда", href: "/safety-assessment" },
]

const footerColumns = [
  {
    title: "Приёмная комиссия",
    items: [
      { label: "+7 (8652) 24-25-27", href: "tel:+78652242527" },
      { label: "college@stvcc.ru", href: "mailto:college@stvcc.ru" },
      { label: "пр-д Черняховского, 3", href: "/contacts" },
    ],
  },
  {
    title: "Реквизиты",
    items: [
      { label: "ИНН 2634092525", href: "#" },
      { label: "КПП 263401001", href: "#" },
      { label: "ОГРН 1132651000403", href: "#" },
    ],
  },
  {
    title: "Образование",
    items: [
      { label: "Специальности", href: "/specialties" },
      { label: "Профессии", href: "/professions" },
      { label: "Доп. образование", href: "/additional-education" },
    ],
  },
  {
    title: "Документы",
    items: [
      { label: "Устав", href: "/documents/charter" },
      { label: "Лицензия", href: "/documents/license" },
      { label: "Аккредитация", href: "/documents/accreditation" },
    ],
  },
]

export default function Footer() {
  return (
    <footer className="bg-white border-t border-border">
      <div className="mx-auto max-w-7xl px-4 lg:px-8 py-12">
        <div className="grid gap-6 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
          <div>
            <Link href="/" className="inline-block mb-3">
              <span className="text-base font-bold text-[#24386a]">Ставропольский колледж связи</span>
            </Link>
            <p className="text-xs leading-relaxed text-[#4a5a7a]">
              ГБПОУ «Ставропольский колледж связи<br />
              имени Героя Советского Союза В.А. Петрова»
            </p>
          </div>

          {footerColumns.map((col) => (
            <div key={col.title}>
              <h3 className="mb-3 text-sm font-semibold text-[#24386a]">{col.title}</h3>
              <ul className="space-y-1.5">
                {col.items.map((item) => (
                  <li key={item.label}>
                    <Link
                      href={item.href}
                      className="text-sm text-[#4a5a7a] hover:text-[#24386a] hover:underline transition-all duration-200"
                    >
                      {item.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-8 border-t border-[#c9ceda] pt-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-xs text-[#7a8aa5]">
            © {new Date().getFullYear()} ГБПОУ «Ставропольский колледж связи
            имени Героя Советского Союза В.А. Петрова». Все права защищены.
          </p>
          <div className="flex gap-4 text-xs text-[#7a8aa5]">
            <Link href="/sveden" className="hover:text-[#4a5a7a] transition-colors">
              Сведения об образовательной организации
            </Link>
            <Link href="/privacy" className="hover:text-[#4a5a7a] transition-colors">
              Политика конфиденциальности
            </Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
