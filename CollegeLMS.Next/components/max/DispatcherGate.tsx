"use client"

import { useState } from "react"
import { ShieldCheck } from "lucide-react"
import { Button, Input, MaxUI, Typography } from "@maxhub/max-ui"
import { dispatcherLogin, notifyDispatcherSession } from "@/api/dispatcher"
import { extractErrorMessage } from "@/lib/utils"

export default function DispatcherGate({
  onSuccess,
}: {
  onSuccess: () => void
}) {
  const [password, setPassword] = useState("")
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!password || busy) return
    setBusy(true)
    setError(null)
    try {
      await dispatcherLogin(password)
      notifyDispatcherSession()
      onSuccess()
    } catch (err) {
      const message = extractErrorMessage(err)
      setError(
        message === "Слишком много попыток. Повторите позже"
          ? "Слишком много попыток, повторите позже"
          : message ?? "Неверный пароль",
      )
    } finally {
      setBusy(false)
    }
  }

  return (
    <MaxUI>
      <main className="max-app__page max-app__login-prompt">
        <ShieldCheck size={32} className="max-app__state-icon" aria-hidden />
        <Typography.Title>Доступ диспетчера</Typography.Title>
        <Typography.Body className="max-app__muted">
          Введите пароль, чтобы управлять корректировками
        </Typography.Body>
        <Input
          type="password"
          aria-label="Пароль диспетчера"
          placeholder="Пароль"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") void submit()
          }}
        />
        {error ? (
          <Typography.Body className="max-app__error">{error}</Typography.Body>
        ) : null}
        <Button
          stretched
          loading={busy}
          disabled={!password}
          onClick={() => void submit()}
        >
          Войти
        </Button>
      </main>
    </MaxUI>
  )
}