# M4 «Расписание: виды День/Неделя/Месяц» — дизайн

> Дата: 2026-08-21
> Статус: утверждён
> Контекст: бэкенд расписания готов (CRUD, экспорт PDF/XLSX, импорт XLSX, `view=calendar`); фронтенд `/schedule` — таблица Пн–Пт × пары + фильтры группа/преподаватель/день + диалоги CRUD. Не реализованы UC-20 (виды день/неделя/месяц) и UC-23 (календарная сетка), нет учёта учебных недель (`weeks[]`) и навигации по неделям.

## Цель

Дать пользователю переключение видов расписания «День / Неделя / Месяц», навигацию по учебным неделям семестра с корректным учётом `weeks[]` записей, подсветку «сегодня» и текущей недели, календарную сетку месяца. Закрыть UC-20 (частично — год не делаем) и UC-23.

## Ограничения

- Бэкенд — минимальные правки (2 шт., см. §1), схема БД не меняется.
- `Weeks` хранится как `integer[]` (Postgres) → `Weeks.Contains(week)` транслируется Npgsql в `= ANY(...)`, индекс не требуется (таблица мала).
- Tailwind CSS v4, shadcn/ui, Lucide; тексты на русском; адаптивность 1366×768 и ~393px, touch-target ≥44px.
- Годовой вид (UC-20 «год») — out of scope (YAGNI).
- Существующие CRUD-диалоги, импорт/экспорт, фильтры — не трогаем (переиспользуем).

## 1. Бэкенд

### 1.1 `ScheduleWeekCalculator`

Новый хелпер `CollegeLMS.API/Services/ScheduleWeekCalculator.cs`:

```csharp
public class ScheduleWeekCalculator
{
    // SemesterStart берётся из конфига Schedule:SemesterStart (DateOnly)
    public int GetCurrentWeek(DateTime today);        // 1..22, clamp: до начала семестра = 1
}
```

- Конфиг: `appsettings.json` → `"Schedule": { "SemesterStart": "2026-09-01" }`.
- Формула: `week = floor((monday(date) − mondayOfSemesterStartWeek) / 7 дней) + 1`, где `monday(x)` — понедельник недели даты x, `mondayOfSemesterStartWeek` — понедельник недели, содержащей SemesterStart (может быть раньше самой даты старта). Clamp 1..22. Пример: старт во вт 01.09 → неделя 1 покрывает пн 31.08–вс 06.09, неделя 2 начинается с пн 07.09.
- Регистрация в DI (`ServiceCollectionExtensions`) как singleton c `IOptions`.

### 1.2 Фильтр по неделе + CurrentWeek

- `GET /api/schedule`: новый query-параметр `week:int?`. Если задан → `query.Where(s => s.Weeks.Contains(week.Value))` (в обоих ветках: список и calendar).
- `CalendarResponse` → добавить поле `CurrentWeek:int` (из `ScheduleWeekCalculator.GetCurrentWeek`). `WeekStart` оставить как есть.

Swagger-аннотации обновить (новый параметр, новое поле).

## 2. Фронтенд

### 2.1 Состояние и URL

- Страница `/schedule` хранит вид и дату-якорь в query-параметрах: `?view=day|week|month&date=YYYY-MM-DD` (по умолчанию `week` + сегодня). Используем `useRouter`/`useSearchParams` (shallow), чтобы ссылки шарились.
- Учебная неделя N вычисляется на клиенте из `CurrentWeek` (запрос calendar при загрузке отдаёт якорь) ИЛИ локально: `weekOf(date) = floor((monday(date) − monday(semesterStart))/7)+1`. SemesterStart прокидываем через новый лёгкий эндпоинт? НЕТ — YAGNI: клиент берёт `CurrentWeek` из ответа `?view=calendar` при первой загрузке и дальше считает смещения относительно него (прибавляя ±1 за шаг ◀/▶). При выходе за диапазон — clamp 1..22 на бэкенде при фильтрации (week вне 1..22 → пустой результат).

### 2.2 Переключатель видов

- shadcn `Tabs` («День | Неделя | Месяц») над фильтрами; стиль — существующий `Tabs` проекта.
- Иконки: `Sunrise` (день), `Columns3` (неделя), `CalendarDays` (месяц) — по DESIGN.md.

### 2.3 Навигация по неделям

- Строка: `◀` `▶` кнопки (icon-button ≥44px), по центру «Неделя N · DD.MM–DD.MM», справа кнопка «Сегодня».
- Шаг ±7 дней от якорной даты; «Сегодня» сбрасывает якорь.
- Бейдж «N семестр» рядом с заголовком страницы (из CurrentWeek).

### 2.4 Вид «День»

- Дата дня = якорная дата; вертикальный список пар этого дня (переиспользуем карточный рендер мобильного вида `ScheduleTable`, вынесенный в `ScheduleDayList`).
- Занятия дня = записи, у которых `dayOfWeek == день даты` И `weeks` содержит номер учебной недели этой даты (клиентская фильтрация по уже загруженному списку `pageSize=100`... недостаточно при >100 записях — поэтому для дня запрашиваем `GET /api/schedule?groupId|teacherId&week=N` и фильтруем по dayOfWeek локально).
- Кнопки ◀/▶ в виде «День» листают дни.

### 2.5 Вид «Неделя» (улучшение текущей таблицы)

- Запрос: `GET /api/schedule?…&week=N` — только занятия выбранной недели.
- Подсветка сегодня: колонка дня с `ring-1 ring-primary/40 bg-accent/30`.
- Пустое состояние: «На неделю N занятий нет» (+ кнопка «Показать все недели» → запрос без week).
- Мобильная раскладка — как сейчас, но тоже по week-фильтру.

### 2.6 Вид «Месяц»

- Новый компонент `components/ScheduleMonthGrid.tsx`:
  - Сетка 7×N (Пн–Вс), ячейки: число месяца, до 3 мини-чипов занятий (subject, цвет = LESSON_TYPE_STYLES), «+ещё N»; ячейки соседних месяцев — приглушённые.
  - Источник данных: тот же список записей (без week-фильтра, `pageSize=100`); принадлежность дате: `dayOfWeek` совпал И `weeks` содержит номер учебной недели даты.
  - Клик по чипу → (canManage ? редактирование : ничего); клик по ячейке → переход в вид «День» этой даты.
  - Сегодня: `bg-primary/10 ring-1 ring-primary/40 rounded-md`.
  - Адаптив: на <md чипы заменяются точками (цвет по типу), клик по ячейке → день.

### 2.7 Файлы

| Действие | Путь |
|---|---|
| Новый | `CollegeLMS.API/Services/ScheduleWeekCalculator.cs` |
| Правка | `Extensions/ServiceCollectionExtensions.cs` (DI), `appsettings.json` |
| Правка | `Controllers/ScheduleController.cs`, `Services/ScheduleService.cs`, `Dtos/ScheduleDtos.cs`, `Interfaces/IScheduleService.cs` (week-параметр, CurrentWeek) |
| Новый | `components/ScheduleMonthGrid.tsx`, `components/ScheduleViewTabs.tsx` (или inline), `lib/scheduleWeeks.ts` (клиентский расчёт номера недели) |
| Правка | `app/(authenticated)/schedule/page.tsx` (состояние вида/dates, запросы), `components/ScheduleTable.tsx` (подсветка сегодня, проп currentWeek), `api/schedule.ts` (week-параметр, CalendarResponse-тип) |

## 3. Данные и ошибки

- Ошибки API — через существующий ErrorBanner/toast; пустые виды — EmptyState-паттерн проекта.
- Loading — существующий LoadingSpinner; при смене недели — мягкая перезагрузка таблицы (без полного скелета, если данные кэшированы — не усложняем).

## 4. Тесты

- **Unit** (`CollegeLMS.Tests/Unit/Services/ScheduleWeekCalculatorTests.cs`): до семестра → 1, внутри → корректный номер, после 22-й → 22, понедельник/воскресенье границы.
- **Integration**: `GET /api/schedule?week=2` возвращает только записи с 2 в weeks; `view=calendar` содержит `currentWeek`; week вне диапазона → пусто, 200.
- **E2E** (`e2e/schedule.spec.ts` — расширить): переключение видов (табы), ◀/▶ меняют подпись недели, месяц рендит сетку и переходит в день по клику, подсветка «сегодня».

## 5. Definition of Done

- [ ] dotnet build + dotnet test зелёные
- [ ] npm run build зелёный
- [ ] Виды День/Неделя/Месяц работают, URL синхронизирован (?view=&date=)
- [ ] Фильтр ?week= работает (Swagger + Postman-коллекция обновлена)
- [ ] E2E зелёные (M4-спеки)
- [ ] docker compose up --build проходит, смоук на localhost
- [ ] PlantUML sequence для week-фильтра (docs/diagrams/sequence/schedule-week.puml)
