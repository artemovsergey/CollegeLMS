"use client"

import { useEffect, useState, useCallback } from "react"
import type {
  Result,
  NewsResponse,
  NewsCategoryResponse,
  CreateNewsRequest,
  UpdateNewsRequest,
  PagedResponse,
  UploadResponse,
} from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

const PAGE_SIZE = 20

export default function AdminNewsPage() {
  const { user } = useAuth()
  const canManage = user?.role === "Admin" || user?.role === "Dispatcher"

  const [news, setNews] = useState<NewsResponse[]>([])
  const [categories, setCategories] = useState<NewsCategoryResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formTitle, setFormTitle] = useState("")
  const [formContent, setFormContent] = useState("")
  const [formImageUrl, setFormImageUrl] = useState("")
  const [formCategoryId, setFormCategoryId] = useState("")
  const [formPublished, setFormPublished] = useState(true)
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)
  const [uploading, setUploading] = useState(false)

  const fetchNews = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<PagedResponse<NewsResponse>>>(
        `/api/news?page=${page}&pageSize=${PAGE_SIZE}`
      )
      const body = res.data
      if (body.isSuccess && body.data) {
        setNews(body.data.items)
        setTotalPages(body.data.totalPages)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки новостей")
    } finally {
      setLoading(false)
    }
  }, [page])

  useEffect(() => {
    api
      .get<Result<NewsCategoryResponse[]>>("/api/news/categories")
      .then(res => {
        if (res.data.isSuccess && res.data.data) setCategories(res.data.data)
      })
      .catch(() => {})
  }, [])

  useEffect(() => {
    fetchNews()
  }, [fetchNews])

  const resetForm = () => {
    setFormTitle("")
    setFormContent("")
    setFormImageUrl("")
    setFormCategoryId("")
    setFormPublished(true)
    setFormError(null)
    setShowCreate(false)
    setEditingId(null)
  }

  const fillForm = (item: NewsResponse) => {
    setEditingId(item.id)
    setFormTitle(item.title)
    setFormContent(item.content)
    setFormImageUrl(item.imageUrl ?? "")
    setFormCategoryId(item.categoryId ?? "")
    setFormPublished(item.isPublished)
    setFormError(null)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateNewsRequest = {
        title: formTitle,
        content: formContent,
        imageUrl: formImageUrl || undefined,
        categoryId: formCategoryId || undefined,
        isPublished: formPublished,
      }
      const res = await api.post<Result<NewsResponse>>("/api/news", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchNews()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания новости")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: UpdateNewsRequest = {
        title: formTitle,
        content: formContent,
        imageUrl: formImageUrl || undefined,
        categoryId: formCategoryId || undefined,
        isPublished: formPublished,
      }
      const res = await api.put<Result<NewsResponse>>(`/api/news/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchNews()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления новости")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm("Удалить новость?")) return
    try {
      await api.delete(`/api/news/${id}`)
      await fetchNews()
    } catch {
      setError("Ошибка удаления")
    }
  }

  const handleTogglePublish = async (item: NewsResponse) => {
    try {
      await api.put<Result<NewsResponse>>(`/api/news/${item.id}`, {
        title: item.title,
        content: item.content,
        categoryId: item.categoryId ?? undefined,
        isPublished: !item.isPublished,
      })
      await fetchNews()
    } catch {
      setError("Ошибка изменения статуса")
    }
  }

  const formDialog = (
    <form onSubmit={editingId ? handleUpdate : handleCreate} className="flex flex-col gap-4">
      {formError && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{formError}</div>
      )}
      <div className="flex flex-col gap-2">
        <Label htmlFor="news-title">Заголовок</Label>
        <Input id="news-title" required value={formTitle} onChange={e => setFormTitle(e.target.value)} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="news-content">Текст</Label>
        <Textarea id="news-content" required value={formContent} onChange={e => setFormContent(e.target.value)} rows={8} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="news-image">Изображение (постер)</Label>
        <div className="flex items-center gap-2">
          <Input
            id="news-image"
            type="file"
            accept="image/jpeg,image/png"
            disabled={uploading}
            onChange={async e => {
              const file = e.target.files?.[0]
              if (!file) return
              setUploading(true)
              setFormError(null)
              try {
                const formData = new FormData()
                formData.append("file", file)
                const res = await api.post<Result<UploadResponse>>("/api/upload", formData, {
                  headers: { "Content-Type": undefined },
                })
                if (res.data.isSuccess && res.data.data) {
                  setFormImageUrl(res.data.data.url)
                } else {
                  setFormError(res.data.errorMessage ?? "Ошибка загрузки")
                }
              } catch {
                setFormError("Ошибка загрузки файла")
              } finally {
                setUploading(false)
              }
            }}
          />
          {uploading && (
            <div className="h-5 w-5 animate-spin rounded-full border-2 border-muted border-t-primary shrink-0" />
          )}
        </div>
        {formImageUrl && (
          <div className="relative mt-1 overflow-hidden rounded-md">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={formImageUrl}
              alt="Превью"
              className="h-32 w-full object-cover"
            />
          </div>
        )}
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="news-category">Категория</Label>
        <Select value={formCategoryId} onValueChange={setFormCategoryId}>
          <SelectTrigger id="news-category">
            <SelectValue placeholder="Без категории" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="none">Без категории</SelectItem>
            {categories.map(cat => (
              <SelectItem key={cat.id} value={cat.id}>
                {cat.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex items-center gap-2">
        <input
          id="news-published"
          type="checkbox"
          checked={formPublished}
          onChange={e => setFormPublished(e.target.checked)}
          className="h-4 w-4 rounded border-gray-300"
        />
        <Label htmlFor="news-published" className="text-sm">
          Опубликовано
        </Label>
      </div>
      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={resetForm}>
          Отмена
        </Button>
        <Button type="submit" disabled={formSubmitting}>
          {formSubmitting ? "Сохранение..." : editingId ? "Сохранить" : "Создать"}
        </Button>
      </div>
    </form>
  )

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-6xl">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Новости</h2>
        {canManage && (
          <Dialog
            open={showCreate}
            onOpenChange={open => {
              if (open) resetForm()
              setShowCreate(open)
            }}
          >
            <DialogTrigger asChild>
              <Button size="sm">+ Создать</Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>Создать новость</DialogTitle>
              </DialogHeader>
              {formDialog}
            </DialogContent>
          </Dialog>
        )}
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
      )}

      {loading ? (
        <div className="flex justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
        </div>
      ) : news.length === 0 ? (
        <p className="text-muted-foreground">Новостей пока нет</p>
      ) : (
        <>
          <div className="rounded-lg border bg-card">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Заголовок</TableHead>
                  <TableHead>Категория</TableHead>
                  <TableHead>Статус</TableHead>
                  <TableHead>Дата</TableHead>
                  {canManage && <TableHead>Действия</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {news.map(item => (
                  <TableRow key={item.id}>
                    <TableCell className="max-w-xs truncate font-medium">{item.title}</TableCell>
                    <TableCell>
                      {item.categoryName ? (
                        <Badge variant="outline">{item.categoryName}</Badge>
                      ) : (
                        <span className="text-sm text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <button
                        onClick={() => canManage && handleTogglePublish(item)}
                        disabled={!canManage}
                        className={`text-sm font-medium focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#568cd6] focus-visible:ring-offset-2 rounded ${
                          item.isPublished
                            ? "text-green-600"
                            : "text-muted-foreground"
                        } ${canManage ? "hover:underline cursor-pointer" : "cursor-default"}`}
                      >
                        {item.isPublished ? "Активна" : "Черновик"}
                      </button>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                    </TableCell>
                    {canManage && (
                      <TableCell>
                        <div className="flex gap-2">
                          <Dialog>
                            <DialogTrigger asChild>
                              <Button variant="ghost" size="sm" onClick={() => fillForm(item)}>
                                Ред.
                              </Button>
                            </DialogTrigger>
                            <DialogContent className="max-w-2xl">
                              <DialogHeader>
                                <DialogTitle>Редактировать новость</DialogTitle>
                              </DialogHeader>
                              {formDialog}
                            </DialogContent>
                          </Dialog>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="text-destructive hover:text-destructive"
                            onClick={() => handleDelete(item.id)}
                          >
                            Удал.
                          </Button>
                        </div>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage(p => Math.max(1, p - 1))}
              >
                ← Назад
              </Button>
              <span className="text-sm text-muted-foreground">
                {page} / {totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              >
                Вперед →
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
