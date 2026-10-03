import { cn } from "@/lib/utils"

/**
 * Типографика по шкале §4. Один источник размеров заголовков на весь продукт.
 *
 * Шкала §4 описывает шесть ролей: заголовок страницы, раздел, подзаголовок,
 * основной текст, вторичный текст, подпись. Пока размер задавался классами в
 * каждой странице, страницы разъехались: на заголовок страницы в разных
 * местах стояло `text-2xl`, `text-xl` и `text-lg`, а `text-3xl font-bold` из
 * шкалы не использовался ни разу — вместо него встречался на процентах
 * результата теста.
 *
 * Роли, а не размеры: правильный размер зависит от смысла элемента, а не от
 * того, на какой он странице. Поэтому `PageTitle` всегда `text-3xl font-bold`
 * независимо от поверхности, а `CardTitle` — `text-base font-semibold`, потому
 * что заголовок карточки это подзаголовок, а не раздел.
 */

export function PageTitle({
  className,
  ...props
}: React.ComponentProps<"h1">) {
  return (
    <h1
      className={cn("text-3xl font-bold tracking-tight text-fg", className)}
      {...props}
    />
  )
}

export function SectionTitle({
  className,
  ...props
}: React.ComponentProps<"h2">) {
  return (
    <h2
      className={cn("text-2xl font-semibold tracking-tight text-fg", className)}
      {...props}
    />
  )
}

export function SubTitle({
  className,
  ...props
}: React.ComponentProps<"h3">) {
  return (
    <h3 className={cn("text-xl font-semibold text-fg", className)} {...props} />
  )
}

/** Заголовок карточки или панели: по шкале это подпись, а не раздел. */
export function CardTitle({
  className,
  ...props
}: React.ComponentProps<"h3">) {
  return (
    <h3 className={cn("text-base font-semibold text-fg", className)} {...props} />
  )
}

/** Подпись поля, метка в таблице, сноска. */
export function Caption({
  className,
  ...props
}: React.ComponentProps<"span">) {
  return (
    <span className={cn("text-xs text-muted-fg", className)} {...props} />
  )
}

/** Вторичный текст под значением или описанием блока. */
export function SecondaryText({
  className,
  ...props
}: React.ComponentProps<"p">) {
  return (
    <p className={cn("text-sm text-muted-fg", className)} {...props} />
  )
}