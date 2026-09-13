"use client"

import { useCallback, useEffect, useState } from "react"
import Link from "next/link"
import { Bell, Info } from "lucide-react"
import {
  Button,
  CellHeader,
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import {
  getNotificationSettings,
  updateNotificationSettings,
} from "@/api/notifications"
import type {
  NotificationSettingsDto,
  NotificationSettingsInput,
} from "@/api/notifications"
import { useMaxContext } from "@/lib/max-context"
import ScheduleError from "@/components/max/ScheduleError"

const WEEKDAYS = [
  { value: 1, label: "Пн" },
  { value: 2, label: "Вт" },
  { value: 3, label: "Ср" },
  { value: 4, label: "Чт" },
  { value: 5, label: "Пт" },
  { value: 6, label: "Сб" },
  { value: 7, label: "Вс" },
]

function timeOptions(): string[] {
  const result: string[] = []
  for (let minutes = 7 * 60 + 30; minutes <= 8 * 60 + 30; minutes += 5) {
    result.push(
      `${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(
        minutes % 60,
      ).padStart(2, "0")}`,
    )
  }
  return result
}

const TIMES = timeOptions()

export default function SettingsView() {
  const { isAuthed } = useMaxContext()
  const [settings, setSettings] = useState<NotificationSettingsDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setSettings(await getNotificationSettings())
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Не удалось загрузить настройки",
      )
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  if (!isAuthed) {
    return (
      <MaxUI>
        <main className="max-app__page max-app__login-prompt">
          <Bell size={32} className="max-app__state-icon" aria-hidden />
          <Typography.Title>Войдите, чтобы настроить уведомления</Typography.Title>
          <Link href="/login" passHref>
            <Button>Войти</Button>
          </Link>
        </main>
      </MaxUI>
    )
  }

  const apply = (patch: Partial<NotificationSettingsInput>) => {
    setSettings((prev) => (prev ? { ...prev, ...patch } : prev))
  }

  const toggleDay = (day: number) => {
    if (!settings) return
    const days = settings.days.includes(day)
      ? settings.days.filter((d) => d !== day)
      : [...settings.days, day]
    apply({ days })
  }

  const save = async () => {
    if (!settings) return
    setSaving(true)
    setSaved(null)
    setError(null)
    try {
      const updated = await updateNotificationSettings({
        enabled: settings.enabled,
        time: settings.time,
        days: settings.days,
      })
      setSettings(updated)
      if (updated.nextNotifyAt) {
        const next = new Date(updated.nextNotifyAt)
        setSaved(
          `Сохранено. Время ${updated.time}, следующие — ${next.toLocaleString("ru-RU", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
          })} (МСК)`,
        )
      } else {
        setSaved("Сохранено")
      }
    } catch {
      setError("Не удалось сохранить. Повторите")
    } finally {
      setSaving(false)
    }
  }

  return (
    <MaxUI>
      <main className="max-app__page">
        <header className="max-app__page-title">
          <Typography.Title>Уведомления</Typography.Title>
        </header>

        {loading ? (
          <div className="max-app__state">
            <Spinner size={24} />
          </div>
        ) : error && !settings ? (
          <ScheduleError message={error} onRetry={() => void load()} />
        ) : settings ? (
          <>
            <CellList mode="island" header={<CellHeader titleStyle="normal">Дайджест</CellHeader>}>
              <CellSimple
                separator
                title="Ежедневный дайджест"
                subtitle="Пар за день и незаметка о замене"
                after={
                  <span className="max-app__switch">
                    <input
                      type="checkbox"
                      role="switch"
                      aria-label="Ежедневный дайджест"
                      checked={settings.enabled}
                      onChange={(e) => apply({ enabled: e.target.checked })}
                    />
                  </span>
                }
              />
              <CellSimple
                separator
                title="Время"
                subtitle="Когда приходит дайджест"
                after={
                  <select
                    aria-label="Время дайджеста"
                    value={settings.time}
                    onChange={(e) =>
                      apply({ time: e.target.value })
                    }
                    disabled={!settings.enabled}
                  >
                    {TIMES.map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </select>
                }
              />
            </CellList>

            <CellList mode="island" header={<CellHeader titleStyle="normal">Дни недели</CellHeader>}>
              <div className="max-app__days-row">
                {WEEKDAYS.map((day) => {
                  const active = settings.days.includes(day.value)
                  return (
                    <label key={day.value} className="max-app__day-toggle">
                      <input
                        type="checkbox"
                        checked={active}
                        onChange={() => toggleDay(day.value)}
                      />
                      <span
                        className={`max-app__day-toggle-label ${
                          active ? "max-app__day-toggle-label--on" : ""
                        }`}
                      >
                        {day.label}
                      </span>
                    </label>
                  )
                })}
              </div>
            </CellList>

            <div className="max-app__note max-app__hint">
              <Info size={14} aria-hidden /> Уведомления об изменениях приходят
              сразу, независимо от времени дайджеста
            </div>

            {error ? (
              <Typography.Body className="max-app__error">
                {error}
              </Typography.Body>
            ) : null}
            {saved ? (
              <div className="max-app__success-box">{saved}</div>
            ) : null}

            <Button stretched loading={saving} onClick={() => void save()}>
              Сохранить
            </Button>
          </>
        ) : null}
      </main>
    </MaxUI>
  )
}