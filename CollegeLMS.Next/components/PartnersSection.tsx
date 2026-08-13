import { partners } from "@/data/partners"

export default function PartnersSection() {
  return (
    <section className="bg-muted py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Наши партнёры</h2>

        <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-4">
          {partners.map((p) => (
            <a
              key={p.name}
              href={p.href}
              target="_blank"
              rel="noopener noreferrer"
              className="group flex flex-col items-center rounded-lg border border-border bg-card p-6 text-center transition-colors duration-200 hover:border-accent/30"
            >
              <div className="mb-3 flex h-16 items-center justify-center">
                <img
                  src={p.logo}
                  alt={`Логотип ${p.name}`}
                  className="h-14 w-auto max-w-[200px] object-contain"
                />
              </div>
              <h3 className="mb-1 text-sm font-semibold text-primary">{p.name}</h3>
              <p className="text-xs text-muted-foreground">{p.description}</p>
            </a>
          ))}
        </div>
      </div>
    </section>
  )
}