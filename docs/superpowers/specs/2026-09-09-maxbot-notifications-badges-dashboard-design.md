# Дизайн: Уведомления бота, бейджи изменений, дашборд диспетчера, документы

## Контекст

Задачи пользователя по CollegeLMS (бот Max + веб-интерфейс):

1. Почему не приходят уведомления об изменениях расписания в боте.
2. Ежедневный дайджест расписания.
3. Бейджи изменений «добавлено/замена/снято/перенесено» в вебе и боте — со смысловым tooltip.
4. Стилизация сообщений бота (структура, читаемость).
5. Дашборд диспетчера: преподаватели и их занятость на текущий день.
6. Меню «Документы»: шаблоны для скачивания.
7. Мини-приложение MAX — отдельным этапом (roadmap).

## Диагностика (root causes)

| Проблема | Причина |
|----------|---------|
| Уведомление о корректировке «не пришло» | `ChangeNotifier.SelectRecipients` фильтрует по `NotifyDays` (дефолт `[1,5]` = пн/пт); корректировка в среду → никому не доставляется |
| Дайджест не ежедневный | `ScheduleNotifier` шлёт только по `NotifyDays` + пропуск воскресенья |
| Нумерация пар в неделе «1..N» в боте | `  7. Предмет…` в `FormatWeekSchedule` — валидный ordered-list в Markdown; рендерер MAX перенумеровывает; в «Сегодня» `*7.*` — не список, номера верны |
| Бейджи малосодержательные | `ChangeTag` несёт только `{ChangeType, Week}`; tooltip дублирует текст; тип «перенос» не выделен |

## Решения пользователя

- **Уведомления об изменениях** — всем подписчикам всегда (независимо от `NotifyDays`; `NotifyEnabled` остаётся фильтром).
- **Дайджест** — ежедневно, только по будням (пн–пт, сб/вс пропускаются); по дням, выбранным в `NotifyDays`.
- **Перенос** — новый тип `Move` (обнаружение: `RemovedNumberPair != NumberPair` или примечание `вм.X п`).
- **Бейджи** — расширить `ChangeTag` деталями для tooltip (`RemovedNumberPair`, `RemovedSubject`); бот — маркеры в сообщениях.
- **Стилизация бота** — Markdown-структура: единая шапка, разделители, выделенные номера пар.
- **Дашборд диспетчера** — слоты преподавателей по времени + все преподаватели со статусами + ссылка на расписание (свободные аудитории НЕ выбраны).
- **Документы** — скачивание реальных xlsx-шаблонов из `import/schedule`.
- **Мини-приложение** — за рамками фаз 1–3.

## План по фазам

- **Фаза 1 (бот)**: фикс нумерации, `ChangeNotifier` без `NotifyDays`, дефолт `NotifyDays=[1..5]`, условия `ScheduleNotifier` (пн–пт), стилизация форматтеров.
- **Фаза 2 (бейджи)**: `Move`, расширение `ChangeTag`, tooltip во фронте, маркеры в боте.
- **Фаза 3 (дашборд + документы)**: эндпоинт диспетчера, страницы дашборда и документов.
- **Фаза 4 (мини-приложение MAX)** — roadmap, отдельно.

## Изменяемые файлы

- `CollegeLMS.MaxBot/Services/MessageFormatter.cs`, `ChangeNotifier.cs`, `ScheduleNotifier.cs`
- `CollegeLMS.MaxBot/Models/UserSettings.cs`, `MaxBotService.cs` (дефолтные дни)
- `CollegeLMS.MaxBot/Clients/CollegeLmsApiDtos.cs` (+`ChangeTags` в боте)
- `CollegeLMS.API/Entities/Enums/ScheduleChangeType.cs` (+`Move`)
- `CollegeLMS.API/Services/ScheduleService.cs` (`GetChangeTagsAsync`), `ScheduleCorrectionService.cs` (детект Move)
- `CollegeLMS.API/Dtos/ScheduleDtos.cs` (`ChangeTag` + детали)
- `CollegeLMS.Next/types/correction.ts`, `schedule.ts`, `components/ChangeTagBadge.tsx`
- `CollegeLMS.Next/app/(authenticated)/dispatcher/dashboard/page.tsx`, `documents/page.tsx`
- `CollegeLMS.API/Controllers`/`Interfaces`/`Services`/`Dtos`/`Mappers` — дашборд диспетчера + шаблоны документов