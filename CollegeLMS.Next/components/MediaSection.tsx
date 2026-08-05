"use client"

import { useState } from "react"
import { Play, Calendar, Clock, Tv, X } from "lucide-react"
import Image from "next/image"

const VIDEO_ID = "1b0e5af3e47d34c76be2a3dd66c77fd5"
const THUMB_URL = "https://pic.rtbcdn.ru/video/2026-01-15/83/dc/83dc9b637234582645635b585150d170.jpg"

export default function MediaSection() {
  const [open, setOpen] = useState(false)

  return (
    <section className="py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Колледж в СМИ</h2>

        <div className="mx-auto max-w-2xl">
          <button
            onClick={() => setOpen(true)}
            className="group flex w-full gap-5 rounded-lg border border-border bg-card p-5 text-left transition-all duration-200 hover:border-accent/30"
          >
            <div className="relative flex h-24 w-40 shrink-0 items-center justify-center overflow-hidden rounded-md bg-muted">
              <Image
                src={THUMB_URL}
                alt="Превью видео"
                fill
                className="object-cover"
                sizes="160px"
                unoptimized
              />
              <span className="absolute inset-0 flex items-center justify-center bg-black/0 transition-colors group-hover:bg-black/30">
                <span className="flex h-10 w-10 items-center justify-center rounded-full bg-white/90 text-primary shadow-sm transition-transform group-hover:scale-110">
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
          </button>
        </div>
      </div>

      {open && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4"
          onClick={() => setOpen(false)}
        >
          <div
            className="relative w-full max-w-3xl overflow-hidden rounded-lg bg-black shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <button
              onClick={() => setOpen(false)}
              className="absolute right-2 top-2 z-10 flex h-8 w-8 items-center justify-center rounded-full bg-black/50 text-white hover:bg-black/70"
              aria-label="Закрыть"
            >
              <X size={18} />
            </button>
            <div className="aspect-video w-full">
              <iframe
                src={`https://rutube.ru/play/embed/${VIDEO_ID}`}
                width="100%"
                height="100%"
                allow="autoplay; fullscreen"
                allowFullScreen
                className="block"
              />
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
