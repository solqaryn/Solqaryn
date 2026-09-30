param(
    [int]$Port = 3307
)

$ErrorActionPreference = "Stop"

$workspace = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$secretDir = Join-Path $workspace ".dotnet_cli_home"
$mysqlBase = "C:\laragon\bin\mysql\mysql-8.4.3-winx64"
$defaultsFile = Join-Path $mysqlBase "my.ini"
$mysqld = Join-Path $mysqlBase "bin\mysqld.exe"
$mysql = Join-Path $mysqlBase "bin\mysql.exe"
$mysqladmin = Join-Path $mysqlBase "bin\mysqladmin.exe"
$dataDir = "C:\laragon\data\mysql-8.4"
$pidFile = Join-Path $secretDir "solqaryn-mysql.pid"
$appPasswordFile = Join-Path $secretDir "solqaryn_mysql_password.txt"

New-Item -ItemType Directory -Force -Path $secretDir | Out-Null

function New-SafePassword([int]$byteCount = 24) {
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] $byteCount
    $rng.GetBytes($bytes)
    return [Convert]::ToBase64String($bytes).Replace("+", "A").Replace("/", "B").TrimEnd("=")
}

function Test-RootPing([int]$PingPort) {
    $previousErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    & $mysqladmin --host=127.0.0.1 --port=$PingPort --user=root ping *> $null
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorPreference
    return $exitCode -eq 0
}

if (-not (Test-Path $appPasswordFile)) {
    Set-Content -Path $appPasswordFile -Value (New-SafePassword) -Encoding ASCII
}

if (Test-RootPing -PingPort $Port) {
    Write-Host "Solqaryn MySQL is already running on 127.0.0.1:$Port."
} else {
    $args = @(
        "--defaults-file=$defaultsFile",
        "--datadir=$dataDir",
        "--port=$Port",
        "--bind-address=127.0.0.1",
        "--mysqlx=0",
        "--console"
    )

    $proc = Start-Process -FilePath $mysqld -ArgumentList $args -WindowStyle Hidden -PassThru
    Set-Content -Path $pidFile -Value $proc.Id -Encoding ASCII

    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 1
        if ($proc.HasExited) {
            throw "mysqld exited early with code $($proc.ExitCode)."
        }

        if (Test-RootPing -PingPort $Port) {
            $ready = $true
            break
        }
    }

    if (-not $ready) {
        throw "Solqaryn MySQL did not become ready on port $Port."
    }
}

$appPassword = (Get-Content $appPasswordFile -Raw).Trim()
$appEscaped = $appPassword.Replace("'", "''")
$sql = @"
CREATE DATABASE IF NOT EXISTS solqaryn CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'Solqaryn'@'localhost' IDENTIFIED BY '$appEscaped';
ALTER USER 'Solqaryn'@'localhost' IDENTIFIED BY '$appEscaped';
CREATE USER IF NOT EXISTS 'Solqaryn'@'127.0.0.1' IDENTIFIED BY '$appEscaped';
ALTER USER 'Solqaryn'@'127.0.0.1' IDENTIFIED BY '$appEscaped';
GRANT ALL PRIVILEGES ON solqaryn.* TO 'Solqaryn'@'localhost';
GRANT ALL PRIVILEGES ON solqaryn.* TO 'Solqaryn'@'127.0.0.1';
FLUSH PRIVILEGES;
"@

$sql | & $mysql --host=127.0.0.1 --port=$Port --user=root
if ($LASTEXITCODE -ne 0) {
    throw "Solqaryn MySQL user/database setup failed."
}

$connectionString = "Server=127.0.0.1;Port=$Port;Database=solqaryn;User=Solqaryn;Password=$appPassword;"
Push-Location (Join-Path $workspace "backend\src\API")
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString | Out-Null
Pop-Location

Write-Host "Solqaryn MySQL is ready on 127.0.0.1:$Port."
