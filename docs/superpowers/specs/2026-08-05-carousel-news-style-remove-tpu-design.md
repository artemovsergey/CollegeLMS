# Дизайн: карусель в стиле новости + удаление TPU-дизайна

Дата: 2026-08-05
Статус: реализовано

## Контекст

Слайды карусели на главной (`Carousel.tsx`) оформлены иначе, чем «постер» на странице новости (`news/[id]/page.tsx`). Требуется единое оформление: слайд карусели = тот же постер, что в шапке новости (синий блок + фото), плюс логотип. «Это одно целое».

Параллельно выяснилось: дизайн-пресет TPU больше не актуален. Классы TPU-стилей (`first-screen-tpu`, `card-slider-tpu`, `text-tpu-text`, `tpu-border`) **не определены ни в одном CSS** — TPU-ветки рендерят неслоенные элементы. Убираем всё, связанное с TPU.

## 1. Карусель — слайд в стиле новости (Carousel.tsx)

Слайд (сейчас: фото на весь слайд + тёмный градиент + текст снизу + логотип в углу) переоформляется в стиль шапки новости (`news/[id]/page.tsx:154-182`):

- Карточка в 2 колонки (`grid grid-cols-1 lg:grid-cols-3` как в новости):
  - **Левая колонка** (`lg:col-span-1`, фон `bg-primary`, текст `text-primary-foreground`, паддинги `p-6 sm:p-10`):
    - Дата `new Date(item.publishedAt).toLocaleDateString("ru-RU")` + `· categoryName` (если есть)
    - Заголовок `h2` (line-clamp-2, bold)
    - Анонс — текст без HTML (`content.replace(/<[^>]*>/g, " ")`), line-clamp-3
    - Кнопка «Подробнее» (white/20 pill, как сейчас)
  - **Правая колонка** (`lg:col-span-2`): фото `Image fill object-cover`; если `imageUrl` нет — градиентная заглушка (существующая)
- **Логотип** — в левом верхнем углу синего блока: `<Image src="/logo.svg">` (как сейчас на слайде, строка 100-110), размер до 100px высотой
- На мобильных: `grid-cols-1` — фото сверху (h-48/56), синий блок под ним; логотип в углу блока
- Высота секции сохраняется: `h-[400px] md:h-[550px]`
- Стрелки, точки-индикаторы, автопрокрутка (5 с, пауза при hover) — без изменений
- Ссылка ведёт на `/news/{id}` (уже есть)

## 2. Удаление TPU

### Удалить файлы (9 компонентов)

- `components/HomeTPU.tsx`
- `components/CarouselTPU.tsx`
- `components/SpecialtiesSectionTPU.tsx`
- `components/AdmissionSectionTPU.tsx`
- `components/NewsSectionTPU.tsx`
- `components/StatisticsSectionTPU.tsx`
- `components/FAQSectionTPU.tsx`
- `components/FeedbackFormTPU.tsx`
- `components/BreadcrumbsTPU.tsx`

### Правки кода

- `lib/design-provider.tsx`:
  - `DesignPreset = "default"` (только)
  - `DESIGN_PRESETS = ["default"]`
  - `DESIGN_LABELS` — только `default`
- `app/(public)/page.tsx`: убрать `import HomeTPU` и ветку `if (design === "tpu") return <HomeTPU />`
- `components/SectionPage.tsx`: убрать `import BreadcrumbsTPU`, ветки `design === "tpu"` для breadcrumbs и классов пустого состояния (вернуть default-классы)
- `components/Carousel.tsx`: убрать `import CarouselTPU` и switch `design === "tpu"` — компонент становится обычным default-рендером

## 3. Проверка

- `npm run build` — сборка без ошибок
- `npm run lint` — без ошибок
- Ручная проверка: главная — слайды в стиле новости с логотипом; нет упоминаний tpu в `app/` и `components/`
- `rg -ri "tpu" CollegeLMS.Next/app CollegeLMS.Next/components CollegeLMS.Next/lib` → пусто (кроме `contacts/page.tsx` — это URL карты, не дизайн)

## Вне скоупа

- Backend не меняется
- `contacts/page.tsx` (URL Google Maps содержит «ГБПОУ Ставропольский колледж связи...», совпадение с tpu — случайное) — не трогаем
- Классы `text-tpu-text`/`tpu-border` не существуют в CSS — удаляются вместе с ветками в `SectionPage.tsx`
