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

export default function FAQSectionTPU() {
  const [openIndex, setOpenIndex] = useState<number | null>(null)

  return (
    <div className="app-section">
      <div className="mx-auto max-w-3xl px-4 lg:px-8">
        <h2 className="app-section__title">Часто задаваемые вопросы</h2>
        <p className="app-section__subtitle">Ответы на самые популярные вопросы о поступлении и обучении</p>

        <div className="space-y-3">
          {faqs.map((faq, i) => (
            <div
              key={i}
              className="overflow-hidden rounded-lg border border-tpu-border bg-white"
            >
              <button
                onClick={() => setOpenIndex(openIndex === i ? null : i)}
                className="flex w-full items-center justify-between px-6 py-5 text-left text-sm font-medium text-tpu-text transition-colors hover:bg-tpu-gray-50"
              >
                <span>{faq.q}</span>
                <ChevronDown
                  size={16}
                  className={`shrink-0 text-tpu-text-muted transition-transform duration-200 ${
                    openIndex === i ? "rotate-180" : ""
                  }`}
                />
              </button>
              {openIndex === i && (
                <div className="border-t border-tpu-border px-6 py-5 text-sm leading-relaxed text-tpu-text-muted">
                  {faq.a}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
