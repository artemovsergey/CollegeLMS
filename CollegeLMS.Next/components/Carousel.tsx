"use client"

import { useState, useEffect, useCallback } from "react"
import useEmblaCarousel from "embla-carousel-react"
import Link from "next/link"
import Image from "next/image"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { ChevronLeft, ChevronRight } from "lucide-react"

export default function Carousel() {
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
      <div className="mx-auto max-w-7xl">
        <section>
          <div className="h-[400px] animate-pulse bg-white/5 md:h-[550px]" />
        </section>
      </div>
    )
  }

  if (error || slides.length === 0) return null

  return (
    <div className="mx-auto max-w-7xl">
      <section
        className="relative mt-6"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <div className="overflow-hidden" ref={emblaRef}>
          <div className="flex">
            {slides.map((item, index) => (
              <div
                key={item.id}
                className="relative flex min-h-0 flex-[0_0_100%] flex-col overflow-hidden rounded-lg bg-primary h-[480px] md:h-[550px] lg:h-[680px] lg:grid lg:grid-cols-3"
              >
                <Link
                  href={`/news/${item.id}`}
                  className="contents"
                >
                  <div className="relative flex flex-1 flex-col justify-center gap-2 overflow-y-auto p-5 text-primary-foreground sm:p-6 lg:col-span-1 lg:h-full lg:gap-3 lg:p-8">
                    <Image
                      src="/logo.svg"
                      alt="Ставропольский колледж связи"
                      width={2048}
                      height={1359}
                      sizes="(min-width: 1024px) 33vw, 0px"
                      className="hidden max-h-72 w-auto max-w-full object-contain lg:block"
                      unoptimized
                    />
                    <p className="text-sm text-primary-foreground/80">
                      {new Date(item.publishedAt).toLocaleDateString("ru-RU", {
                        year: "numeric",
                        month: "long",
                        day: "numeric",
                      })}
                      {item.categoryName && ` · ${item.categoryName}`}
                    </p>
                    <h2 className="line-clamp-2 text-xl font-bold leading-tight sm:text-2xl md:text-3xl">
                      {item.title}
                    </h2>
                    <p className="line-clamp-3 text-sm text-primary-foreground/90">
                      {item.content.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim()}
                    </p>
                    <div className="mt-2">
                      <span className="inline-block rounded-full bg-white/20 px-6 py-2 text-sm font-medium text-white backdrop-blur-sm transition-colors hover:bg-white/30">
                        Подробнее
                      </span>
                    </div>
                  </div>
                  <div className="relative order-first h-48 sm:h-56 lg:order-none lg:col-span-2 lg:h-full">
                    {item.imageUrl ? (
                      <Image
                        src={item.imageUrl}
                        alt=""
                        fill
                        sizes="(min-width: 1024px) 66vw, 100vw"
                        className="object-cover"
                      />
                    ) : (
                      <div className="absolute inset-0 bg-gradient-to-br from-accent/80 via-primary/60 to-accent/60" />
                    )}
                  </div>
                </Link>
                {slides.length > 1 && (
                  <div className="relative z-10 hidden flex-col items-center gap-3 self-end pb-8 lg:flex lg:col-span-1 lg:row-span-1 lg:-mt-8">
                    <div className="flex items-center gap-2">
                      <button
                        onClick={(e) => { e.preventDefault(); scrollPrev() }}
                        className="rounded-full bg-white/20 p-2 text-white backdrop-blur-sm transition-colors hover:bg-white/40"
                        aria-label="Предыдущий"
                      >
                        <ChevronLeft size={20} />
                      </button>
                      <span className="text-sm text-primary-foreground/70 min-w-[3rem] text-center">
                        {selectedIndex + 1}/{slides.length}
                      </span>
                      <button
                        onClick={(e) => { e.preventDefault(); scrollNext() }}
                        className="rounded-full bg-white/20 p-2 text-white backdrop-blur-sm transition-colors hover:bg-white/40"
                        aria-label="Следующий"
                      >
                        <ChevronRight size={20} />
                      </button>
                    </div>
                    <div className="flex gap-2">
                      {slides.map((_, i) => (
                        <button
                          key={i}
                          onClick={(e) => { e.preventDefault(); scrollTo(i) }}
                          className={`h-2 rounded-full transition-all ${
                            i === selectedIndex ? "w-6 bg-white" : "w-2 bg-white/50"
                          }`}
                          aria-label={`Слайд ${i + 1}`}
                        />
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))}
        </div>
      </div>
    </section>
    </div>
  )
}
