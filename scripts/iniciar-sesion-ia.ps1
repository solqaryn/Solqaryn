# PROJECT_SCOPE_LOCK=STRICT
# EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
# PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
# PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
# EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
# LOCAL_SKILL_COUNT=1
# Prohibido usar skills/documentacion/contexto fuera de SOLQARYN sin autorizacion explicita o allowlist ACTIVE.

[CmdletBinding()]
param(
    [switch]$NoFetch
)

$ErrorActionPreference = "Stop"
$ExpectedRepo = "solqaryn/Solqaryn"
$ExpectedBranch = "dev"
$ExpectedOrigins = @(
    "https://github.com/solqaryn/Solqaryn",
    "https://github.com/solqaryn/Solqaryn.git",
    "git@github.com:solqaryn/Solqaryn.git",
    "ssh://git@github.com/solqaryn/Solqaryn.git"
)

function Invoke-GitCapture {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Falló: git $($Arguments -join ' ')`n$($output | Out-String)"
    }

    return (($output | Out-String).Trim())
}

$originalLocation = Get-Location

try {
    $repoRoot = Invoke-GitCapture @("rev-parse", "--show-toplevel")
    Set-Location $repoRoot

    $originUrl = Invoke-GitCapture @("remote", "get-url", "origin")
    if ($ExpectedOrigins -notcontains $originUrl) {
        throw "PROJECT GUARD: origin '$originUrl' no corresponde a '$ExpectedRepo'. No continúes; podrías estar en otro proyecto."
    }

    $canonicalFiles = @(
        "AGENTS.md",
        "PROJECT_CONTEXT.md",
        "PROJECT_INDEX.md",
        "ARCHITECTURE.md",
        "TASKS.md",
        "CHANGELOG_AI.md"
    )

    foreach ($file in $canonicalFiles) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            throw "PROJECT GUARD: falta el archivo canónico '$file'."
        }
    }

    if (-not (Select-String -Path "PROJECT_CONTEXT.md" -SimpleMatch "PROJECT_ID: SOLQARYN" -Quiet)) {
        throw "PROJECT GUARD: PROJECT_CONTEXT.md no confirma PROJECT_ID=SOLQARYN."
    }
    if (-not (Select-String -Path "PROJECT_CONTEXT.md" -SimpleMatch $ExpectedRepo -Quiet)) {
        throw "PROJECT GUARD: PROJECT_CONTEXT.md no confirma '$ExpectedRepo'."
    }

    if (-not $NoFetch) {
        & git fetch origin --prune --quiet
        if ($LASTEXITCODE -ne 0) {
            throw "No fue posible actualizar referencias remotas con git fetch."
        }
    }

    $branch = Invoke-GitCapture @("branch", "--show-current")
    if ($branch -ne $ExpectedBranch) {
        throw "PROJECT GUARD: rama actual '$branch'. Debes estar en '$ExpectedBranch' antes de escribir."
    }

    $head = Invoke-GitCapture @("rev-parse", "--short=12", "HEAD")
    $originHead = Invoke-GitCapture @("rev-parse", "--short=12", "origin/dev")
    $divergenceRaw = Invoke-GitCapture @("rev-list", "--left-right", "--count", "HEAD...origin/dev")
    $parts = $divergenceRaw -split "\s+"
    $ahead = [int]$parts[0]
    $behind = [int]$parts[1]
    $status = Invoke-GitCapture @("status", "--porcelain")
    $dirty = -not [string]::IsNullOrWhiteSpace($status)

    Write-Host ""
    Write-Host "=== SOLQARYN / SESSION GATE ===" -ForegroundColor Cyan
    Write-Host "PROJECT_ID=SOLQARYN"
    Write-Host "REPOSITORY=$ExpectedRepo"
    Write-Host "BRANCH=$branch"
    Write-Host "HEAD=$head"
    Write-Host "ORIGIN_HEAD=$originHead"
    Write-Host "AHEAD=$ahead"
    Write-Host "BEHIND=$behind"
    Write-Host "DIRTY=$($dirty.ToString().ToLowerInvariant())"

    if ($behind -gt 0) {
        Write-Warning "El checkout está $behind commit(s) detrás de origin/dev. No empieces cambios nuevos hasta sincronizar."
    }
    if ($ahead -gt 0) {
        Write-Warning "Hay $ahead commit(s) locales aún no reflejados en origin/dev. Presérvalos y resuelve el handoff antes de una tarea nueva."
    }
    if ($dirty) {
        Write-Warning "Hay cambios locales sin commit. No los descartes. Determina si pertenecen a la tarea en recuperación antes de editar."
    }

    Write-Host ""
    Write-Host "Últimos commits:" -ForegroundColor DarkCyan
    & git log -3 --pretty=format:"%h %s"
    Write-Host ""
    Write-Host ""
    Write-Host "Lectura mínima: AGENTS.md -> PROJECT_CONTEXT.md -> TASKS.md -> última entrada de CHANGELOG_AI.md." -ForegroundColor Green
    Write-Host "No reindexes el repositorio: abre solo los archivos objetivo y dependencias directas."
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
finally {
    Set-Location $originalLocation
}
