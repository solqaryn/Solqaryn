# PROJECT_SCOPE_LOCK=STRICT
# EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
# PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
# PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
# EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
# LOCAL_SKILL_COUNT=1
# Prohibido usar skills/documentacion/contexto fuera de SOLQARYN sin autorizacion explicita o allowlist ACTIVE.

[CmdletBinding()]
param(
    [string]$Repository = "solqaryn/Solqaryn",
    [string]$Branch = "dev",
    [ValidateSet("ALL", "J1", "J2", "J3", "J4", "J5", "J6")]
    [string]$Worker = "ALL"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Require-Command([string]$Name, [string]$InstallHint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "No se encontro '$Name'. $InstallHint"
    }
}

function Test-JulesCredential([string]$WorkerId, [string]$PlainKey) {
    $headers = @{ "x-goog-api-key" = $PlainKey }
    $sources = Invoke-RestMethod -Uri "https://jules.googleapis.com/v1alpha/sources?pageSize=100" -Headers $headers -Method Get
    $source = @($sources.sources) | Where-Object {
        $_.githubRepo.owner -eq "solqaryn" -and $_.githubRepo.repo -eq "Solqaryn"
    } | Select-Object -First 1
    if (-not $source) {
        throw "$WorkerId: la credencial es valida para la API, pero Solqaryn no aparece como source de Jules."
    }
    $hasBranch = @($source.githubRepo.branches) | Where-Object { $_.displayName -eq $Branch }
    if (-not $hasBranch) {
        throw "$WorkerId: Jules ve Solqaryn, pero no expone la rama '$Branch'."
    }
    Write-Host "$WorkerId readiness OK: Solqaryn / $Branch" -ForegroundColor Green
}

Write-Step "Validando identidad Solqaryn"
if ($Repository -ne "solqaryn/Solqaryn") {
    throw "Repositorio no autorizado para este script: $Repository"
}
if ($Branch -ne "dev") {
    throw "Rama no autorizada para este script: $Branch"
}

Require-Command "gh" "Instala GitHub CLI y autentica tu cuenta antes de continuar."

gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI no esta autenticado. Ejecuta 'gh auth login' y vuelve a correr este script."
}

$workers = if ($Worker -eq "ALL") { @("J1", "J2", "J3", "J4", "J5", "J6") } else { @($Worker) }

foreach ($workerId in $workers) {
    $secretName = "JULES_${workerId}_API_KEY"
    Write-Step "Validando y registrando $secretName"
    Write-Host "La clave no se mostrara ni se guardara en archivos." -ForegroundColor Yellow
    $secureKey = Read-Host $secretName -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)
    $plainKey = $null
    try {
        $plainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        if ([string]::IsNullOrWhiteSpace($plainKey)) {
            throw "$secretName esta vacio."
        }
        Test-JulesCredential -WorkerId $workerId -PlainKey $plainKey
        $plainKey | gh secret set $secretName --repo $Repository
        if ($LASTEXITCODE -ne 0) {
            throw "No fue posible registrar $secretName en GitHub Actions."
        }
    }
    finally {
        if ($null -ne $plainKey) { $plainKey = $null }
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

Write-Step "Verificando nombres de secrets canónicos"
$secretNames = @(& gh secret list --repo $Repository --json name --jq '.[].name')
if ($LASTEXITCODE -ne 0) {
    throw "No fue posible listar los secrets del repositorio."
}
foreach ($workerId in $workers) {
    $secretName = "JULES_${workerId}_API_KEY"
    if ($secretNames -notcontains $secretName) {
        throw "GitHub no reporta el secret $secretName."
    }
    Write-Host "$secretName registrado (valor oculto)." -ForegroundColor Green
}

Write-Host "`nPRE-FLIGHT JULES J1-J6 COMPLETADO." -ForegroundColor Green
Write-Host "Repo: $Repository" -ForegroundColor Green
Write-Host "Rama: $Branch" -ForegroundColor Green
Write-Host "Workers verificados: $($workers -join ', ')" -ForegroundColor Green
Write-Host "Este script no crea sesiones, manifests, branches, PRs, pushes, merges ni deploys." -ForegroundColor Green
