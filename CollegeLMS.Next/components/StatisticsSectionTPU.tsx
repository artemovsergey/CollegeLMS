"use client"

const statsData = [
  { value: "1", suffix: "", label: "Место в Ставропольском крае среди колледжей связи" },
  { value: "14800+", suffix: "", label: "Выпускников за 30 лет" },
  { value: "6", suffix: "", label: "Специальностей и профессий" },
  { value: "500+", suffix: "", label: "Студентов очной формы" },
  { value: "50+", suffix: "", label: "Преподавателей и сотрудников" },
  { value: "15+", suffix: "", label: "Предприятий-партнёров" },
]

export default function StatisticsSectionTPU() {
  return (
    <div className="app-section app-section--alt">
      <div className="mx-auto max-w-7xl px-4 lg:px-8">
        <h2 className="app-section__title">Ставропольский колледж связи сегодня</h2>
        <p className="app-section__subtitle">
          Ключевые показатели образовательной деятельности
        </p>
        <div className="stats-tpu">
          {statsData.map((s) => (
            <div key={s.label} className="stat-item">
              <div className="stat-item__number">
                {s.value}{s.suffix}
              </div>
              <div className="stat-item__label">{s.label}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
