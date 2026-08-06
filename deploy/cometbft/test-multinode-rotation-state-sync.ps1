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

    $rotationBytes = [byte[]]::new(32)
    for ($index = 0; $index -lt $rotationBytes.Length; $index++) {
        $rotationBytes[$index] = 0xA5
    }
    $rotationKey = [Convert]::ToBase64String($rotationBytes)
    $validators = (Invoke-RestMethod "http://localhost:26658/validators").result.validators
    $activeRotationKey = @($validators | Where-Object {
        $_.pub_key.value -eq $rotationKey -and [long]$_.voting_power -gt 0
    })
    if ($activeRotationKey.Count -eq 0) {
        throw "The rotated validator key was not active after state sync."
    }

    Write-Host "Rotation plus state-sync succeeded: rotated key remains active at power $($activeRotationKey[0].voting_power)."
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
