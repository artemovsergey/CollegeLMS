"use client"

import { useEffect, useRef } from "react"
import { cn } from "@/lib/utils"
import { X } from "lucide-react"

interface NativeDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  className?: string
  children: React.ReactNode
}

export function NativeDialog({
  open,
  onOpenChange,
  className,
  children,
}: NativeDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    if (open && !dialog.open) {
      dialog.showModal()
    } else if (!open && dialog.open) {
      dialog.close()
    }
  }, [open])

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    const handleClose = () => onOpenChange(false)
    dialog.addEventListener("close", handleClose)

    const handleBackdropClick = (e: MouseEvent) => {
      if (e.target === dialog) onOpenChange(false)
    }
    dialog.addEventListener("click", handleBackdropClick)

    return () => {
      dialog.removeEventListener("close", handleClose)
      dialog.removeEventListener("click", handleBackdropClick)
    }
  }, [onOpenChange])

  return (
    <dialog
      ref={dialogRef}
      className={cn(
        "backdrop:bg-black/50 rounded-lg border bg-background p-0 shadow-lg",
        "open:animate-in open:fade-in-0 open:zoom-in-95",
        "closed:animate-out closed:fade-out-0 closed:zoom-out-95",
        className
      )}
    >
      <div className="relative">{children}</div>
    </dialog>
  )
}

export function NativeDialogHeader({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("flex flex-col space-y-1.5 p-6 pb-0", className)}
      {...props}
    />
  )
}

export function NativeDialogTitle({
  className,
  ...props
}: React.HTMLAttributes<HTMLHeadingElement>) {
  return (
    <h2
      className={cn("text-lg font-semibold leading-none tracking-tight", className)}
      {...props}
    />
  )
}

export function NativeDialogDescription({
  className,
  ...props
}: React.HTMLAttributes<HTMLParagraphElement>) {
  return (
    <p
      className={cn("text-sm text-muted-foreground", className)}
      {...props}
    />
  )
}

export function NativeDialogFooter({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("flex flex-col-reverse p-6 pt-4 sm:flex-row sm:justify-end sm:space-x-2", className)}
      {...props}
    />
  )
}

export function NativeDialogClose({
  className,
  onClick,
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      type="button"
      className={cn(
        "absolute right-4 top-4 rounded-sm opacity-70 ring-offset-background transition-opacity hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
        className
      )}
      onClick={onClick}
      {...props}
    >
      <X className="size-4" />
      <span className="sr-only">Закрыть</span>
    </button>
  )
}
