"use client"

import { BookMarked } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"

interface CorrectionRulesProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

interface Rule {
  title: string
  items: string[]
}

/** Шпаргалка по работе с корректировкой: что делает каждая операция. */
const RULES: { group: string; rules: Rule[] }[] = [
  {
    group: "Общее",
    rules: [
      {
        title: "Пакет корректировки",
        items: [
          "Пакет создаётся на одну дату: неделя и день недели определяются по ней автоматически.",
          "Дату пакета можно изменить, пока он не применён, — неделя и день пересчитаются.",
          "Позиции применяются по порядку: каждая следующая видит результат предыдущей.",
          "Ошибки пакета показываются в редакторе; пока они есть, «Применить» недоступно.",
        ],
      },
      {
        title: "Занятый слот",
        items: [
          "Пару можно поставить и в занятую пару — это разрешено и не является ошибкой.",
          "При добавлении в занятую пару позиция сохраняется как замена, в примечании ставится «вм.N».",
          "При переносе пара может встать в любую пару дня, свободную или занятую.",
        ],
      },
    ],
  },
  {
    group: "Операции",
    rules: [
      {
        title: "Добавление",
        items: [
          "Новое занятие в выбранную пару дня — вторым, если пара уже занята.",
          "Указываются преподаватель и предмет из нагрузки группы.",
          "Существующее занятие при этом остаётся: добавление ничего не снимает.",
        ],
      },
      {
        title: "Снятие",
        items: [
          "Занятие убирается из пары на выбранную дату.",
          "Обычное снятие удаляет неделю из расписания; если недель не осталось — удаляется сама пара.",
          "Снятие с примечанием «сам.р.» — пара остаётся в расписании и просто помечается: это информирование студентов, а не отмена занятия.",
        ],
      },
      {
        title: "Замена",
        items: [
          "Вместо одного занятия в этой же паре ставится другое.",
          "Указывается новое занятие: преподаватель и предмет.",
          "Старое занятие снимается, новое добавляется в ту же пару.",
        ],
      },
      {
        title: "Перенос",
        items: [
          "Выберите группу — покажется карточка её слотов на этот день.",
          "Нажмите на занятие, которое переносите, или перетащите его на нужную пару.",
          "Пара назначения может быть свободной или занятой — в слоте может быть несколько занятий.",
          "В примечание автоматически подставляется «вм.N», где N — исходная пара.",
        ],
      },
    ],
  },
  {
    group: "Примечание",
    rules: [
      {
        title: "Соглашения",
        items: [
          "«сам.р.» — самостоятельная работа: пара остаётся в расписании, студенты видят бейдж и могут не приходить. Работает для любой операции.",
          "«вм.N» — вместо пары N: подставляется автоматически при переносе.",
          "Остальной текст примечания — свободный, он показывается в расписании, в ленте изменений и в уведомлениях бота.",
        ],
      },
      {
        title: "Применение и отмена",
        items: [
          "После применения пакет нельзя отредактировать.",
          "Применённый пакет можно удалить: расписание вернётся к состоянию до корректировки, записи журнала удалятся.",
          "Кнопка «Очистить применённые» убирает все применённые пакеты разом — удобно в начале нового семестра.",
        ],
      },
    ],
  },
]

/** Краткая шпаргалка по правилам корректировки, доступная со страницы пакетов. */
export function CorrectionRules({ open, onOpenChange }: CorrectionRulesProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="scroll-stable sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <BookMarked className="size-5" aria-hidden />
            Правила корректировки расписания
          </DialogTitle>
          <DialogDescription>
            Шпаргалка: что делает каждая операция и как работают примечания.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-5">
          {RULES.map((section) => (
            <section key={section.group} className="grid gap-3">
              <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
                {section.group}
              </h3>
              <div className="grid gap-3 sm:grid-cols-2">
                {section.rules.map((rule) => (
                  <div key={rule.title} className="rounded-md border p-3">
                    <h4 className="text-sm font-semibold">{rule.title}</h4>
                    <ul className="mt-1.5 grid gap-1 text-sm text-muted-foreground">
                      {rule.items.map((item) => (
                        <li key={item} className="flex gap-1.5">
                          <span aria-hidden className="text-muted-foreground">
                            •
                          </span>
                          <span>{item}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            </section>
          ))}
        </div>

        <Button variant="outline" onClick={() => onOpenChange(false)}>
          Понятно
        </Button>
      </DialogContent>
    </Dialog>
  )
}
