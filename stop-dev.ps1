$ErrorActionPreference = 'Stop'
$processFile = Join-Path $PSScriptRoot '.local/processes.json'
if (!(Test-Path $processFile)) { Write-Host 'Khong co phien do start-dev.ps1 ghi nhan.'; return }
# Chỉ dừng đúng PID và thời điểm bắt đầu đã ghi; tránh dừng nhầm process dùng lại PID.
$ownedProcesses = Get-Content -LiteralPath $processFile -Raw | ConvertFrom-Json
foreach ($owned in $ownedProcesses) {
    $running = Get-Process -Id $owned.Id -ErrorAction SilentlyContinue
    if ($running -and $running.ProcessName -eq $owned.ProcessName -and
        $running.StartTime.ToUniversalTime().Ticks -eq [long]$owned.StartTimeUtcTicks) {
        Stop-Process -Id $running.Id
    }
}
Remove-Item -LiteralPath $processFile
Write-Host 'Da dung phien local.'
