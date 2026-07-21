# Feedback v3 — UI/UX доработки

## A1: Header scroll behavior

Первая строка (соцсети + профиль) скрывается при скролле вниз (`scrollY > 0`), вторая строка (логотип + навигация) остаётся закреплённой.

**Реализация:** `useEffect` с `scroll` listener в `Header.tsx`. При `scrollY > 0` — добавляем `-translate-y-full transition-transform duration-300` на row 1. Row 2 остаётся `sticky top-0`.

## A2: Cookie consent

Баннер внизу страницы: «Продолжая использовать сайт, вы соглашаетесь на обработку файлов cookie» + кнопка «Принять». Состояние хранится в `localStorage("cookie-consent")`.

**Компонент:** `components/CookieConsent.tsx` — `"use client"`, проверяет `localStorage`, если нет — показывает баннер. Добавлен в корневой `layout.tsx` до `ThemeSwitcher`.

## A3: Contacts page

Страница `app/(public)/contacts/page.tsx`:
- Адрес: пр-д Черняховского, 3
- Телефон: +7 (8652) 24-25-27
- Email: college@stvcc.ru
- Часы работы: Пн-Пт 9:00-18:00
- Yandex Maps iframe с меткой на адрес
- Ссылка в Header row 1 (рядом с «Расписание») и в Footer

## A4: Import → News + Stop

**Frontend:**
- Убрать `/admin/import` из меню (`admin/layout.tsx`)
- Добавить кнопку «Импорт» на `admin/news/page.tsx` (рядом с «+ Создать»)
- Кнопка «Остановить» во время выполнения импорта

**Backend:**
- `WordPressImportService.cs`: добавить `CancellationTokenSource`, метод `StopImport(importId)` устанавливает токен
- Новый endpoint: `POST /api/import/wordpress/stop/{importId}`

## B1: Specialty cards (mti.moscow style)

Переделать `SpecialtiesSection.tsx`:
- Grid 2-3 колонки
- Каждая карточка: иконка (цветная), название специальности, квалификация, срок обучения
- Hover-эффект: подъём карточки + тень
- Кнопка «Подробнее» → `/specialties/{slug}`

## B2: Specialty detail page

`app/(public)/specialties/[slug]/page.tsx`:
- Полное описание специальности
- Квалификация, срок обучения, форма обучения
- Учебный план (список дисциплин)
- Контакты приёмной комиссии
- Кнопка «Подать заявление»

## B3: News title link

В `admin/news/page.tsx` — заголовок новости в таблице сделать ссылкой на `/news/{id}` с `target="_blank"`.

## B4: Social icons 1:1 с TPU

Скопировать SVG-иконки социальных сетей с tpu.ru (VK, Telegram, YouTube, возможно Rutube). Точные копии по размеру, цвету, пропорциям.

## C1: Logo original sizes

Убрать фиксированную высоту логотипа. Использовать `className="h-auto w-auto"` с `max-h-[80px]` чтобы SVG показывался в полный размер без искажений.

## C2: Dark theme logo

В `globals.css` или inline: для тёмной темы `filter: brightness(0) invert(1)`. Убрать фоновый цвет если есть.

## C3: Footer classic blue

Заменить `bg-black` на `bg-accent` или `bg-[var(--color-accent)]` в `Footer.tsx`. Текст на синем фоне — белый (уже используется).

## C4: Photo spacing

В `ContentRenderer.tsx` увеличить `[&_img]:mt-4` → `[&_img:not(:first-child)]:mt-6`.

## C5: Empty section News-Partners

StatisticsSection всегда рендерится. Добавить `border-b border-border` для визуального разделения секций. Если данных нет — компонент должен скрываться (но он статический, так что всегда есть).
