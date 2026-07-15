import { AlertCircle } from "lucide-react"

interface ErrorBannerProps {
  message: string
  className?: string
}

export default function ErrorBanner({ message, className = "" }: ErrorBannerProps) {
  return (
    <div className={`flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive ${className}`}>
      <AlertCircle className="size-4 shrink-0" />
      {message}
    </div>
  )
}
