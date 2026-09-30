#requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$Once,
    [switch]$SelfTest,
    [switch]$ContractSelfTest,
    [int]$PollSeconds = 60,
    [string]$Repository = "solqaryn/Solqaryn",
    [string]$Branch = "dev"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$script:AntiGStatus = "RESERVED_INACTIVE"
$script:AntiGOperationalNow = $false
$script:AntiGScheduler = "DISABLED"
$script:AntiGHandoffProcessing = "DISABLED"
$script:AntiGAuthority = "MASTER"
$script:AntiGCanCertifyListoReal = $false
$script:AntiGFutureReincorporation = "EXPLICIT_AUTHORIZATION_REQUIRED"

function Write-AntiGReservedState {
    Write-Host "ANTIG_STATUS=$script:AntiGStatus"
    Write-Host "ANTIG_OPERATIONAL_NOW=FALSE"
    Write-Host "ANTIG_SCHEDULER=$script:AntiGScheduler"
    Write-Host "ANTIG_HANDOFF_PROCESSING=$script:AntiGHandoffProcessing"
    Write-Host "ANTIG_AUTHORITY=$script:AntiGAuthority"
    Write-Host "ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE"
    Write-Host "ANTIG_FUTURE_REINCORPORATION=$script:AntiGFutureReincorporation"
}

if ($Repository -ne "solqaryn/Solqaryn" -or $Branch -ne "dev") {
    throw "PROJECT GUARD: unauthorized repository/branch."
}

if ($SelfTest -or $ContractSelfTest) {
    if ($script:AntiGStatus -ne "RESERVED_INACTIVE") { throw "AntiG reserved-state self-test failed." }
    if ($script:AntiGOperationalNow) { throw "AntiG must not be operational." }
    if ($script:AntiGScheduler -ne "DISABLED") { throw "AntiG scheduler must be disabled." }
    if ($script:AntiGHandoffProcessing -ne "DISABLED") { throw "AntiG handoff processing must be disabled." }
    if ($script:AntiGCanCertifyListoReal) { throw "AntiG must not certify LISTO_REAL." }
    Write-AntiGReservedState
    Write-Host "ANTIG_RESERVED_INACTIVE_SELF_TEST=PASS"
    exit 0
}

Write-AntiGReservedState
Write-Host "ANTIG_NO_ACTION=RESERVED_INACTIVE"
exit 0
