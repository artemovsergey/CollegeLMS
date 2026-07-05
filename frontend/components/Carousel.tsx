"use client"

import { useState, useEffect, useCallback } from "react"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import Link from "next/link"
import { ChevronLeft, ChevronRight, Calendar } from "lucide-react"

export default function Carousel() {
  const [slides, setSlides] = useState<NewsResponse[]>([])
  const [current, setCurrent] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<Result<PagedResponse<NewsResponse>>>("/api/news?page=1&pageSize=10")
      .then((res) => {
        const body = res.data
        if (body.isSuccess && body.data) {
          const withImages = body.data.items.filter((item) => item.imageUrl)
          setSlides(withImages.slice(0, 5))
        }
      })
      .catch(() => setError("Не удалось загрузить"))
      .finally(() => setLoading(false))
  }, [])

  const next = useCallback(() => {
    setCurrent((c) => (c + 1) % slides.length)
  }, [slides.length])

  const prev = useCallback(() => {
    setCurrent((c) => (c - 1 + slides.length) % slides.length)
  }, [slides.length])

  useEffect(() => {
    if (slides.length < 2) return
    const timer = setInterval(next, 5000)
    return () => clearInterval(timer)
  }, [slides.length, next])

  if (loading) return null

  if (error || slides.length === 0) return null

  return (
    <section className="relative overflow-hidden bg-[#f0e8d1]">
      <div className="relative mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <div className="relative aspect-[21/9] overflow-hidden rounded-xl border border-[#d4c9a8] shadow-lg">
          {slides.map((item, i) => (
            <Link
              key={item.id}
              href={`/news/${item.id}`}
              className={`absolute inset-0 transition-opacity duration-500 ${
                i === current ? "opacity-100" : "opacity-0 pointer-events-none"
              }`}
            >
              {item.imageUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={item.imageUrl}
                  alt=""
                  className="h-full w-full object-contain bg-foreground"
                />
              )}
              <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent" />
              <div className="absolute bottom-0 left-0 right-0 p-6 sm:p-8 md:p-10">
                <div className="flex items-center gap-2 mb-3">
                  <span className="inline-flex items-center gap-1.5 rounded-full bg-primary/90 px-3 py-1 text-xs font-medium text-white backdrop-blur-sm">
                    <Calendar size={12} />
                    {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                  </span>
                </div>
                <h2 className="text-xl font-bold text-white sm:text-2xl md:text-3xl lg:text-4xl line-clamp-2 drop-shadow-lg">
                  {item.title}
                </h2>
              </div>
            </Link>
          ))}
        </div>

        <button
          onClick={prev}
          className="absolute left-7 top-1/2 -translate-y-1/2 rounded-full bg-white/80 p-2 text-foreground shadow-md backdrop-blur-sm transition-colors hover:bg-white"
          aria-label="Предыдущий"
        >
          <ChevronLeft size={24} />
        </button>
        <button
          onClick={next}
          className="absolute right-7 top-1/2 -translate-y-1/2 rounded-full bg-white/80 p-2 text-foreground shadow-md backdrop-blur-sm transition-colors hover:bg-white"
          aria-label="Следующий"
        >
          <ChevronRight size={24} />
        </button>

        <div className="absolute bottom-11 left-1/2 flex -translate-x-1/2 gap-2">
          {slides.map((_, i) => (
            <button
              key={i}
              onClick={() => setCurrent(i)}
              className={`h-2 rounded-full transition-all ${
                i === current ? "w-6 bg-white" : "w-2 bg-white/60"
              }`}
              aria-label={`Слайд ${i + 1}`}
            />
          ))}
        </div>
      </div>
    </section>
  )
}
