"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, CourseDocumentResponse } from "@/types"
import api from "@/lib/api"
import { uploadCourseDocument, downloadCourseDocument, deleteCourseDocument } from "@/lib/api"
import { Button } from "@/components/ui/button"
import EmptyState from "@/components/EmptyState"
import LoadingSpinner from "@/components/LoadingSpinner"
import { toast } from "sonner"
import { FileText, UploadCloud, Trash2 } from "lucide-react"
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

const MAX_SIZE = 50 * 1024 * 1024

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} Б`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КБ`
  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`
}

export default function DocumentsTab({
  courseId,
  canManage,
}: {
  courseId: string
  canManage: boolean
}) {
  const [documents, setDocuments] = useState<CourseDocumentResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)
  const [dragging, setDragging] = useState(false)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)

  const fetchDocuments = useCallback(async () => {
    try {
      const res = await api.get<Result<CourseDocumentResponse[]>>(`/api/courses/${courseId}/documents`)
      if (res.data.isSuccess && res.data.data) setDocuments(res.data.data)
    } catch {
      // ignore
    } finally {
      setLoading(false)
    }
  }, [courseId])

  useEffect(() => {
    fetchDocuments()
  }, [fetchDocuments])

  const uploadFile = async (file: File) => {
    if (file.size > MAX_SIZE) {
      toast.error("Файл больше 50 МБ")
      return
    }
    setUploading(true)
    try {
      await uploadCourseDocument(courseId, file)
      toast.success("Документ загружен")
      await fetchDocuments()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ошибка загрузки документа")
    } finally {
      setUploading(false)
    }
  }

  const handlePick = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) uploadFile(file)
    e.target.value = ""
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setDeleting(true)
    try {
      await deleteCourseDocument(courseId, deleteId)
      toast.success("Документ удалён")
      setDeleteId(null)
      await fetchDocuments()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Ошибка удаления")
    } finally {
      setDeleting(false)
    }
  }

  if (loading) return <LoadingSpinner size="lg" className="py-10" />

  return (
    <div className="flex flex-col gap-4">
      {canManage && (
        <div
          onDragOver={e => { e.preventDefault(); setDragging(true) }}
          onDragLeave={() => setDragging(false)}
          onDrop={e => {
            e.preventDefault()
            setDragging(false)
            const file = e.dataTransfer.files?.[0]
            if (file) uploadFile(file)
          }}
          className={`flex flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed p-6 transition-colors ${
            dragging ? "border-primary bg-primary/5" : "border-muted"
          }`}
        >
          <UploadCloud className="size-6 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">
            Перетащите файл сюда или выберите через кнопку
          </p>
          <label>
            <input type="file" className="sr-only" onChange={handlePick} disabled={uploading} />
            <Button type="button" asChild disabled={uploading} className="pointer-events-none">
              <span>{uploading ? "Загрузка..." : "Загрузить документ"}</span>
            </Button>
          </label>
        </div>
      )}

      {documents.length === 0 ? (
        <EmptyState message="Документы ещё не загружены" />
      ) : (
        <div className="rounded-lg border bg-card divide-y">
          {documents.map(d => (
            <div key={d.id} className="flex items-center justify-between gap-3 p-4">
              <button
                type="button"
                className="flex items-center gap-3 min-w-0 text-left hover:text-primary transition-colors"
                onClick={() => downloadCourseDocument(courseId, d.id, d.fileName)}
              >
                <FileText className="size-5 shrink-0 text-muted-foreground" />
                <div className="flex flex-col gap-0.5 min-w-0">
                  <span className="font-medium truncate">{d.fileName}</span>
                  <span className="text-xs text-muted-foreground">
                    {formatSize(d.sizeBytes)} · {new Date(d.createdAt).toLocaleDateString("ru-RU")}
                  </span>
                </div>
              </button>
              {canManage && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-muted-foreground hover:text-fg shrink-0"
                  onClick={() => setDeleteId(d.id)}
                >
                  <Trash2 className="size-4" />
                </Button>
              )}
            </div>
          ))}
        </div>
      )}

      <AlertDialog open={deleteId !== null} onOpenChange={o => !o && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить документ?</AlertDialogTitle>
            <AlertDialogDescription>Действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={deleting} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              {deleting ? "Удаление..." : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
