Add-Type -AssemblyName System.Windows.Forms

# Создаем иконку уведомления
$notify = New-Object System.Windows.Forms.NotifyIcon
$notify.Icon = [System.Drawing.SystemIcons]::Information
$notify.BalloonTipTitle = "OpenCode Agent"
$notify.BalloonTipText = "✅ Агент завершил работу"
$notify.Visible = $true
$notify.ShowBalloonTip(5000)

# Ждем, чтобы уведомление успело отобразиться
Start-Sleep -Seconds 6
$notify.Dispose()