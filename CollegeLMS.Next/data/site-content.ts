export interface Subsection {
  title: string
  slug: string
  href: string
  content: string
}

export interface Section {
  title: string
  slug: string
  href: string
  subsections: Subsection[]
}

export const siteNavigation: Section[] = [
  {
    title: "Колледж",
    slug: "college",
    href: "/college",
    subsections: [
      { title: "О колледже", slug: "o-kolledzhe", href: "/college/o-kolledzhe", content: "" },
      { title: "История создания", slug: "istoriya-sozdaniya-kolledzha", href: "/college/istoriya-sozdaniya-kolledzha", content: "" },
      { title: "Воспитательная работа", slug: "vospitatelnaya-rabota", href: "/college/vospitatelnaya-rabota", content: "" },
      { title: "Наставничество", slug: "nastavnichestvo", href: "/college/nastavnichestvo", content: "" },
      { title: "Общежитие", slug: "obshhezhitie", href: "/college/obshhezhitie", content: "" },
      { title: "Профсоюзная организация", slug: "pervichnaya-profsoyuznaya-organizatsiya", href: "/college/pervichnaya-profsoyuznaya-organizatsiya", content: "" },
      { title: "Независимая оценка качества", slug: "nezavisimaya-otsenka-kachestva-uslovij-osushhestvleniya-obrazovatelnoj-deyatelnosti", href: "/college/nezavisimaya-otsenka-kachestva-uslovij-osushhestvleniya-obrazovatelnoj-deyatelnosti", content: "" },
      { title: "План работы", slug: "plan-raboty-gbpou-sks-na-tekushhij-uchebnyj-god", href: "/college/plan-raboty-gbpou-sks-na-tekushhij-uchebnyj-god", content: "" },
      { title: "Контакты", slug: "polnaya-kontaktnaya-informatsiya", href: "/college/polnaya-kontaktnaya-informatsiya", content: "" },
      { title: "Сведения об ОО", slug: "svedeniya-oo", href: "/about", content: "" },
    ],
  },
  {
    title: "Обучение",
    slug: "education",
    href: "/education",
    subsections: [
      { title: "Специальности", slug: "perechen-spetsialnostey", href: "/education/perechen-spetsialnostey", content: "" },
      { title: "Курсы", slug: "kursyi", href: "/education/kursyi", content: "" },
      { title: "Целевое обучение", slug: "tselevoe-obuchenie", href: "/education/tselevoe-obuchenie", content: "" },
      { title: "Оплата услуг", slug: "oplata-uslug", href: "/education/oplata-uslug", content: "" },
      { title: "Образовательный кредит", slug: "obrazovatelnyj-kredit", href: "/education/obrazovatelnyj-kredit", content: "" },
    ],
  },
  {
    title: "Поступление",
    slug: "admissions",
    href: "/admissions",
    subsections: [
      { title: "Приемная комиссия", slug: "priemnaya-komissiya", href: "/admissions/priemnaya-komissiya", content: "" },
      { title: "Специальности", slug: "perechen-spetsialnostey", href: "/admissions/perechen-spetsialnostey", content: "" },
      { title: "Дни открытых дверей", slug: "den-otkrytyh-dverej-2026", href: "/admissions/den-otkrytyh-dverej-2026", content: "" },
      { title: "Приказы о зачислении", slug: "prikazy-na-zachislenie-2025", href: "/admissions/prikazy-na-zachislenie-2025", content: "" },
      { title: "Документы для приема", slug: "kakie-dokumentyi-neobhodimo-prinesti", href: "/admissions/kakie-dokumentyi-neobhodimo-prinesti", content: "" },
      { title: "Общежитие", slug: "obshhezhitie", href: "/admissions/obshhezhitie", content: "" },
      { title: "Количество заявлений", slug: "kolichestvo-podannyih-zayavleniy", href: "/admissions/kolichestvo-podannyih-zayavleniy", content: "" },
      { title: "Задать вопрос", slug: "8008-2", href: "/admissions/8008-2", content: "" },
    ],
  },
  {
    title: "Студентам",
    slug: "student-life",
    href: "/student-life",
    subsections: [
      { title: "Расписание занятий", slug: "raspisanie-zanyatij-po-ochnoj-forme-obucheniya", href: "/student-life/raspisanie-zanyatij-po-ochnoj-forme-obucheniya", content: "" },
      { title: "Расписание экзаменов", slug: "raspisanie-ekzamenov", href: "/student-life/raspisanie-ekzamenov", content: "" },
      { title: "Государственная итоговая аттестация", slug: "raspisanie-gosudarstvennoj-itogovoj-attestatsii", href: "/student-life/raspisanie-gosudarstvennoj-itogovoj-attestatsii", content: "" },
      { title: "Задолженности", slug: "raspisanie-likvidatsii-akademicheskih-zadolzhennostej", href: "/student-life/raspisanie-likvidatsii-akademicheskih-zadolzhennostej", content: "" },
      { title: "Библиотека", slug: "biblioteka", href: "/student-life/biblioteka", content: "" },
      { title: "Дистанционное обучение", slug: "distancionnoeobuch", href: "/student-life/distancionnoeobuch", content: "" },
      { title: "Трудоустройство и карьера", slug: "trudoustroystvo-i-karera", href: "/graduates/trudoustroystvo-i-karera", content: "" },
      { title: "Центр содействия трудоустройству", slug: "tsentr-sodejstviya-trudoustrojstvu-vypusknikov", href: "/graduates/tsentr-sodejstviya-trudoustrojstvu-vypusknikov", content: "" },
      { title: "Актуальные вакансии", slug: "aktualnyie-vakansii", href: "/graduates/aktualnyie-vakansii", content: "" },
      { title: "Оставить резюме", slug: "ostavit-rezyume-dlya-poiska-rabotyi", href: "/graduates/ostavit-rezyume-dlya-poiska-rabotyi", content: "" },
      { title: "Полезные ссылки", slug: "poleznyie-ssyilki", href: "/graduates/poleznyie-ssyilki", content: "" },
    ],
  },
]

export function getSectionBySlug(slug: string): Section | undefined {
  return siteNavigation.find((s) => s.slug === slug)
}

export function getSubsection(sectionSlug: string, subSlug: string): Subsection | undefined {
  const section = getSectionBySlug(sectionSlug)
  return section?.subsections.find((s) => s.slug === subSlug)
}

export const partnersContent = `
  <h2>Наши партнёры</h2>
  <p>Колледж сотрудничает с ведущими предприятиями связи и информационных технологий Ставропольского края и России.</p>
  <ul>
    <li>ПАО «Ростелеком»</li>
    <li>ПАО «МТС»</li>
    <li>ПАО «МегаФон»</li>
    <li>ПАО «ВымпелКом»</li>
    <li>АО «Почта России»</li>
    <li>ПАО «ОДК-Сатурн»</li>
  </ul>
`
