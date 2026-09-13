"use client"

import { Button, Typography } from "@maxhub/max-ui"

export default function ScheduleError({
  message,
  onRetry,
}: {
  message: string
  onRetry?: () => void
}) {
  return (
    <div className="max-app__state">
      <Typography.Body>{message}</Typography.Body>
      {onRetry ? (
        <Button size="small" onClick={onRetry}>
          Повторить
        </Button>
      ) : null}
    </div>
  )
}