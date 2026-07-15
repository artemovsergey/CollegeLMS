"use client"

import { useState } from "react"
import { ChevronDown } from "lucide-react"

const faqs = [
  {
    q: "Какие документы нужны для поступления?",
    a: "Паспорт (копия), аттестат об основном общем или среднем общем образовании (оригинал или копия), СНИЛС, 4 фотографии 3×4, медицинская справка (для специальностей с особыми требованиями).",
  },
  {
    q: "Нужно ли сдавать вступительные экзамены?",
    a: "Приём на программы СПО проводится по конкурсу аттестатов. Вступительные испытания не предусмотрены. Средний балл аттестата — единственный критерий зачисления.",
  },
  {
    q: "Предоставляется ли отсрочка от армии?",
    a: "Да, всем студентам очной формы обучения на время освоения образовательной программы предоставляется отсрочка от призыва на военную службу в соответствии с Федеральным законом.",
  },
  {
    q: "Есть ли общежитие?",
    a: "Иногородним студентам при наличии свободных мест предоставляется общежитие. Количество мест ограничено, распределение производится при зачислении.",
  },
  {
    q: "Можно ли перевестись из другого колледжа?",
    a: "Да, перевод возможен при наличии свободных мест и отсутствии академической задолженности. Перечень необходимых документов можно уточнить в учебной части.",
  },
  {
    q: "Какая стипендия и как её получить?",
    a: "Государственная академическая стипендия назначается студентам очной формы обучения, сдавшим сессию на «хорошо» и «отлично». Размер стипендии устанавливается ежегодно.",
  },
  {
    q: "Есть ли бюджетные места?",
    a: "Да, приём осуществляется за счёт бюджетных ассигнований бюджета Ставропольского края. Количество мест утверждается ежегодно Министерством энергетики, промышленности и связи СК.",
  },
]

export default function FAQSection() {
  const [openIndex, setOpenIndex] = useState<number | null>(null)

  return (
    <section className="py-16">
      <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Часто задаваемые вопросы</h2>
        <div className="space-y-2">
          {faqs.map((faq, i) => (
            <div
              key={i}
              className="overflow-hidden rounded-lg border border-border bg-card"
            >
              <button
                onClick={() => setOpenIndex(openIndex === i ? null : i)}
                className="flex w-full items-center justify-between px-5 py-4 text-left text-sm font-medium text-primary transition-colors hover:bg-muted/50"
              >
                <span>{faq.q}</span>
                <ChevronDown
                  size={16}
                  className={`shrink-0 transition-transform duration-200 ${
                    openIndex === i ? "rotate-180" : ""
                  }`}
                />
              </button>
              {openIndex === i && (
                <div className="border-t border-border px-5 py-4 text-sm leading-relaxed text-muted-foreground">
                  {faq.a}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
