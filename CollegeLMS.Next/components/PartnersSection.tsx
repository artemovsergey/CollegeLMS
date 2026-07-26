const partners = [
  {
    name: "ПАО «ОДК-Сатурн»",
    description: "Предприятие авиационного двигателестроения, партнёр в подготовке кадров",
    svg: (
      <svg viewBox="0 0 100 100" fill="none" className="h-10 w-10">
        <circle cx="50" cy="50" r="38" stroke="#1a5fb4" strokeWidth="3" fill="none" />
        <circle cx="50" cy="50" r="22" stroke="#1a5fb4" strokeWidth="2.5" fill="none" />
        <circle cx="50" cy="50" r="8" fill="#1a5fb4" />
        <path d="M50 12v10M50 78v10M12 50h10M78 50h10" stroke="#1a5fb4" strokeWidth="2.5" strokeLinecap="round" />
        <path d="M25.4 25.4l7.1 7.1M67.5 67.5l7.1 7.1M67.5 25.4l-7.1 7.1M25.4 67.5l7.1-7.1" stroke="#1a5fb4" strokeWidth="2" strokeLinecap="round" opacity="0.6" />
      </svg>
    ),
  },
  {
    name: "Военный университет радиоэлектроники",
    description: "Высшее учебное заведение, стратегический партнёр в области радиоэлектроники",
    svg: (
      <svg viewBox="0 0 100 100" fill="none" className="h-10 w-10">
        <path d="M50 10l9 27h29l-23 17 9 27-24-17-24 17 9-27-23-17h29z" fill="#c62828" opacity="0.9" />
        <circle cx="50" cy="42" r="10" fill="white" />
        <path d="M50 30v-8M50 52v6M34 38l-6-2M62 38l6-2M38 48l-4 4M58 48l4 4" stroke="#fff" strokeWidth="1.5" strokeLinecap="round" opacity="0.7" />
      </svg>
    ),
  },
  {
    name: "Министерство энергетики, промышленности и связи СК",
    description: "Учредитель колледжа, курирующий образовательную деятельность",
    svg: (
      <svg viewBox="0 0 100 100" fill="none" className="h-10 w-10">
        <rect x="10" y="10" width="80" height="80" rx="8" stroke="#2e7d32" strokeWidth="2.5" fill="none" />
        <path d="M50 82V45M50 36V26" stroke="#2e7d32" strokeWidth="2.5" strokeLinecap="round" />
        <rect x="38" y="48" width="24" height="34" rx="3" fill="#2e7d32" opacity="0.25" />
        <path d="M30 62h40" stroke="#2e7d32" strokeWidth="2" />
        <circle cx="50" cy="22" r="5" fill="#2e7d32" />
      </svg>
    ),
  },
  {
    name: "ПАО «Ростелеком»",
    description: "Крупнейший провайдер цифровых услуг, база практики студентов",
    svg: (
      <svg viewBox="0 0 100 100" fill="none" className="h-10 w-10">
        <rect x="12" y="24" width="76" height="52" rx="6" fill="#00aa00" />
        <rect x="22" y="34" width="56" height="32" rx="3" fill="white" />
        <text x="50" y="58" textAnchor="middle" fill="#00aa00" fontSize="32" fontWeight="bold" fontFamily="sans-serif">RT</text>
        <path d="M12 30l38 20 38-20" stroke="#00aa00" strokeWidth="1.5" fill="none" opacity="0.3" />
      </svg>
    ),
  },
  {
    name: "АО «ЭР-Телеком»",
    description: "Телекоммуникационная компания, партнёр в сфере IT и связи",
    svg: (
      <svg viewBox="0 0 100 100" fill="none" className="h-10 w-10">
        <circle cx="50" cy="50" r="38" fill="#008080" />
        <circle cx="50" cy="50" r="34" fill="white" />
        <text x="50" y="56" textAnchor="middle" fill="#008080" fontSize="26" fontWeight="bold" fontFamily="sans-serif">ЭР</text>
        <circle cx="50" cy="50" r="40" stroke="#008080" strokeWidth="2" strokeDasharray="4 3" fill="none" />
      </svg>
    ),
  },
]

export default function PartnersSection() {
  return (
    <section className="bg-muted py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Наши партнёры</h2>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {partners.map((p) => (
            <div
              key={p.name}
              className="flex flex-col items-center rounded-lg border border-border bg-card p-6 text-center transition-all duration-200 hover:border-accent/30 hover:shadow-sm"
            >
              <span className="mb-3 flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary">
                {p.svg}
              </span>
              <h3 className="mb-1 text-sm font-semibold text-primary">{p.name}</h3>
              <p className="text-xs text-muted-foreground">{p.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
