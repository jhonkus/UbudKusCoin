$ErrorActionPreference = "Stop"

$compose = Join-Path $PSScriptRoot "docker-compose.multinode.yml"
$repo = (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
$tool = Join-Path $repo "deploy/cometbft/ValidatorUpdateIntegrationTool/ValidatorUpdateIntegrationTool.csproj"
$previousValidatorKey = $env:VALIDATOR_PUBKEY_HEX

function Get-Status([int]$port) {
    Invoke-RestMethod "http://localhost:$port/status"
}

function Wait-Height([long]$minimum, [int]$timeoutSeconds = 120) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        try {
            $status = Get-Status 26657
            $height = [long]$status.result.sync_info.latest_block_height
            if ($height -ge $minimum) {
                return $status.result.sync_info
            }
        }
        catch {
            # RPC can be unavailable during validator startup.
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "The source validator did not reach height $minimum before the timeout."
}

function Convert-BytesToHex([byte[]]$bytes) {
    ($bytes | ForEach-Object { $_.ToString("X2") }) -join ""
}

Push-Location $repo
try {
    docker compose -f $compose down -v --remove-orphans
    docker compose -f $compose up --build -d
    $before = Wait-Height 2
    $validatorKeyBase64 = (Get-Status 26657).result.validator_info.pub_key.value
    $env:VALIDATOR_PUBKEY_HEX = Convert-BytesToHex ([Convert]::FromBase64String($validatorKeyBase64))
    $txHex = (dotnet run --project $tool).Trim()
    if ($txHex -notmatch '^[0-9A-F]+$') {
        throw "The integration tool did not produce a hexadecimal KTX2 transaction."
    }

    $response = Invoke-RestMethod "http://localhost:26657/broadcast_tx_commit?tx=0x$txHex"
    $txResult = $response.result.tx_result
    if ($null -eq $txResult) {
        $txResult = $response.result.deliver_tx
    }
    if ([int]$response.result.check_tx.code -ne 0 -or [int]$txResult.code -ne 0) {
        throw "CometBFT rejected the staking transaction: $($response | ConvertTo-Json -Compress)"
    }

    $after = Wait-Height ([long]$before.latest_block_height + 1)
    $afterValidators = (Invoke-RestMethod "http://localhost:26657/validators").result.validators
    $bondKey = $validatorKeyBase64
    $bonded = @($afterValidators | Where-Object {
        $_.pub_key.value -eq $bondKey -and [long]$_.voting_power -gt 0
    })
    if ($bonded.Count -eq 0) {
        throw "The bonded validator key was not present in CometBFT's validator set."
    }

    Write-Host "Validator update succeeded: height $($after.latest_block_height), validators $($afterValidators.Count), bonded power $($bonded[0].voting_power)"
}
finally {
    $env:VALIDATOR_PUBKEY_HEX = $previousValidatorKey
    Pop-Location
}
