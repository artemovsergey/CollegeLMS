---
name: vps-bb-status
description: Безопасная проверка VPS CollegeLMS и bb (getbb.app) — SSH, диск, нагрузка, Docker, bb-треды, opencode-web, healthcheck; пороги, команды только для чтения и порядок действий при нехватке места.
---

# VPS и bb: проверка состояния

## Доступ

- Хост `176.109.105.252`, пользователь `user1`, ключ `~/.ssh/id_rsa`.
- С домашней машины прямой SSH сейчас работает: `ssh user1@176.109.105.252`.
- Если сеть блокирует TCP/22 — прокси `gate1.scc:3128` и
  `~/.ssh/ssh-proxy.cmd` (см. README репозитория `secrets`).
- Пароли и ключи в чат не выводить.

## Что смотреть (только чтение)

```bash
hostname; uptime
df -h /
free -h
docker ps --format 'table {{.Names}}\t{{.Status}}'
systemctl is-active bb-app opencode-web
systemctl --failed
timeout 20 bb status
timeout 30 bb thread list --json | head -c 4000
```

Пороги:

- свободно на `/` < 3 ГБ — критично для сборки Next.js (порог деплоя);
- load average устойчиво выше числа vCPU — что-то зависло;
- `bb-healthcheck.service` в `failed` — проверить причину, часто это диск;
- `bb` CLI может отвечать долго (нагрузка/БД 130+ МБ) — всегда `timeout`.

## Если мало места

Безопасно и обратимо:

```bash
sudo journalctl --disk-usage
sudo journalctl --vacuum-size=300M
docker system df
```

Требует явного согласия (не делать автоматически):

- `docker system prune` / `docker image prune`;
- удаление старых `~/.bb/thread-storage`, `backups`, `test-results`;
- ротация `~/.bb/logs`, `~/.bb/bb.db-wal`.

Никогда не удалять `~/.bb/bb.db`, `~/.secrets`, `/home/user1/CollegeLMS/.env`,
`/home/user1/CollegeLMS` и Docker-тома с БД.

## bb

- `bb` — сервер и CLI оркестрации агентов (getbb.app), `bb-app.service`,
  порт 38886, API `http://127.0.0.1:38886`.
- Проект CollegeLMS: `proj_ise48szvgw`, каталог
  `/home/user1/bb-workspace/CollegeLMS`.
- Провайдеры: `acp-opencode`, `acp-copilot`; перед переключением провайдера
  сверяйся с `~/.bb/AGENTS.md` (VPN для codex/claude/copilot/kilocode).
- Кастомные плагины: `~/bb-plugins/bb-plugin-{vps-monitor,maxbot-stats,vpn,
  free-quota,copilot-credits}`; установленные — `~/.bb/plugins`.
- bb-треды — рабочая среда, не удаляй и не архивируй без просьбы.

## Прод CollegeLMS

- Прод-каталог: `/home/user1/CollegeLMS`; рабочий каталог на домашней машине —
  отдельная копия, но compose-проект тот же (`collegelms`).
- Docker-команды — только из прод-каталога:
  `cd /home/user1/CollegeLMS && docker compose --profile max-bot up -d --build <service>`.
- Деплой — через GitHub Actions; ручной перезапуск только по просьбе.
- Логи: `docker compose --profile max-bot logs api --tail 50`.

## Формат отчёта

Коротко: хост/uptime, свободное место, load, статусы `bb-app` и
`opencode-web`, упавшие юниты, контейнеры не в `Up`, размер `~/.bb`, вывод
healthcheck. Затем — один рекомендуемый шаг и что требует согласия.
