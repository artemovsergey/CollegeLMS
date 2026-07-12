param([string]$Message = "✅ Агент завершил работу")

# msg.exe — надежное Windows-уведомление, работает из любого контекста
msg * "OpenCode Agent: $Message" 2>$null