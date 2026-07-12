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

export default function AdminFeedbackPage() {
  const { user } = useAuth()
  const [items, setItems] = useState<FeedbackListItemDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [expandedId, setExpandedId] = useState<string | null>(null)

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

  if (loading) return <LoadingSpinner className="py-20" />
  if (error) return <ErrorBanner message={error} className="m-6" />

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Обратная связь</h2>
        <Badge variant="secondary">{items.length} сообщений</Badge>
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
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map(item => (
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
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  )
}
