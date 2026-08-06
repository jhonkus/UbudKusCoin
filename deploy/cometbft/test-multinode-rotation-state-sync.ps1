$ErrorActionPreference = "Stop"

$scriptRoot = $PSScriptRoot
$compose = Join-Path $scriptRoot "docker-compose.multinode.yml"
$previousKeepHarness = $env:KEEP_HARNESS
$env:KEEP_HARNESS = "1"

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "test-multinode-validator-update.ps1")
    if ($LASTEXITCODE -ne 0) {
        throw "The validator key rotation drill failed."
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "test-multinode-state-sync.ps1")
    if ($LASTEXITCODE -ne 0) {
        throw "The state-sync drill failed after validator key rotation."
    }

    $genesisKeys = @((Invoke-RestMethod "http://localhost:26658/genesis").result.genesis.validators | ForEach-Object {
        $_.pub_key.value
    })
    $validators = (Invoke-RestMethod "http://localhost:26658/validators").result.validators
    $activeRotationKey = @($validators | Where-Object {
        $_.pub_key.value -notin $genesisKeys -and [long]$_.voting_power -gt 0
    })
    if ($activeRotationKey.Count -eq 0) {
        throw "The rotated validator key was not active after state sync."
    }

    Write-Host "Rotation plus state-sync succeeded: new key $($activeRotationKey[0].pub_key.value) remains active at power $($activeRotationKey[0].voting_power)."
}
finally {
    if ($null -eq $previousKeepHarness) {
        Remove-Item Env:KEEP_HARNESS -ErrorAction SilentlyContinue
    }
    else {
        $env:KEEP_HARNESS = $previousKeepHarness
    }

    if ($previousKeepHarness -ne "1") {
        docker compose -f $compose down -v --remove-orphans
    }
}
