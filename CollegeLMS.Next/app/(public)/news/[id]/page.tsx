"use client"

import { useEffect, useState, useMemo, useCallback, useRef } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import type { Result, NewsResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import ContentRenderer from "@/components/ContentRenderer"
import Image from "next/image"
import { X, ChevronLeft, ChevronRight } from "lucide-react"

const normalizeUrl = (url: string) =>
  url.replace(/-[0-9]+x[0-9]+(\.[a-z]+)$/, "$1")

export default function NewsDetailPage() {
  const params = useParams()
  const router = useRouter()
  const [news, setNews] = useState<NewsResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [galleryIndex, setGalleryIndex] = useState(0)
  const [galleryOpen, setGalleryOpen] = useState(false)
  const contentRef = useRef<HTMLDivElement>(null)

  const allImages = useMemo(() => {
    const urlSet = new Set<string>()
    if (!news) return []

    if (news.imageUrl) urlSet.add(normalizeUrl(news.imageUrl))

    const anchorRe = /<a[^>]+href="([^"]+\.(?:jpe?g|png|gif|webp))"[^>]*>/gi
    let m: RegExpExecArray | null
    while ((m = anchorRe.exec(news.content)) !== null) urlSet.add(normalizeUrl(m[1]))

    const imgRe = /<img[^>]+src="([^"]+)"[^>]*>/gi
    while ((m = imgRe.exec(news.content)) !== null) urlSet.add(normalizeUrl(m[1]))

    return Array.from(urlSet)
  }, [news])

  const excerpt = useMemo(() => {
    if (!news) return ""
    const text = news.content
      .replace(/<[^>]+>/g, " ")
      .replace(/\s+/g, " ")
      .trim()
    return text.length > 200 ? `${text.slice(0, 200)}…` : text
  }, [news])

  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (!galleryOpen) return
      if (e.key === "ArrowLeft" && galleryIndex > 0) setGalleryIndex(i => i - 1)
      if (e.key === "ArrowRight" && galleryIndex < allImages.length - 1) setGalleryIndex(i => i + 1)
      if (e.key === "Escape") setGalleryOpen(false)
    },
    [galleryOpen, galleryIndex, allImages.length]
  )

  useEffect(() => {
    window.addEventListener("keydown", handleKeyDown)
    return () => window.removeEventListener("keydown", handleKeyDown)
  }, [handleKeyDown])

  useEffect(() => {
    const el = contentRef.current
    if (!el || allImages.length === 0) return

    const controller = new AbortController()
    const { signal } = controller

    el.addEventListener(
      "click",
      e => {
        const target = e.target as HTMLElement

        let img = target.closest("img")
        if (!img) {
          const anchor = target.closest("a")
          if (anchor) img = anchor.querySelector("img")
        }
        if (!img) return

        const src = img.getAttribute("src")
        if (!src) return
        const idx = allImages.indexOf(normalizeUrl(src))
        if (idx < 0) return

        e.preventDefault()
        e.stopPropagation()
        setGalleryIndex(idx)
        setGalleryOpen(true)
      },
      { signal }
    )

    return () => controller.abort()
  }, [news, allImages])

  useEffect(() => {
    if (!params.id) return
    setLoading(true)
    setError(null)
    api
      .get<Result<NewsResponse>>(`/api/news/${params.id}`)
      .then(res => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setNews(body.data)
        } else {
          if (body.statusCode === 404) {
            setError("Новость не найдена")
          } else {
            setError(body.errorMessage ?? "Ошибка загрузки")
          }
        }
      })
      .catch(() => setError("Ошибка загрузки новости"))
      .finally(() => setLoading(false))
  }, [params.id])

  if (loading) {
    return (
      <div className="flex justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
      </div>
    )
  }

  if (error || !news) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-20 text-center">
        <p className="mb-4 text-lg text-destructive">{error ?? "Новость не найдена"}</p>
        <div className="flex justify-center gap-3">
          <Button variant="outline" onClick={() => router.back()}>
            ← Назад
          </Button>
          <Button asChild>
            <Link href="/news">Все новости</Link>
          </Button>
        </div>
      </div>
    )
  }

  return (
    <article className="mx-auto max-w-3xl px-4 py-10 sm:px-6">
      <Button variant="ghost" size="sm" className="mb-6" asChild>
        <Link href="/news">← Все новости</Link>
      </Button>

      {allImages.length > 0 ? (
        <div className="mb-8 grid grid-cols-1 overflow-hidden rounded-xl border border-border lg:grid-cols-3">
          <div className="flex flex-col justify-center gap-3 bg-primary p-6 text-primary-foreground lg:p-8">
            <p className="text-sm text-primary-foreground/80">
              {new Date(news.publishedAt).toLocaleDateString("ru-RU", {
                year: "numeric",
                month: "long",
                day: "numeric",
              })}
              {news.categoryName && ` · ${news.categoryName}`}
            </p>
            <h1 className="text-xl font-bold leading-tight sm:text-2xl">{news.title}</h1>
            {excerpt && <p className="line-clamp-3 text-sm text-primary-foreground/90">{excerpt}</p>}
          </div>
          <button
            onClick={() => { setGalleryIndex(0); setGalleryOpen(true) }}
            className="lg:col-span-2 lg:min-h-[320px] overflow-hidden rounded-r-xl text-left"
            aria-label="Открыть фотографию"
          >
            <Image
              src={allImages[0]}
              alt=""
              width={0}
              height={0}
              sizes="(min-width: 1024px) 66vw, 100vw"
              className="h-full max-h-[420px] w-full object-cover"
              style={{ width: "100%", height: "100%", maxHeight: "420px" }}
            />
          </button>
        </div>
      ) : (
        <div className="mb-8 rounded-xl bg-primary p-6 lg:p-8">
          <p className="mb-2 text-sm text-primary-foreground/80">
            {new Date(news.publishedAt).toLocaleDateString("ru-RU", {
              year: "numeric",
              month: "long",
              day: "numeric",
            })}
            {news.categoryName && ` · ${news.categoryName}`}
          </p>
          <h1 className="text-2xl font-bold leading-tight text-primary-foreground sm:text-3xl">
            {news.title}
          </h1>
        </div>
      )}

      <div ref={contentRef} className="[&_img]:cursor-pointer">
        <ContentRenderer content={news.content} />
      </div>

      <div className="mt-10 border-t border-border pt-4 text-xs text-muted-foreground">
        Опубликовано: {news.createdByName}
      </div>

      {/* Lightbox */}
      {galleryOpen && allImages.length > 0 && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4"
          onClick={() => setGalleryOpen(false)}
        >
          <button
            onClick={() => setGalleryOpen(false)}
            className="absolute right-4 top-4 z-10 text-white/70 transition-colors hover:text-white"
            aria-label="Закрыть"
          >
            <X size={32} />
          </button>

          {galleryIndex > 0 && (
            <button
              onClick={e => { e.stopPropagation(); setGalleryIndex(i => i - 1) }}
              className="absolute left-4 top-1/2 z-10 -translate-y-1/2 text-white/70 transition-colors hover:text-white"
              aria-label="Предыдущее"
            >
              <ChevronLeft size={40} />
            </button>
          )}

          <Image
            src={allImages[galleryIndex]}
            alt=""
            width={0}
            height={0}
            sizes="100vw"
            className="max-h-[90vh] max-w-[90vw] object-contain"
            style={{ width: 'auto', height: 'auto' }}
            onClick={e => e.stopPropagation()}
            unoptimized
          />

          {galleryIndex < allImages.length - 1 && (
            <button
              onClick={e => { e.stopPropagation(); setGalleryIndex(i => i + 1) }}
              className="absolute right-4 top-1/2 z-10 -translate-y-1/2 text-white/70 transition-colors hover:text-white"
              aria-label="Следующее"
            >
              <ChevronRight size={40} />
            </button>
          )}

          <div className="absolute bottom-4 left-1/2 z-10 -translate-x-1/2 rounded-full bg-black/50 px-3 py-1 text-sm text-white/80">
            {galleryIndex + 1} / {allImages.length}
          </div>
        </div>
      )}
    </article>
  )
}
