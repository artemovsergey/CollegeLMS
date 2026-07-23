"use client"

import { useState, useEffect, useCallback } from "react"
import useEmblaCarousel from "embla-carousel-react"
import Link from "next/link"
import Image from "next/image"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { useDesign } from "@/lib/design-provider"
import CarouselTPU from "./CarouselTPU"

function CarouselDefault() {
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
              <Link
                key={item.id}
                href={`/news/${item.id}`}
                className="relative min-w-0 flex-[0_0_100%] h-[400px] md:h-[550px]"
              >
              {item.imageUrl ? (
                <>
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={item.imageUrl}
                    alt=""
                    className="h-full w-full object-cover rounded-lg"
                  />
                </>
              ) : (
                <div className="absolute inset-0 bg-gradient-to-br from-lilac/80 via-primary/60 to-blue-900/80" />
              )}

              <div className="absolute inset-0 bg-gradient-to-r from-[#568EDD] via-[#568EDD]/70 to-transparent pointer-events-none" />

              <div className="absolute top-4 left-4 sm:top-6 sm:left-6 z-10 drop-shadow-[0_2px_6px_rgba(0,0,0,0.4)]">
                <Image
                  src="/logo.svg"
                  alt="Ставропольский колледж связи"
                  width={120}
                  height={80}
                  className="w-auto h-auto"
                  style={{ maxWidth: "120px", maxHeight: "80px" }}
                  unoptimized
                />
              </div>

              <div className="absolute bottom-0 left-0 right-0 p-6 sm:p-10">
                <p className="mb-2 text-sm text-white/80">
                  {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                </p>
                <h2 className="mb-2 text-xl font-bold text-white sm:text-2xl md:text-3xl line-clamp-2 [text-shadow:_0_2px_4px_rgb(0_0_0_/_50%)]">
                  {item.title}
                </h2>
                <p className="max-w-2xl text-sm text-white/70 line-clamp-2 [text-shadow:_0_1px_3px_rgb(0_0_0_/_50%)]">
                  {item.content.replace(/<[^>]*>/g, '').slice(0, 100)}
                  {item.content.length > 100 ? "..." : ""}
                </p>
                <div className="mt-4">
                  <span className="inline-block rounded-full bg-white/20 px-6 py-2 text-sm font-medium text-white backdrop-blur-sm transition-colors hover:bg-white/30">
                    Подробнее
                  </span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </div>

      {slides.length > 1 && (
        <>
          <button
            onClick={scrollPrev}
            className="absolute left-4 top-1/2 -translate-y-1/2 rounded-full bg-white/20 p-2 text-white backdrop-blur-sm transition-colors hover:bg-white/40"
            aria-label="Предыдущий"
          >
            <ChevronLeft size={24} />
          </button>
          <button
            onClick={scrollNext}
            className="absolute right-4 top-1/2 -translate-y-1/2 rounded-full bg-white/20 p-2 text-white backdrop-blur-sm transition-colors hover:bg-white/40"
            aria-label="Следующий"
          >
            <ChevronRight size={24} />
          </button>

          <div className="absolute bottom-4 left-1/2 flex -translate-x-1/2 gap-2">
            {slides.map((_, i) => (
              <button
                key={i}
                onClick={() => scrollTo(i)}
                className={`h-2 rounded-full transition-all ${
                  i === selectedIndex ? "w-6 bg-white" : "w-2 bg-white/50"
                }`}
                aria-label={`Слайд ${i + 1}`}
              />
            ))}
          </div>
        </>
      )}
    </section>
    </div>
  )
}

export default function Carousel() {
  const { design } = useDesign()
  if (design === "tpu") return <CarouselTPU />
  return <CarouselDefault />
}
