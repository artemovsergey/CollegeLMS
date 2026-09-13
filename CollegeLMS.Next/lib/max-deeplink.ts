export type MaxRoute =
  | "today"
  | "day"
  | "week"
  | "changes"
  | "correction"
  | "dispatcher"
  | "schedule"

export interface MaxDeepLink {
  route: MaxRoute
  date?: string
  id?: string
  groupId?: string
  teacherId?: string
  view?: "day" | "week"
}

const VALID_ROUTES: MaxRoute[] = [
  "today",
  "day",
  "week",
  "changes",
  "correction",
  "dispatcher",
  "schedule",
]

export function parseMaxDeepLink(search: string): MaxDeepLink {
  const params = new URLSearchParams(search)
  const rawRoute = params.get("route") ?? "today"
  const route = VALID_ROUTES.includes(rawRoute as MaxRoute)
    ? (rawRoute as MaxRoute)
    : "today"
  const result: MaxDeepLink = { route }

  const date = params.get("date")
  if (date) result.date = date
  const id = params.get("id")
  if (id) result.id = id
  const groupId = params.get("groupId")
  if (groupId) result.groupId = groupId
  const teacherId = params.get("teacherId")
  if (teacherId) result.teacherId = teacherId
  const view = params.get("view")
  if (view === "day" || view === "week") result.view = view

  return result
}