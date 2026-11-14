$ErrorActionPreference = "Stop"

$workspace = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$secretDir = Join-Path $workspace ".dotnet_cli_home"
$pidFile = Join-Path $secretDir "solqaryn-mysql.pid"
$mysqladmin = "C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysqladmin.exe"

$previousErrorPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
& $mysqladmin --host=127.0.0.1 --port=3307 --user=root shutdown *> $null
$ErrorActionPreference = $previousErrorPreference

if (Test-Path $pidFile) {
    $mysqlPid = (Get-Content $pidFile -Raw).Trim()
    if ($mysqlPid -and (Get-Process -Id $mysqlPid -ErrorAction SilentlyContinue)) {
        Stop-Process -Id $mysqlPid -Force
    }
    Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Solqaryn MySQL stopped."
