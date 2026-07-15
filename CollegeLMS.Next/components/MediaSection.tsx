import { Play, Calendar, Clock, Tv } from "lucide-react"
import Link from "next/link"

export default function MediaSection() {
  return (
    <section className="py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Колледж в СМИ</h2>

        <div className="mx-auto max-w-2xl">
          <Link
            href="https://rutube.ru/video/1b0e5af3e47d34c76be2a3dd66c77fd5/"
            target="_blank"
            rel="noopener noreferrer"
            className="group flex gap-5 rounded-lg border border-border bg-card p-5 transition-all duration-200 hover:border-accent/30 hover:shadow-sm"
          >
            <div className="relative flex h-24 w-40 shrink-0 items-center justify-center overflow-hidden rounded-md bg-primary/10">
              <Tv size={36} className="text-primary/40" />
              <span className="absolute inset-0 flex items-center justify-center bg-black/0 transition-colors group-hover:bg-black/20">
                <span className="flex h-10 w-10 items-center justify-center rounded-full bg-white/80 text-primary">
                  <Play size={20} className="ml-0.5" />
                </span>
              </span>
            </div>
            <div className="flex flex-col justify-center">
              <div className="mb-1 flex items-center gap-2 text-xs text-muted-foreground">
                <Tv size={14} />
                <span>СВОЁ ТВ СТАВРОПОЛЬСКИЙ КРАЙ</span>
              </div>
              <h3 className="mb-1 text-sm font-semibold text-primary line-clamp-2 group-hover:text-accent">
                Актуальное интервью. Ставропольский колледж связи: итоги 2025 года
              </h3>
              <p className="mb-1 text-xs text-muted-foreground line-clamp-1">
                Гость студии — Галина Секацкая, директор ГБПОУ СКС
              </p>
              <div className="flex items-center gap-3 text-xs text-muted-foreground">
                <span className="flex items-center gap-1">
                  <Calendar size={12} /> 15 января 2026
                </span>
                <span className="flex items-center gap-1">
                  <Clock size={12} /> 24:34
                </span>
              </div>
            </div>
          </Link>
        </div>
      </div>
    </section>
  )
}
