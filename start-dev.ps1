# start-dev.ps1
# Tự động thiết lập biến môi trường và chạy cả Backend & Frontend

# 1. Thiết lập môi trường cho Backend
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__EWMSDatabase = 'Server=.;Database=EWMS_GoodsReceipt_Development;Integrated Security=true;TrustServerCertificate=true;Encrypt=false'
$env:GoodsReceipts__UseDevelopmentUserHeader = 'true'
$env:GoodsReceipts__SeedDevelopmentData = 'true'

# 2. Khởi động Backend trong một cửa sổ PowerShell mới
Start-Process powershell -ArgumentList '-NoExit', '-Command', @"
cd '$PWD'
dotnet run --project EWMS/EWMS.Api/EWMS.Api.csproj --no-launch-profile --urls http://localhost:5085
"@

# 3. Khởi động Frontend trong một cửa sổ PowerShell khác
Start-Process powershell -ArgumentList '-NoExit', '-Command', @"
cd '$PWD/frontend'
npm.cmd run dev -- --host 127.0.0.1 --port 5173
"@

Write-Host "Đang khởi động Backend và Frontend..."
Write-Host "Backend: http://localhost:5085"
Write-Host "Frontend: http://127.0.0.1:5173"