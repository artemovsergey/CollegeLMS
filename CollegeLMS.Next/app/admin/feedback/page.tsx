"use client"

import { useEffect, useState } from "react"
import type { Result, FeedbackListItemDto } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { Eye, ChevronLeft, ChevronRight } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"

export default function AdminFeedbackPage() {
  const { user } = useAuth()
  const [items, setItems] = useState<FeedbackListItemDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [selectedMessage, setSelectedMessage] = useState<FeedbackListItemDto | null>(null)
  const [page, setPage] = useState(1)
  const pageSize = 10
  const totalPages = Math.ceil(items.length / pageSize)
  const paginatedItems = items.slice((page - 1) * pageSize, page * pageSize)

  useEffect(() => {
    const fetch = async () => {
      setLoading(true)
      setError(null)
      try {
        const res = await api.get<Result<FeedbackListItemDto[]>>("/api/feedback")
        const body = res.data
        if (body.isSuccess && body.data) {
          setItems(body.data)
        } else {
          setError(body.errorMessage ?? "Ошибка загрузки")
        }
      } catch {
        setError("Ошибка загрузки сообщений")
      } finally {
        setLoading(false)
      }
    }
    fetch()
  }, [])

  useEffect(() => { setPage(1) }, [items.length])

  if (loading) return <LoadingSpinner className="py-20" />
  if (error) return <ErrorBanner message={error} className="m-6" />

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Обратная связь</h2>
        <Badge variant="secondary">{items.length} {items.length === 1 ? "сообщение" : (items.length >= 2 && items.length <= 4 ? "сообщения" : "сообщений")}</Badge>
      </div>

      {items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-20 text-center text-muted-foreground">
          <p className="text-lg">Нет сообщений</p>
          <p className="text-sm">Пользователи ещё не отправляли обратную связь.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Дата</TableHead>
              <TableHead>Имя</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Сообщение</TableHead>
            <TableHead></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {paginatedItems.map(item => (
              <TableRow
                key={item.id}
                className="cursor-pointer"
                onClick={() =>
                  setExpandedId(expandedId === item.id ? null : item.id)
                }
              >
                <TableCell className="whitespace-nowrap text-sm">
                  {new Date(item.createdAt).toLocaleString("ru")}
                </TableCell>
                <TableCell className="font-medium">{item.name}</TableCell>
                <TableCell className="text-muted-foreground">
                  {item.email}
                </TableCell>
                <TableCell className="max-w-md">
                  <p
                    className={
                      expandedId === item.id
                        ? "line-clamp-none"
                        : "line-clamp-2"
                    }
                  >
                    {item.message}
                  </p>
                </TableCell>
                <TableCell>
                  <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setSelectedMessage(item) }} aria-label="Просмотреть">
                    <Eye size={16} />
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
            <ChevronLeft size={16} />
          </Button>
          <span className="text-sm text-muted-foreground px-2">{page} / {totalPages}</span>
          <Button variant="outline" size="sm" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>
            <ChevronRight size={16} />
          </Button>
        </div>
      )}

      <Dialog open={!!selectedMessage} onOpenChange={(o) => !o && setSelectedMessage(null)}>
        <DialogContent className="bg-card max-w-lg">
          <DialogHeader>
            <DialogTitle>Сообщение от {selectedMessage?.name}</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-3">
            <div className="text-sm text-muted-foreground">
              <span className="font-medium text-fg">Email:</span> {selectedMessage?.email}
            </div>
            <div className="text-sm text-muted-foreground">
              <span className="font-medium text-fg">Дата:</span> {selectedMessage && new Date(selectedMessage.createdAt).toLocaleString("ru")}
            </div>
            <div className="mt-2 rounded-lg bg-muted/30 p-4 text-sm leading-relaxed text-fg whitespace-pre-wrap">
              {selectedMessage?.message}
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
