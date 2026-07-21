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
      className={`docs-content ${className} [&_img]:mt-4 [&_img:first-child]:mt-0 [&_img:not(:first-child)]:mt-6 [&_img]:rounded-md [&_img]:w-full`}
      dangerouslySetInnerHTML={{ __html: sanitized }}
    />
  )
}
