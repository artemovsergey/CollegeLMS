import { cn } from "@/lib/utils"
import { PageTitle, SecondaryText } from "@/components/ui/heading"

/**
 * Контейнер страницы. Одна ширина и один боковой отступ на поверхность.
 *
 * На публичном сайте было пять ширин (`max-w-7xl`, `max-w-4xl`, `max-w-3xl`,
 * `max-w-2xl` и `max-w-lg`) и четыре рецепта горизонтального поля, в LMS — четыре
 * соглашения о контейнере, из которых два не имели адаптивных отступов вовсе:
 * на 320px оставалось 272px полезной ширины. Читалось это не как расхождение,
 * а как разные разделы, поэтому никто не заметил.
 *
 * `density` различает поверхности намеренно и только числом: просторная
 * публичная и плотная MAX. Внутри одной поверхности значение не выбирается.
 */

export type PageDensity = "comfortable" | "compact"

const WIDTH: Record<PageDensity, string> = {
  comfortable: "max-w-7xl",
  compact: "max-w-5xl",
}

const PADDING: Record<PageDensity, string> = {
  comfortable: "px-4 sm:px-6 lg:px-8",
  // На 320px боковые 24px съедали бы 48px из 320 — восьмую часть экрана.
  compact: "px-4 py-6 sm:px-6 lg:px-8",
}

const VERTICAL: Record<PageDensity, string> = {
  comfortable: "py-16",
  compact: "py-6",
}

interface ContainerProps {
  density?: PageDensity
  className?: string
  children: React.ReactNode
}

export function Container({
  density = "comfortable",
  className,
  children,
}: ContainerProps) {
  return (
    <div className={cn("mx-auto w-full", WIDTH[density], PADDING[density], className)}>
      {children}
    </div>
  )
}

interface PageShellProps extends ContainerProps {
  /** Один колонтитур страницы: заголовок, пояснение, действия. */
  title?: React.ReactNode
  description?: React.ReactNode
  /** Действия справа от заголовка: кнопки, фильтры. */
  actions?: React.ReactNode
  /** Иконка слева от заголовка, размер h-6 (§6.1). */
  icon?: React.ReactNode
  /** Подсчёт или метка рядом с заголовком. */
  meta?: React.ReactNode
}

/**
 * Страница целиком: контейнер, шапка, содержимое.
 *
 * Раньше шапку собирали вручную тремя способами — `header flex-col gap-1` с
 * `h1`, `div flex items-center gap-2` с иконкой и `h2`, и заголовок при этом
 * оказывался то `<h1>`, то `<h2>`. Теперь уровень заголовка задаёт компонент,
 * а раскладка шапки одинакова на всех поверхностях и перестаёт зависеть от
 * того, писал её автор страницы или кто-то до него.
 */
export function PageShell({
  density = "comfortable",
  className,
  children,
  title,
  description,
  actions,
  icon,
  meta,
}: PageShellProps) {
  const hasHeader = title != null || description != null || actions != null
  return (
    <Container density={density} className={className}>
      {hasHeader && (
        <header
          className={cn(
            "flex flex-col gap-4",
            (actions != null || meta != null) &&
              "sm:flex-row sm:items-start sm:justify-between sm:gap-6",
          )}
        >
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              {icon}
              {typeof title === "string" ? (
                <PageTitle>{title}</PageTitle>
              ) : (
                title
              )}
              {meta}
            </div>
            {description != null && (
              <SecondaryText className="mt-1">{description}</SecondaryText>
            )}
          </div>
          {actions != null && (
            // На 320px кнопки встают под заголовок, а не сталкиваются с ним.
            <div className="flex flex-wrap items-center gap-2">{actions}</div>
          )}
        </header>
      )}
      <div className={cn(hasHeader && VERTICAL[density])}>{children}</div>
    </Container>
  )
}