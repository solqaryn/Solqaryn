param(
    [Parameter(Mandatory = $true)]
    [string]$RootPassword,

    [string]$RootUser = "root",
    [string]$AppUser = "Solqaryn",
    [string]$Database = "solqaryn",
    [string]$Server = "localhost",
    [int]$Port = 3306,
    [string]$AppPassword
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($AppPassword)) {
    $passwordFile = Join-Path $PSScriptRoot "..\..\.dotnet_cli_home\solqaryn_mysql_password.txt"
    if (Test-Path $passwordFile) {
        $AppPassword = (Get-Content $passwordFile -Raw).Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($AppPassword)) {
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] 24
    $rng.GetBytes($bytes)
    $AppPassword = [Convert]::ToBase64String($bytes).Replace("+", "A").Replace("/", "B").TrimEnd("=")
}

$escapedPassword = $AppPassword.Replace("'", "''")
$sql = @"
CREATE DATABASE IF NOT EXISTS `$Database` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '$AppUser'@'localhost' IDENTIFIED BY '$escapedPassword';
ALTER USER '$AppUser'@'localhost' IDENTIFIED BY '$escapedPassword';
GRANT ALL PRIVILEGES ON `$Database`.* TO '$AppUser'@'localhost';
FLUSH PRIVILEGES;
"@

$sql | mysql --host=$Server --port=$Port --user=$RootUser --password=$RootPassword

$connectionString = "Server=$Server;Port=$Port;Database=$Database;User=$AppUser;Password=$AppPassword;"
Push-Location (Join-Path $PSScriptRoot "..\src\API")
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString | Out-Null
Pop-Location

Push-Location (Join-Path $PSScriptRoot "..")
dotnet ef database update -p src\Infrastructure -s src\API
Pop-Location

Write-Host "MySQL user '$AppUser' is ready and migrations were applied to '$Database'."
