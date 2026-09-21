// Установка: скопировать в %USERPROFILE%\.config\opencode\plugin\ (Linux/macOS: ~/.config/opencode/plugin/)
// и добавить "./plugin/auto-answer.ts" в массив "plugin" глобального opencode.jsonc.
// Автоответ рекомендованным (первым) вариантом через 5 минут после появления вопроса.

import type { Plugin } from "@opencode-ai/plugin"

const TIMEOUT_MS = 5 * 60 * 1000

export default (async ({ client }) => {
  const timers = new Map<string, ReturnType<typeof setTimeout>>()

  return {
    event: async ({ event }) => {
      if (event.type === "question.asked") {
        const req = event.properties as {
          id: string
          questions: { options?: { label: string }[] }[]
        }
        const timer = setTimeout(() => {
          timers.delete(req.id)
          const answers = req.questions.map((q) => (q.options?.[0] ? [q.options[0].label] : []))
          client.question
            .reply({ requestID: req.id, answers })
            .catch(() => client.question.reject({ requestID: req.id }).catch(() => {}))
        }, TIMEOUT_MS)
        timers.set(req.id, timer)
      }
      if (event.type === "question.replied" || event.type === "question.rejected") {
        const id = (event.properties as { requestID?: string }).requestID
        const t = id ? timers.get(id) : undefined
        if (t) {
          clearTimeout(t)
          timers.delete(id!)
        }
      }
    },
  }
}) satisfies Plugin
