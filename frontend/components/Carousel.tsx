"use client"

import { useState, useEffect, useCallback } from "react"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import Link from "next/link"
import { ChevronLeft, ChevronRight } from "lucide-react"

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
    <section className="relative overflow-hidden bg-[#152851]">
      <div className="relative mx-auto max-w-7xl">
        <div className="relative h-[400px] sm:h-[450px] md:h-[500px]">
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
                  className="h-full w-full object-cover"
                />
              )}
              <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/30 to-transparent" />
              <div className="absolute bottom-0 left-0 right-0 p-6 sm:p-10">
                <p className="mb-2 text-sm text-white/80">
                  {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                </p>
                <h2 className="text-xl font-bold text-white sm:text-2xl md:text-3xl line-clamp-2">
                  {item.title}
                </h2>
              </div>
            </Link>
          ))}
        </div>

        <button
          onClick={prev}
          className="absolute left-4 top-1/2 -translate-y-1/2 rounded-full bg-white/20 p-2 text-white backdrop-blur-sm transition-colors hover:bg-white/40"
          aria-label="Предыдущий"
        >
          <ChevronLeft size={24} />
        </button>
        <button
          onClick={next}
          className="absolute right-4 top-1/2 -translate-y-1/2 rounded-full bg-white/20 p-2 text-white backdrop-blur-sm transition-colors hover:bg-white/40"
          aria-label="Следующий"
        >
          <ChevronRight size={24} />
        </button>

        <div className="absolute bottom-4 left-1/2 flex -translate-x-1/2 gap-2">
          {slides.map((_, i) => (
            <button
              key={i}
              onClick={() => setCurrent(i)}
              className={`h-2 rounded-full transition-all ${
                i === current ? "w-6 bg-white" : "w-2 bg-white/50"
              }`}
              aria-label={`Слайд ${i + 1}`}
            />
          ))}
        </div>
      </div>
    </section>
  )
}
