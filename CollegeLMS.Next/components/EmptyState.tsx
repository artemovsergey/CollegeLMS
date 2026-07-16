interface EmptyStateProps {
  message: string
  className?: string
}

export default function EmptyState({ message, className = "" }: EmptyStateProps) {
  return (
    <p className={`text-sm text-muted-foreground ${className}`}>
      {message}
    </p>
  )
}
