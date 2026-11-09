param([switch]$AuditOnly)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$textColumns = @{
    User = @('FullName'); Role = @('Description'); Permission = @('Description')
    Warehouse = @('WarehouseName', 'Address'); Supplier = @('SupplierName', 'ContactPerson', 'Address')
    Product = @('ProductName'); ProductVariant = @('VariantName', 'Specs'); Category = @('CategoryName')
    StorageSectionType = @('Description'); StorageSection = @('SectionName'); StorageBin = @('BinName')
}
$keys = @{
    User = 'UserName'; Warehouse = 'WarehouseCode'; Supplier = 'SupplierCode'; Product = 'ProductCode'
    ProductVariant = 'SKU'; Category = 'CategoryName'; StorageSectionType = 'Id'; StorageSection = 'Id'; StorageBin = 'BinCode'
}
$manifest = Get-Content (Join-Path $PSScriptRoot 'demo-text-repairs.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $manifest) {
    if (!$textColumns.ContainsKey($entry.table) -or $entry.column -notin $textColumns[$entry.table] -or $keys[$entry.table] -ne $entry.keyColumn) {
        throw 'Unexpected table/column in text repair manifest.'
    }
}
$connection = New-Object System.Data.SqlClient.SqlConnection 'Server=.;Database=EWMS_GoodsReceipt_Development;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;Connect Timeout=5'
$transaction = $null
function Read-TextRows {
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($table in $textColumns.Keys) {
        foreach ($column in $textColumns[$table]) {
            $command = $connection.CreateCommand(); $command.CommandTimeout = 15
            if ($transaction) { $command.Transaction = $transaction }
            $command.CommandText = "SELECT Id, CONVERT(nvarchar(4000), [$column]) AS TextValue FROM dbo.[$table] WHERE [$column] IS NOT NULL"
            $reader = $command.ExecuteReader()
            try { while ($reader.Read()) { $rows.Add([pscustomobject]@{table=$table; id=$reader.GetInt32(0); column=$column; value=$reader.GetString(1)}) } }
            finally { $reader.Dispose(); $command.Dispose() }
        }
    }
    return ,$rows.ToArray()
}
try {
    $connection.Open()
    $before = Read-TextRows
    $brokenBefore = @($before | Where-Object { $_.value -match '[?\uFFFD]|\u00C3|\u00E1\u00BA|\u00E1\u00BB' })
    Write-Host ("Before: {0} text values checked, {1} suspicious values." -f $before.Count, $brokenBefore.Count)
    if ($AuditOnly) {
        $brokenBefore | Format-Table table,id,column,value -AutoSize
        return
    }
    # Store only display text and row IDs, never passwords/tokens or other account fields.
    $backupRoot = Join-Path $projectRoot '.local'
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $backupPath = Join-Path $backupRoot "unicode-text-before-$stamp.json"
    $before | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $backupPath -Encoding UTF8
    $transaction = $connection.BeginTransaction()
    $changed = 0
    foreach ($entry in $manifest) {
        $command = $connection.CreateCommand(); $command.CommandTimeout = 15; $command.Transaction = $transaction
        # Only the exact known lossy varchar representation is eligible. Custom names stay intact.
        $conflict = if ($entry.table -eq 'Category') {
            'AND NOT EXISTS (SELECT 1 FROM dbo.Category other WHERE other.Id <> target.Id AND other.CategoryName = @correct)'
        } else { '' }
        $command.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id=t.user_type_id
 WHERE c.object_id=OBJECT_ID(N'dbo.[$($entry.table)]') AND c.name=@column AND t.name=N'nvarchar')
 THROW 51011, 'Apply PreserveVietnameseText migration first.', 1;
UPDATE target SET [$($entry.column)] = @correct
FROM dbo.[$($entry.table)] target
WHERE CONVERT(nvarchar(500), target.[$($entry.keyColumn)]) IN (@key, CONVERT(nvarchar(500), CONVERT(varchar(500), @key)))
 AND CONVERT(nvarchar(4000), target.[$($entry.column)]) COLLATE Latin1_General_100_BIN2 <>
     @correct COLLATE Latin1_General_100_BIN2
 AND CONVERT(nvarchar(4000), target.[$($entry.column)]) COLLATE Latin1_General_100_BIN2 =
     CONVERT(nvarchar(4000), CONVERT(varchar(4000), @correct)) COLLATE Latin1_General_100_BIN2
 $conflict;
"@
        [void]$command.Parameters.Add('@correct', [System.Data.SqlDbType]::NVarChar, 500); $command.Parameters['@correct'].Value = $entry.correctValue
        [void]$command.Parameters.Add('@key', [System.Data.SqlDbType]::NVarChar, 500); $command.Parameters['@key'].Value = $entry.keyValue
        [void]$command.Parameters.Add('@column', [System.Data.SqlDbType]::NVarChar, 128); $command.Parameters['@column'].Value = $entry.column
        try { $changed += $command.ExecuteNonQuery() } finally { $command.Dispose() }
    }
    $after = Read-TextRows
    $brokenAfter = @($after | Where-Object { $_.value -match '[?\uFFFD]|\u00C3|\u00E1\u00BA|\u00E1\u00BB' })
    $transaction.Commit(); $transaction.Dispose(); $transaction = $null
    $reportPath = Join-Path $backupRoot "unicode-text-after-$stamp.json"
    [pscustomobject]@{checked=$after.Count; changed=$changed; remaining=$brokenAfter} |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host ("After: {0} values repaired; {1} suspicious values remain." -f $changed, $brokenAfter.Count)
    Write-Host "Backup: $backupPath"
    Write-Host "Report: $reportPath"
    if ($brokenAfter.Count) { $brokenAfter | Format-Table table,id,column,value -AutoSize }
} catch {
    if ($transaction) { $transaction.Rollback(); $transaction.Dispose(); $transaction = $null }
    throw
} finally { $connection.Dispose() }
