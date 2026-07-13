# Восстановление сети на VPS Cloud.ru

## История проблемы

13.07.2026 — VPS перестала отвечать по SSH и бот перестал работать.
Причина: **enp3s0 поднят (UP), но IPv4 от DHCP не получен.**

### Диагностика
```bash
# Интерфейс UP, но без IPv4:
ip a show enp3s0
# enp3s0: <BROADCAST,MULTICAST,UP,LOWER_UP> ... inet6 fe80::... (только link-local)
# НЕТ inet 10.x.x.x

# Нет маршрута по умолчанию:
ip route
# только docker-сети

# DNS не резолвится:
ping google.com  # Temporary failure in name resolution
ping 8.8.8.8     # Network is unreachable
```

## Причина

Cloud.ru использует ConfigDrive + cloud-init для настройки сети.
При некоторых загрузках DHCP-клиент не отрабатывает, и интерфейс
остаётся без IPv4. Это известная проблема провайдера.

## Решение (ручное)

### Шаг 1 — cloud-init clean + init
```bash
sudo cloud-init clean
sudo cloud-init init
```

### Шаг 2 — Перезагрузка
```bash
sudo reboot
```

### Шаг 3 — Если не помогло — принудительное применение netplan
```bash
sudo netplan apply
# или
sudo ip link set enp3s0 down && sudo ip link set enp3s0 up
sudo netplan apply
```

### Шаг 4 — Если всё ещё нет IP — выключить/включить ВМ в панели Cloud.ru
1. Личный кабинет Cloud.ru → Виртуальные машины
2. Нажать "Выключить"
3. После полной остановки — "Включить"
4. Повторить Шаг 1-2 после загрузки

## Скрипт авто-восстановления (на будущее)

Если проблема будет повторяться — установить systemd-сервис:

### `/usr/local/bin/fix-network.sh`
```bash
#!/bin/bash
# Авто-восстановление сети при загрузке (Cloud.ru bug)
# Проверяет, получил ли enp3s0 IPv4 адрес

LOGFILE="/var/log/fix-network.log"
INTERFACE="enp3s0"

echo "[$(date)] Проверка сети на $INTERFACE..." >> "$LOGFILE"

if ! ip -4 addr show "$INTERFACE" | grep -q "inet "; then
    echo "[$(date)] IPv4 не найден на $INTERFACE. Пытаюсь восстановить..." >> "$LOGFILE"

    # Попытка 1: netplan apply
    netplan apply 2>&1 >> "$LOGFILE"
    sleep 5

    if ip -4 addr show "$INTERFACE" | grep -q "inet "; then
        echo "[$(date)] ✅ IPv4 получен после netplan apply" >> "$LOGFILE"
        exit 0
    fi

    # Попытка 2: перезапуск systemd-networkd
    systemctl restart systemd-networkd 2>&1 >> "$LOGFILE"
    sleep 10

    if ip -4 addr show "$INTERFACE" | grep -q "inet "; then
        echo "[$(date)] ✅ IPv4 получен после restart systemd-networkd" >> "$LOGFILE"
        exit 0
    fi

    # Попытка 3: сброс линка + cloud-init
    ip link set "$INTERFACE" down && sleep 2 && ip link set "$INTERFACE" up
    cloud-init clean && cloud-init init 2>&1 >> "$LOGFILE"
    sleep 10

    if ip -4 addr show "$INTERFACE" | grep -q "inet "; then
        echo "[$(date)] ✅ IPv4 получен после cloud-init reinit" >> "$LOGFILE"
        exit 0
    fi

    echo "[$(date)] ❌ Не удалось восстановить сеть. Нужно ручное вмешательство." >> "$LOGFILE"
else
    echo "[$(date)] ✅ IPv4 уже есть: $(ip -4 addr show "$INTERFACE" | grep inet | awk '{print $2}')" >> "$LOGFILE"
fi
```

### `/etc/systemd/system/fix-network.service`
```ini
[Unit]
Description=Network auto-recovery (Cloud.ru fix)
After=network-online.target
Wants=network-online.target

[Service]
Type=oneshot
ExecStart=/usr/local/bin/fix-network.sh
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
```

### Установка
```bash
sudo chmod +x /usr/local/bin/fix-network.sh
sudo systemctl daemon-reload
sudo systemctl enable --now fix-network.service
```

### Проверка
```bash
sudo systemctl status fix-network.service
cat /var/log/fix-network.log
```

## Полезные ссылки
- https://cloud.ru/docs/virtual-machines/ug/topics/troubleshooting.html
- https://cloud.ru/docs/virtual-machines/ug/topics/guides__activate-network-interface.html
