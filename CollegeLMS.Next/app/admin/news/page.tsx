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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { toast } from "sonner"
import { Pencil, Trash2, Upload, Square, ExternalLink } from "lucide-react"
import type { ImportProgressDto } from "@/types"

const PAGE_SIZE = 20

export default function AdminNewsPage() {
  const { user } = useAuth()
  const canManage = user?.role === "Admin"

  const [news, setNews] = useState<NewsResponse[]>([])
  const [categories, setCategories] = useState<NewsCategoryResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const [formTitle, setFormTitle] = useState("")
  const [formContent, setFormContent] = useState("")
  const [formImageUrl, setFormImageUrl] = useState("")
  const [formCategoryId, setFormCategoryId] = useState("")
  const [formPublished, setFormPublished] = useState(true)
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)
  const [uploading, setUploading] = useState(false)

  // Import state
  const [importId, setImportId] = useState<string | null>(null)
  const [importProgress, setImportProgress] = useState<ImportProgressDto | null>(null)
  const [importing, setImporting] = useState(false)
  const [polling, setPolling] = useState(false)

  const startImport = async () => {
    setImportProgress(null)
    setImporting(true)
    try {
      const res = await api.post<Result<string>>("/api/import/wordpress/rest")
      const body = res.data
      if (!body.isSuccess || !body.data) {
        toast.error(body.errorMessage ?? "Ошибка запуска импорта")
        setImporting(false)
        return
      }
      setImportId(body.data)
      setImporting(false)
      setPolling(true)
      pollStatus(body.data)
    } catch {
      toast.error("Ошибка запуска импорта")
      setImporting(false)
    }
  }

  const stopImport = async () => {
    if (!importId) return
    try {
      await api.post(`/api/import/wordpress/stop/${importId}`)
      toast("Импорт остановлен")
      setPolling(false)
    } catch {
      toast.error("Ошибка остановки импорта")
    }
  }

  const pollStatus = (id: string) => {
    const interval = setInterval(async () => {
      try {
        const res = await api.get<Result<ImportProgressDto>>(`/api/import/wordpress/status/${id}`)
        const body = res.data
        if (body.isSuccess && body.data) {
          setImportProgress(body.data)
          if (body.data.status === "completed" || body.data.status === "failed" || body.data.status === "cancelled") {
            clearInterval(interval)
            setPolling(false)
            setImportId(null)
            if (body.data.status === "completed") {
              toast.success("Импорт завершён")
              fetchNews()
            }
          }
        } else {
          clearInterval(interval)
          setPolling(false)
        }
      } catch {
        clearInterval(interval)
        setPolling(false)
      }
    }, 2000)
  }

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
        toast("Новость создана")
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
        toast("Новость обновлена")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления новости")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return
    try {
      await api.delete(`/api/news/${deleteId}`)
      setDeleteId(null)
      await fetchNews()
      toast("Новость удалена")
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
      toast(item.isPublished ? "Новость снята с публикации" : "Новость опубликована")
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
        <div className="flex items-center gap-2">
          {canManage && (
            <>
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
              <Button
                size="sm"
                variant="outline"
                onClick={startImport}
                disabled={importing || polling}
              >
                <Upload size={16} className="mr-1" />
                {importing ? "Запуск..." : "Импорт"}
              </Button>
              {polling && (
                <Button
                  size="sm"
                  variant="outline"
                  className="text-destructive border-destructive hover:bg-destructive/10"
                  onClick={stopImport}
                >
                  <Square size={16} className="mr-1" />
                  Остановить
                </Button>
              )}
            </>
          )}
        </div>
      </div>

      {importProgress && (
        <div className="rounded-lg border bg-card p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium">
              Импорт: {importProgress.status === "running" ? "Выполняется..." : importProgress.status === "completed" ? "Завершён" : importProgress.status === "cancelled" ? "Остановлен" : "Ошибка"}
            </span>
            {importProgress.status === "running" && (
              <div className="h-4 w-4 animate-spin rounded-full border-2 border-muted border-t-primary" />
            )}
          </div>
          {importProgress.total > 0 && (
            <div className="flex flex-col gap-1">
              <div className="flex justify-between text-sm text-muted-foreground">
                <span>Обработано: {importProgress.processed} из {importProgress.total}</span>
                <span>{Math.round((importProgress.processed / importProgress.total) * 100)}%</span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary transition-all duration-500"
                  style={{ width: `${Math.round((importProgress.processed / importProgress.total) * 100)}%` }}
                />
              </div>
            </div>
          )}
          {importProgress.result && (
            <div className="mt-2 text-sm text-muted-foreground">
              Импортировано: {importProgress.result.postsImported}, пропущено: {importProgress.result.postsSkipped}
            </div>
          )}
        </div>
      )}

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
                    <TableCell className="max-w-xs truncate font-medium">
                      <a href={`/news/${item.id}`} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1 hover:text-accent transition-colors">
                        {item.title}
                        <ExternalLink size={12} className="shrink-0 text-muted-fg" />
                      </a>
                    </TableCell>
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
                              <Button variant="ghost" size="sm" onClick={() => fillForm(item)} aria-label="Редактировать">
                                <Pencil size={16} />
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
                            onClick={() => setDeleteId(item.id)}
                            aria-label="Удалить"
                          >
                            <Trash2 size={16} />
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

      <AlertDialog open={!!deleteId} onOpenChange={() => setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить новость?</AlertDialogTitle>
            <AlertDialogDescription>Это действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">Удалить</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
