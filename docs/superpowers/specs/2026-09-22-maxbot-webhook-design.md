# Дизайн: webhook для бота MAX

- **Дата:** 2026-09-22
- **Статус:** реализовано (ветка master)
- **Компоненты:** `CollegeLMS.MaxBot`, `loadbalancer`, `docker-compose.yml`, `deploy.yml`

## Контекст

MAX не рекомендует long polling для production: доставка ограничена по скорости
и сроку хранения событий. После инцидента 22.09.2026 (бот молчал из-за
`400 proto.payload` на inline-кнопке `open_app`) стало ясно, что у поллинга
нет явной диагностики доставки — события «исчезают» вместе с marker.

## Решение

Бот переходит на **webhook-подписку** с секретом и резервным long polling:

1. При старте (`MaxBotService.ExecuteAsync`) бот читает `GET /subscriptions`
   и удаляет подписки с чужим URL, затем оформляет `POST /subscriptions`:
   `{ url, update_types: [message_created, message_callback, bot_started], secret }`.
2. MAX шлёт каждый `Update` как HTTPS POST на `https://stvcc.tech/maxbot/webhook`
   с заголовком `X-Max-Bot-Api-Secret`.
3. `POST /maxbot/webhook` (Program.cs) проверяет секрет (сравнение в постоянном
   времени), десериализует `MaxUpdate` и кладёт его в `MaxUpdateQueue`
   (bounded 1000, DropOldest), сразу возвращая `200` (требование MAX — ответ
   за 30 секунд).
4. `MaxBotService` читает очередь (`ConsumeWebhookUpdatesAsync`) и обрабатывает
   апдейты тем же `HandleUpdateAsync`, что и при поллинге.
5. Если подписка не удалась (3 попытки, интервал 5 с) — бот переходит на
   резервный long polling, **продолжая** читать очередь: если доставка
   на эндпоинт ещё работает, события не теряются.

## Схема

```
MAX platform-api2 (Update)
  → nginx (stvcc.tech, location /maxbot/)      loadbalancer
  → maxbot:8080  POST /maxbot/webhook          CollegeLMS.MaxBot
      → WebhookSecretValidator (401 при неверном секрете)
      → MaxUpdateQueue (Channel, 1000, DropOldest)
  → MaxBotService.ConsumeWebhookUpdatesAsync
      → HandleUpdateAsync → ответы через MaxApiClient
```

## Конфигурация

| Ключ | Env | Назначение |
|------|-----|-----------|
| `MaxBot:WebhookUrl` | `MAX_BOT_WEBHOOK_URL` | Публичный URL; пусто — long polling. Дефолт в compose: `https://stvcc.tech/maxbot/webhook` |
| `MaxBot:WebhookSecret` | `MAX_BOT_WEBHOOK_SECRET` | Секрет подписки (5–256 `[A-Za-z0-9_-]`); в деплое обязателен |

`deploy.yml`:

- передаёт `MAX_BOT_WEBHOOK_SECRET` в SSH-скрипт и `.env`;
- **падает** (`exit 1`) при пустом секрете — иначе эндпоинт был бы открыт для
  поддельных апдейтов;
- проверяет `POST https://stvcc.tech/maxbot/webhook` → ожидает `200/400/401`
  (маршрут через nginx живой), иначе `exit 1`;
- в grep старта MaxBot добавлен шаблон `Webhook-подписка активна`.

## Изменённые файлы

- `CollegeLMS.MaxBot/MaxBotOptions.cs` — `WebhookUrl`, `WebhookSecret`
- `CollegeLMS.MaxBot/Models/Max/MaxSubscription.cs` — модели подписок
- `CollegeLMS.MaxBot/Clients/MaxApiClient.cs` — `GetSubscriptionsAsync`,
  `SubscribeWebhookAsync`, `UnsubscribeWebhookAsync`
- `CollegeLMS.MaxBot/Services/MaxUpdateQueue.cs`,
  `Services/WebhookSecretValidator.cs`
- `CollegeLMS.MaxBot/Program.cs` — `MapPost("/maxbot/webhook")`, DI очереди
- `CollegeLMS.MaxBot/Bot/MaxBotService.cs` — webhook-режим, резервный поллинг
- `loadbalancer/nginx.conf` — `location /maxbot/` (variable upstream, чтобы
  nginx стартовал без профиля `max-bot`)
- `docker-compose.yml`, `.env.example`, `deploy.yml`, `CollegeLMS.MaxBot/README.md`

## Тесты

- `MaxApiSubscriptionTests` — GET/POST/DELETE `/subscriptions`, наличие/отсутствие
  `secret`, `success=false`.
- `WebhookSecretValidatorTests` — пустой секрет (проверка выключена), совпадение,
  несовпадение.
- `MaxUpdateQueueTests` — порядок, вытеснение самого старого при переполнении.

## Ограничения и риски

- MAX ретраит доставку 10 раз (60 с → … ×2.5); после 8 часов без успешного
  ответа подписка снимается автоматически. Бот узнает об этом только при
  рестарте (переподписка) — возможное улучшение: периодическая проверка
  подписки.
- Секрет обязателен в деплое; для локальной разработки без вебхука
  достаточно не задавать `MaxBot:WebhookUrl` (long polling).
- Deep-link `start_param` в mini-app пока не читается фронтендом —
  отдельная задача.

## Критерии приёмки

- [x] Бот поднимает подписку и логирует `Webhook-подписка активна: {Url}`
- [x] `POST https://stvcc.tech/maxbot/webhook` без секрета → `401`
- [x] Апдейты `/start` и inline-кнопок обрабатываются (пользователь видит ответ)
- [x] При недоступности подписки включается резервный long polling
- [x] CI/CD зелёный, деплой на VPS выполнен
