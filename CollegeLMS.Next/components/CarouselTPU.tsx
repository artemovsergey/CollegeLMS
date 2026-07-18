"use client"

import { useState, useEffect, useCallback } from "react"
import useEmblaCarousel from "embla-carousel-react"
import Link from "next/link"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { ChevronLeft, ChevronRight } from "lucide-react"

export default function CarouselTPU() {
  const [slides, setSlides] = useState<NewsResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [emblaRef, emblaApi] = useEmblaCarousel({ loop: true })
  const [selectedIndex, setSelectedIndex] = useState(0)
  const [isHovered, setIsHovered] = useState(false)

  useEffect(() => {
    api
      .get<Result<PagedResponse<NewsResponse>>>("/api/news?page=1&pageSize=50")
      .then((res) => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setSlides(body.data.items.filter((n) => n.imageUrl).slice(0, 5))
        }
      })
      .catch(() => setError("Не удалось загрузить"))
      .finally(() => setLoading(false))
  }, [])

  const onSelect = useCallback(() => {
    if (!emblaApi) return
    setSelectedIndex(emblaApi.selectedScrollSnap())
  }, [emblaApi])

  useEffect(() => {
    if (!emblaApi) return
    emblaApi.on("select", onSelect)
    onSelect()
  }, [emblaApi, onSelect])

  useEffect(() => {
    if (!emblaApi || slides.length < 2 || isHovered) return
    const timer = setInterval(() => emblaApi.scrollNext(), 5000)
    return () => clearInterval(timer)
  }, [emblaApi, slides.length, isHovered])

  const scrollPrev = useCallback(() => emblaApi?.scrollPrev(), [emblaApi])
  const scrollNext = useCallback(() => emblaApi?.scrollNext(), [emblaApi])
  const scrollTo = useCallback(
    (index: number) => emblaApi?.scrollTo(index),
    [emblaApi],
  )

  if (loading) {
    return (
      <section className="h-[80vh] min-h-[600px] w-full bg-gray-100 animate-pulse" />
    )
  }

  if (error || slides.length === 0) return null

  return (
    <section
      className="relative h-[80vh] min-h-[600px] w-full"
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div className="overflow-hidden h-full" ref={emblaRef}>
        <div className="flex h-full">
          {slides.map((item) => (
            <Link
              key={item.id}
              href={`/news/${item.id}`}
              className="relative min-w-0 flex-[0_0_100%] h-full"
            >
              <img
                src={item.imageUrl ?? ""}
                alt=""
                className="h-full w-full object-cover"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/30 to-transparent" />
              <div className="absolute bottom-0 left-0 right-0 p-8 lg:p-16 max-w-3xl">
                {item.categoryName && (
                  <span className="inline-block mb-3 text-xs font-semibold uppercase tracking-wider text-white/80">
                    {item.categoryName}
                  </span>
                )}
                <span className="inline-block mb-3 text-xs text-white/60">
                  {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                </span>
                <h1 className="text-3xl lg:text-5xl font-bold text-white leading-tight mb-4">
                  {item.title}
                </h1>
                <p className="text-white/80 text-lg mb-6 line-clamp-2">
                  {item.content?.replace(/<[^>]*>/g, "").slice(0, 150) ?? ""}
                </p>
                <span className="inline-flex items-center gap-2 px-6 py-3 bg-[#0066cc] text-white text-sm font-medium rounded-md hover:bg-[#0052a3] transition-colors">
                  Подробнее →
                </span>
              </div>
            </Link>
          ))}
        </div>
      </div>

      <div className="absolute bottom-6 left-1/2 -translate-x-1/2 flex gap-2">
        {slides.map((_, i) => (
          <button
            key={i}
            onClick={() => scrollTo(i)}
            className={`h-2.5 rounded-full transition-all ${
              i === selectedIndex ? "bg-white w-8" : "bg-white/50 w-2.5"
            }`}
            aria-label={`Слайд ${i + 1}`}
          />
        ))}
      </div>

      <button
        onClick={scrollPrev}
        className="absolute left-4 top-1/2 -translate-y-1/2 p-2 rounded-full bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm transition-all"
        aria-label="Предыдущий"
      >
        <ChevronLeft size={24} />
      </button>
      <button
        onClick={scrollNext}
        className="absolute right-4 top-1/2 -translate-y-1/2 p-2 rounded-full bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm transition-all"
        aria-label="Следующий"
      >
        <ChevronRight size={24} />
      </button>
    </section>
  )
}
