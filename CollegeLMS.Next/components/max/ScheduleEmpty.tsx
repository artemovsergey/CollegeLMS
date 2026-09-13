"use client"

import { Typography } from "@maxhub/max-ui"

export default function ScheduleEmpty() {
  return (
    <div className="max-app__state">
      <Typography.Title>Пар нет</Typography.Title>
      <Typography.Body className="max-app__muted">
        На этот день занятий нет
      </Typography.Body>
    </div>
  )
}