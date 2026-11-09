$ErrorActionPreference = 'Stop'
$apiProcess = $null
$webProcess = $null
$projectRoot = $PSScriptRoot
$apiProject = Join-Path $projectRoot 'EWMS/EWMS.Api/EWMS.Api.csproj'
$frontendRoot = Join-Path $projectRoot 'frontend'
$runtimeDirectory = Join-Path $projectRoot '.local'
New-Item -ItemType Directory -Path $runtimeDirectory -Force | Out-Null

# Dùng database local hiện có; không tự thêm tài khoản hoặc dữ liệu mẫu.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__EWMSDatabase = 'Server=.;Database=EWMS_GoodsReceipt_Development;Integrated Security=true;TrustServerCertificate=true;Encrypt=false'
try {
    foreach ($webPort in @(5085, 5173)) {
        if (Get-NetTCPConnection -LocalPort $webPort -State Listen -ErrorAction SilentlyContinue) {
            throw "Cong $webPort dang duoc su dung. Dung ung dung cu truoc khi chay lai."
        }
    }
    dotnet build $apiProject --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
    dotnet ef database update --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'Database migration failed.' }
    if (!(Test-Path (Join-Path $frontendRoot 'node_modules/vite/bin/vite.js'))) {
        Push-Location $frontendRoot
        try { npm.cmd ci; if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' } }
        finally { Pop-Location }
    }
    $env:VITE_API_PROXY_TARGET = 'http://localhost:5085'
    $apiDll = Join-Path $projectRoot 'EWMS/EWMS.Api/bin/Debug/net8.0/EWMS.Api.dll'
    $apiProcess = Start-Process dotnet -ArgumentList @("`"$apiDll`"", '--urls', 'http://localhost:5085') -WorkingDirectory (Split-Path $apiProject) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtimeDirectory 'api.log') -RedirectStandardError (Join-Path $runtimeDirectory 'api-error.log')
    $apiReady = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($apiProcess.HasExited) { throw 'API khong khoi dong duoc. Xem .local/api.log va api-error.log.' }
        try {
            $health = Invoke-WebRequest 'http://localhost:5085/api/auth/csrf' -UseBasicParsing -TimeoutSec 1
            if ($health.StatusCode -eq 200) { $apiReady = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (!$apiReady) { throw 'API chua san sang. Xem .local/api.log.' }
    $webProcess = Start-Process node -ArgumentList @('node_modules/vite/bin/vite.js', '--host', '127.0.0.1', '--port', '5173', '--strictPort') -WorkingDirectory $frontendRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtimeDirectory 'web.log') -RedirectStandardError (Join-Path $runtimeDirectory 'web-error.log')
    @($apiProcess, $webProcess) | Select-Object Id, ProcessName, @{Name='StartTimeUtcTicks'; Expression={$_.StartTime.ToUniversalTime().Ticks}} | ConvertTo-Json | Set-Content (Join-Path $runtimeDirectory 'processes.json') -Encoding UTF8
    Write-Host 'Web: http://127.0.0.1:5173/login'
    Write-Host 'Log: .local/api.log va .local/web.log. Dung bang ./stop-dev.ps1'
} catch {
    # Nếu khởi động dở dang, chỉ dừng các process vừa tạo trong lần gọi này.
    if ($webProcess -and !$webProcess.HasExited) { Stop-Process -Id $webProcess.Id -ErrorAction SilentlyContinue }
    if ($apiProcess -and !$apiProcess.HasExited) { Stop-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue }
    throw
}
