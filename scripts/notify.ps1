param([string]$Message = "✅ Агент завершил работу")

Add-Type -AssemblyName System.Windows.Forms

$notify = New-Object System.Windows.Forms.NotifyIcon
$notify.Icon = [System.Drawing.SystemIcons]::Information
$notify.BalloonTipTitle = "OpenCode Agent"
$notify.BalloonTipText = $Message
$notify.Visible = $true
$notify.ShowBalloonTip(5000)

Start-Sleep -Seconds 6
$notify.Dispose()