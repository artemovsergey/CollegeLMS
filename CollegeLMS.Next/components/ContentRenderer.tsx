export default function ContentRenderer({
  content,
  className = "",
}: {
  content: string
  className?: string
}) {
  const sanitized = content
    .replace(/<script[\s\S]*?<\/script>/gi, "")
    .replace(/\s+on\w+\s*=\s*(?:"[^"]*"|'[^']*'|[^\s>]+)/gi, "")
    .replace(/style\s*=\s*"[^"]*"/gi, "")

  return (
    <div
      className={`docs-content ${className}`}
      dangerouslySetInnerHTML={{ __html: sanitized }}
    />
  )
}
