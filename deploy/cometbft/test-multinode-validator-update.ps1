$ErrorActionPreference = "Stop"

$compose = Join-Path $PSScriptRoot "docker-compose.multinode.yml"
$repo = (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
$tool = Join-Path $repo "deploy/cometbft/ValidatorUpdateIntegrationTool/ValidatorUpdateIntegrationTool.csproj"
$previousValidatorKey = $env:VALIDATOR_PUBKEY_HEX
$previousTransactionKind = $env:TRANSACTION_KIND

# ── helpers ──────────────────────────────────────────────────────────────────

function Get-Status([int]$port) {
    Invoke-RestMethod "http://localhost:$port/status"
}

function Get-ConsensusHealth([int]$port) {
    Invoke-RestMethod "http://localhost:$port/health/consensus"
}

# Fix #2: Compare catching_up as boolean, not string cast.
function Wait-ClusterReady([int]$timeoutSeconds = 180) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    $rpcPorts = @(26657, 26658, 26659, 26660)
    $applicationPorts = @(5100, 5101, 5102, 5103)

    do {
        try {
            $statuses = @($rpcPorts | ForEach-Object { Get-Status $_ })
            $syncInfo = @($statuses | ForEach-Object { $_.result.sync_info })
            $heights = @($syncInfo | ForEach-Object { [long]$_.latest_block_height })
            $hashes  = @($syncInfo | ForEach-Object { $_.latest_block_hash })

            # Fix #2: use $_.catching_up -eq $true (boolean comparison)
            $isSynchronized =
                $heights.Count -eq $rpcPorts.Count -and
                ($heights | Select-Object -Unique).Count -eq 1 -and
                ($hashes  | Select-Object -Unique).Count -eq 1 -and
                ($syncInfo | Where-Object { $_.catching_up -eq $true }).Count -eq 0

            $health = @($applicationPorts | ForEach-Object { Get-ConsensusHealth $_ })
            $applicationsHealthy =
                $health.Count -eq $applicationPorts.Count -and
                ($health | Where-Object { -not [bool]$_.healthy }).Count -eq 0

            if ($isSynchronized -and $applicationsHealthy) {
                Write-Host "Cluster ready — height=$($heights[0]) hash=$($hashes[0])"
                return $syncInfo[0]
            }
        }
        catch {
            # Services start independently; wait until both CometBFT and the ABCI apps agree.
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "The four-node cluster did not become synchronized and application-healthy before the timeout."
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

# Fix #5: Accept both upper and lower-case hex (case-insensitive regex)
function New-TransactionHex([string]$validatorKeyBase64, [string]$kind) {
    $env:VALIDATOR_PUBKEY_HEX = Convert-BytesToHex ([Convert]::FromBase64String($validatorKeyBase64))
    $env:TRANSACTION_KIND = $kind
    $txHex = (dotnet run --project $tool --no-restore).Trim()
    if ($txHex -notmatch '(?i)^[0-9A-F]+$') {
        throw "The integration tool did not produce a hexadecimal KTX2 transaction. Got: $txHex"
    }
    return $txHex.ToUpperInvariant()
}

# Fix #1 + Fix #4: Verify all 4 nodes share identical latest_block_hash (AppHash divergence check)
function Assert-NoConsensusDivergence([string]$phase) {
    $rpcPorts = @(26657, 26658, 26659, 26660)
    $syncs = @($rpcPorts | ForEach-Object {
        try { (Get-Status $_).result.sync_info }
        catch { throw "Cannot reach RPC on port $_ during $phase divergence check." }
    })

    $heights = @($syncs | ForEach-Object { [long]$_.latest_block_height })
    $hashes  = @($syncs | ForEach-Object { $_.latest_block_hash })

    $uniqueHeights = ($heights | Select-Object -Unique).Count
    $uniqueHashes  = ($hashes  | Select-Object -Unique).Count

    # Log per-node state for the qualification report
    for ($i = 0; $i -lt $rpcPorts.Count; $i++) {
        Write-Host "  node$i port=$($rpcPorts[$i])  height=$($heights[$i])  hash=$($hashes[$i])"
    }

    if ($uniqueHeights -ne 1) {
        $detail = ($heights -join ", ")
        throw "[$phase] CONSENSUS DIVERGENCE: nodes have different heights: [$detail]"
    }

    if ($uniqueHashes -ne 1) {
        $detail = ($hashes -join ", ")
        throw "[$phase] CONSENSUS DIVERGENCE: nodes have different block hashes (AppHash split): [$detail]"
    }

    Write-Host "[$phase] All 4 nodes agree: height=$($heights[0]) hash=$($hashes[0]) — NO DIVERGENCE"
}

# ── main test ─────────────────────────────────────────────────────────────────

Push-Location $repo
try {
    docker compose -f $compose down -v --remove-orphans
    docker compose -f $compose up --build -d

    $before = Wait-ClusterReady
    Write-Host "Cluster ready: height=$($before.latest_block_height) hash=$($before.latest_block_hash)"

    # Verify no divergence at baseline
    Assert-NoConsensusDivergence "Baseline"

    $dataVolume = Get-VolumeName "multinode-data"
    $validatorKeyBase64 = (Get-Status 26657).result.validator_info.pub_key.value
    $rotationKey = New-RotationKey $dataVolume
    $rotationKeyBase64 = $rotationKey.pub_key.value

    if ($validatorKeyBase64 -eq $rotationKeyBase64) {
        throw "Source and rotation validator keys must be different."
    }

    # Bond the current validator key
    Broadcast-Transaction (New-TransactionHex $validatorKeyBase64 "Bond") "Bond"

    $after = Wait-Height ([long]$before.latest_block_height + 1)
    Assert-NoConsensusDivergence "After-Bond"

    $bondValidators = (Invoke-RestMethod "http://localhost:26657/validators").result.validators
    $bonded = @($bondValidators | Where-Object {
        $_.pub_key.value -eq $validatorKeyBase64 -and [long]$_.voting_power -gt 0
    })
    if ($bonded.Count -eq 0) {
        throw "The bonded validator key was not present in CometBFT's validator set."
    }
    Write-Host "Bond verified: key present in validator set with voting_power=$($bonded[0].voting_power)"

    # Rotate to the new key
    Broadcast-Transaction (New-TransactionHex $rotationKeyBase64 "RotateValidatorKey") "RotateValidatorKey"
    Invoke-VolumeShell $dataVolume 'cp /network/rotation-key-home/config/priv_validator_key.json /network/node0/config/priv_validator_key.json && chmod 644 /network/node0/config/priv_validator_key.json'

    docker compose -f $compose stop ukc-app-0 cometbft-0
    docker compose -f $compose rm -f ukc-app-0 cometbft-0
    docker compose -f $compose up -d ukc-app-0 cometbft-0

    $rotationCommitHeight = [long](Get-Status 26658).result.sync_info.latest_block_height
    $rotated = Wait-Height ($rotationCommitHeight + 3) 120 26658

    # Verify all nodes agree on chain state after rotation (Fix #1)
    Assert-NoConsensusDivergence "After-Rotation"

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

    Write-Host "Validator key rotation SUCCESS:"
    Write-Host "  height=$($rotated.latest_block_height)"
    Write-Host "  old_key_voting_power=0"
    Write-Host "  new_key_voting_power=$($newKeyActive[0].voting_power)"
}
finally {
    $env:VALIDATOR_PUBKEY_HEX = $previousValidatorKey
    $env:TRANSACTION_KIND = $previousTransactionKind
    if ($env:KEEP_HARNESS -ne "1") {
        docker compose -f $compose down -v --remove-orphans
    }
    Pop-Location
}
