import { redirect } from "next/navigation"

export const metadata = {
  title: "Расписание",
}

export default function MaxHomePage({
  searchParams,
}: {
  searchParams: { [key: string]: string | string[] | undefined }
}) {
  const route = typeof searchParams.route === "string" ? searchParams.route : ""
  const dispatchTarget =
    route === "changes" || route === "correction"
      ? "/max/changes"
      : route === "dispatcher"
        ? "/max/dispatcher"
        : "/max/schedule"

  const params = new URLSearchParams()
  if (route) params.set("route", route)
  if (typeof searchParams.date === "string") params.set("date", searchParams.date)
  if (typeof searchParams.day === "string") params.set("day", searchParams.day)
  if (typeof searchParams.id === "string") params.set("id", searchParams.id)
  if (typeof searchParams.view === "string") params.set("view", searchParams.view)
  if (typeof searchParams.groupId === "string")
    params.set("groupId", searchParams.groupId)
  if (typeof searchParams.teacherId === "string")
    params.set("teacherId", searchParams.teacherId)
  const qs = params.toString()
  redirect(`${dispatchTarget}${qs ? `?${qs}` : ""}`)
}