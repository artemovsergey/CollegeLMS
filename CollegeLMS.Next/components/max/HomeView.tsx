"use client"

import { Typography } from "@maxhub/max-ui"

export default function HomeView() {
  return (
    <div className="max-app__page">
      <Typography.Title>Главная</Typography.Title>
      <Typography.Body className="max-app__muted">
        Загружается…
      </Typography.Body>
    </div>
  )
}