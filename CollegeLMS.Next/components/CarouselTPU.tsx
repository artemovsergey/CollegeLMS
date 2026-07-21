"use client"

import { useState, useEffect, useCallback } from "react"
import useEmblaCarousel from "embla-carousel-react"
import Link from "next/link"
import { ArrowRight } from "lucide-react"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"

export default function CarouselTPU() {
  const [slides, setSlides] = useState<NewsResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [emblaRef, emblaApi] = useEmblaCarousel({ loop: true })
  const [selectedIndex, setSelectedIndex] = useState(0)

  useEffect(() => {
    api
      .get<Result<PagedResponse<NewsResponse>>>("/api/news?page=1&pageSize=50")
      .then((res) => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setSlides(body.data.items.filter((n) => n.imageUrl).slice(0, 5))
        }
      })
      .catch(() => {})
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
    if (!emblaApi || slides.length < 2) return
    const timer = setInterval(() => emblaApi.scrollNext(), 5000)
    return () => clearInterval(timer)
  }, [emblaApi, slides.length])

  if (loading) {
    return <div className="first-screen-tpu bg-gray-200" />
  }

  return (
    <div className="first-screen-tpu" style={{ marginTop: "calc(var(--header-h, 114px) * -1)" }}>
      <div className="first-screen-tpu__bg">
        <img
          src="/placeholder.svg?height=1080&width=1920"
          alt=""
          className="h-full w-full object-cover"
        />
      </div>
      <div className="first-screen-tpu__overlay" />

      <div className="first-screen-tpu__content">
        <div className="mx-auto max-w-7xl px-4 lg:px-8">
          <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-8">
            <div className="first-screen-tpu__title">
              <div className="text-white/80 text-lg font-medium mb-2">
                Томский политехнический университет
              </div>
              <h1 style={{ fontSize: "3rem", fontWeight: 700, color: "#fff", lineHeight: 1.1 }}>
                Миссия: инженер
                <br />
                <span style={{ fontSize: "1.5rem", fontWeight: 400, color: "rgba(255,255,255,0.85)" }}>
                  Первый технический университет в азиатской части России
                </span>
              </h1>
            </div>

            {slides.length > 0 && (
              <div className="card-slider-tpu" ref={emblaRef}>
                <div className="flex">
                  {slides.map((item) => (
                    <div key={item.id} className="min-w-0 flex-[0_0_100%]">
                      <div className="card-slider-tpu__item">
                        <div className="card-slider-tpu__item-title line-clamp-2">
                          {item.title}
                        </div>
                        <Link
                          href={`/news/${item.id}`}
                          className="card-slider-tpu__link"
                        >
                          Подробнее
                          <ArrowRight size={14} />
                        </Link>
                      </div>
                    </div>
                  ))}
                </div>
                <div className="card-slider-tpu__dots">
                  {slides.map((_, i) => (
                    <button
                      key={i}
                      onClick={() => emblaApi?.scrollTo(i)}
                      className={`card-slider-tpu__dot ${i === selectedIndex ? "active" : ""}`}
                      aria-label={`Слайд ${i + 1}`}
                    />
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
