$ErrorActionPreference = "Stop"

$compose = Join-Path $PSScriptRoot "docker-compose.multinode.yml"
$repo = (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
$tool = Join-Path $repo "deploy/cometbft/ValidatorUpdateIntegrationTool/ValidatorUpdateIntegrationTool.csproj"
$previousValidatorKey = $env:VALIDATOR_PUBKEY_HEX
$previousTransactionKind = $env:TRANSACTION_KIND

function Get-Status([int]$port) {
    Invoke-RestMethod "http://localhost:$port/status"
}

function Wait-Height([long]$minimum, [int]$timeoutSeconds = 120, [int]$port = 26657) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        try {
            $status = Get-Status $port
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

function Get-VolumeName([string]$volume) {
    $expected = "cometbft_$volume"
    $name = docker volume inspect $expected --format '{{.Name}}' 2>$null | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($name)) {
        $name = docker volume ls -q --filter "label=com.docker.compose.volume=$volume" | Select-Object -First 1
    }
    if ([string]::IsNullOrWhiteSpace($name)) {
        throw "Could not find Compose volume '$volume'."
    }
    return $name.Trim()
}

function Invoke-VolumeShell([string]$volume, [string]$script) {
    docker run --rm --entrypoint /bin/sh --user "0:0" -v "${volume}:/network" cometbft/cometbft:v0.38.17 -c $script
    if ($LASTEXITCODE -ne 0) {
        throw "Volume operation failed with exit code $LASTEXITCODE."
    }
}

function New-RotationKey([string]$dataVolume) {
    $keyJson = docker run --rm --entrypoint /bin/sh `
        --user "0:0" `
        -e "CMTHOME=/network/rotation-key-home" `
        -v "${dataVolume}:/network" cometbft/cometbft:v0.38.17 `
        -c 'rm -rf /network/rotation-key-home && cometbft init >/dev/null 2>&1 && cat /network/rotation-key-home/config/priv_validator_key.json'
    $key = $keyJson | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($key.pub_key.value) -or [string]::IsNullOrWhiteSpace($key.priv_key.value)) {
        throw "CometBFT did not generate a complete rotation key."
    }
    return $key
}

function Broadcast-Transaction([string]$transactionHex, [string]$description) {
    $deadline = (Get-Date).AddSeconds(30)
    $lastError = "RPC did not return a response."
    do {
        try {
            $response = Invoke-RestMethod "http://localhost:26657/broadcast_tx_commit?tx=0x$transactionHex"
            $txResult = $response.result.tx_result
            if ($null -eq $txResult) {
                $txResult = $response.result.deliver_tx
            }
            if ([int]$response.result.check_tx.code -ne 0 -or [int]$txResult.code -ne 0) {
                throw "CometBFT rejected $description transaction: $($response | ConvertTo-Json -Compress)"
            }
            Write-Host "$description accepted at check_tx/deliver_tx: $($txResult.log)"
            return
        }
        catch {
            $lastError = $_.Exception.Message
            Start-Sleep -Seconds 2
        }
    } while ((Get-Date) -lt $deadline)

    throw "Could not broadcast $description transaction before the timeout: $lastError"
}

function New-TransactionHex([string]$validatorKeyBase64, [string]$kind) {
    $env:VALIDATOR_PUBKEY_HEX = Convert-BytesToHex ([Convert]::FromBase64String($validatorKeyBase64))
    $env:TRANSACTION_KIND = $kind
    $txHex = (dotnet run --project $tool --no-restore).Trim()
    if ($txHex -notmatch '^[0-9A-F]+$') {
        throw "The integration tool did not produce a hexadecimal KTX2 transaction."
    }
    return $txHex
}

Push-Location $repo
try {
    docker compose -f $compose down -v --remove-orphans
    docker compose -f $compose up --build -d
    $before = Wait-Height 2
    $dataVolume = Get-VolumeName "multinode-data"
    $validatorKeyBase64 = (Get-Status 26657).result.validator_info.pub_key.value
    $rotationKey = New-RotationKey $dataVolume
    $rotationKeyBase64 = $rotationKey.pub_key.value
    if ($validatorKeyBase64 -eq $rotationKeyBase64) {
        throw "Source and rotation validator keys must be different."
    }

    Broadcast-Transaction (New-TransactionHex $validatorKeyBase64 "Bond") "Bond"

    $after = Wait-Height ([long]$before.latest_block_height + 1)
    $bondValidators = (Invoke-RestMethod "http://localhost:26657/validators").result.validators
    $bonded = @($bondValidators | Where-Object {
        $_.pub_key.value -eq $validatorKeyBase64 -and [long]$_.voting_power -gt 0
    })
    if ($bonded.Count -eq 0) {
        throw "The bonded validator key was not present in CometBFT's validator set."
    }

    $rotateBefore = $after
    Broadcast-Transaction (New-TransactionHex $rotationKeyBase64 "RotateValidatorKey") "RotateValidatorKey"
    Invoke-VolumeShell $dataVolume 'cp /network/rotation-key-home/config/priv_validator_key.json /network/node0/config/priv_validator_key.json && chmod 644 /network/node0/config/priv_validator_key.json'
    docker compose -f $compose stop ukc-app-0 cometbft-0
    docker compose -f $compose rm -f ukc-app-0 cometbft-0
    docker compose -f $compose up -d ukc-app-0 cometbft-0
    $rotationCommitHeight = [long](Get-Status 26658).result.sync_info.latest_block_height
    $rotated = Wait-Height ($rotationCommitHeight + 3) 120 26658
    $rotatedValidators = (Invoke-RestMethod "http://localhost:26658/validators").result.validators
    $oldKeyActive = @($rotatedValidators | Where-Object {
        $_.pub_key.value -eq $validatorKeyBase64 -and [long]$_.voting_power -gt 0
    })
    $newKeyActive = @($rotatedValidators | Where-Object {
        $_.pub_key.value -eq $rotationKeyBase64 -and [long]$_.voting_power -gt 0
    })
    if ($oldKeyActive.Count -ne 0 -or $newKeyActive.Count -eq 0) {
        $summary = @($rotatedValidators | ForEach-Object {
            "$($_.pub_key.value):$($_.voting_power)"
        }) -join ", "
        throw "Validator key rotation did not remove the old key and activate the new key. old=$validatorKeyBase64 new=$rotationKeyBase64 validators=[$summary]"
    }

    Write-Host "Validator key rotation succeeded: height $($rotated.latest_block_height), old power 0, new power $($newKeyActive[0].voting_power)"
}
finally {
    $env:VALIDATOR_PUBKEY_HEX = $previousValidatorKey
    $env:TRANSACTION_KIND = $previousTransactionKind
    if ($env:KEEP_HARNESS -ne "1") {
        docker compose -f $compose down -v --remove-orphans
    }
    Pop-Location
}
