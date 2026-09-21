export type MaxRoute = "today" | "day" | "week" | "changes" | "schedule"

export interface MaxDeepLink {
  route: MaxRoute
  date?: string
  id?: string
  groupId?: string
  teacherId?: string
  view?: "day" | "week"
}

const VALID_ROUTES: MaxRoute[] = ["today", "day", "week", "changes", "schedule"]

/** payload кнопки open_app: `{route}[-yyyy-MM-dd][-g-<guid>|-t-<guid>]` */
const START_PARAM_PATTERN =
  /^(today|day|week|changes|schedule)(?:-(\d{4}-\d{2}-\d{2}))?(?:-([gt])-([0-9a-fA-F-]{36}))?$/

/** Разбирает deep-link из query-параметров (обычные https-ссылки из уведомлений). */
export function parseMaxDeepLink(search: string): MaxDeepLink {
  const params = new URLSearchParams(search)
  const rawRoute = params.get("route") ?? "today"
  const route = (VALID_ROUTES as string[]).includes(rawRoute)
    ? (rawRoute as MaxRoute)
    : "today"
  const view = params.get("view")

  return {
    route,
    date: params.get("date") ?? params.get("day") ?? undefined,
    id: params.get("id") ?? undefined,
    groupId: params.get("groupId") ?? undefined,
    teacherId: params.get("teacherId") ?? undefined,
    view: view === "day" || view === "week" ? view : undefined,
  }
}

/** Разбирает payload кнопки open_app (start_param в MAX Bridge). */
export function parseStartParam(
  startParam: string | null | undefined
): MaxDeepLink | null {
  if (!startParam) return null

  const match = START_PARAM_PATTERN.exec(startParam)
  if (!match) return null

  const [, route, date, entityType, entityId] = match

  return {
    route: route as MaxRoute,
    date: date ?? undefined,
    groupId: entityType === "g" ? entityId : undefined,
    teacherId: entityType === "t" ? entityId : undefined,
  }
}

/** start_param (кнопка в боте) приоритетнее query-параметров ссылки. */
export function resolveMaxDeepLink(
  search: string,
  startParam?: string | null
): MaxDeepLink {
  const fromQuery = parseMaxDeepLink(search)
  const fromStart = parseStartParam(startParam)
  if (!fromStart) return fromQuery

  return {
    ...fromQuery,
    ...fromStart,
    id: fromQuery.id,
    view: fromQuery.view,
  }
}
