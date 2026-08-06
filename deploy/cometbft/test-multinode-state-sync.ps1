$ErrorActionPreference = "Stop"

$compose = Join-Path $PSScriptRoot "docker-compose.multinode.yml"
$repo = (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
$network = "ukc-multinode_default"
$cometImage = "cometbft/cometbft:v0.38.17"

function Get-Status([int]$port) {
    return Invoke-RestMethod "http://localhost:$port/status"
}

function Wait-Healthy([int]$timeoutSeconds = 120) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        try {
            $ports = @(26657, 26658, 26659, 26660)
            $statuses = @($ports | ForEach-Object { Get-Status $_ })
            $heights = @($statuses | ForEach-Object { [long]$_.result.sync_info.latest_block_height })
            $hashes = @($statuses | ForEach-Object { [string]$_.result.sync_info.latest_block_hash })
            if (($heights[0] -gt 0) -and (($heights | Sort-Object -Unique).Count -eq 1) -and (($hashes | Sort-Object -Unique).Count -eq 1)) {
                return [pscustomobject]@{ Height = $heights[0]; Hash = $hashes[0] }
            }
        }
        catch {
            # Validators can be temporarily unavailable during startup.
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "The four validators did not converge before the timeout."
}

function Get-VolumeName([string]$volume) {
    $expected = "ukc-multinode_$volume"
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
    docker run --rm --entrypoint /bin/sh --user "0:0" -v "${volume}:/network" $cometImage -c $script
    if ($LASTEXITCODE -ne 0) {
        throw "Volume operation failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repo
try {
    if ($env:KEEP_HARNESS -eq "1") {
        # The combined drill has already bootstrapped and rotated the source cluster.
        # Do not recreate those applications while validating recovery of node 1.
        docker compose -f $compose up -d --no-build
    }
    else {
        docker compose -f $compose down -v --remove-orphans
        docker compose -f $compose up --build -d
    }
    $before = Wait-Healthy
    # Keep enough finalized history when a recently rotated validator has not
    # yet been operationally switched to its new external signing key.
    $trustHeight = [Math]::Max(1, $before.Height - 5)
    $trustedBlock = Invoke-RestMethod "http://localhost:26657/block?height=$trustHeight"
    $trustHash = [string]$trustedBlock.result.block_id.hash
    if ([string]::IsNullOrWhiteSpace($trustHash)) {
        throw "Could not resolve a trusted block hash at height $trustHeight."
    }

    Write-Host "Source chain: height $($before.Height), trust height $trustHeight, hash $trustHash"

    docker stop ukc-multinode-cometbft-1-1 ukc-multinode-ukc-app-1-1 | Out-Null
    docker rm ukc-multinode-cometbft-1-1 ukc-multinode-ukc-app-1-1 | Out-Null

    $dbVolume = Get-VolumeName "ukc-db-1"
    $dataVolume = Get-VolumeName "multinode-data"
    docker volume rm $dbVolume | Out-Null
    $emptyValidatorState = "eyJoZWlnaHQiOiIwIiwicm91bmQiOi0xLCJzdGVwIjowfQ=="
    Invoke-VolumeShell $dataVolume "rm -rf /network/node1/data/* && mkdir -p /network/node1/data && echo $emptyValidatorState | base64 -d > /network/node1/data/priv_validator_state.json"

    $configure = Join-Path $repo "deploy/cometbft/configure-state-sync.sh"
    docker run --rm --entrypoint /bin/sh `
        -e "TRUST_HEIGHT=$trustHeight" `
        -e "TRUST_HASH=$trustHash" `
        -e "RPC_SERVERS=cometbft-0:26657,cometbft-2:26657" `
        -e "NODE_HOME=node1" `
        -v "${dataVolume}:/network" `
        -v "${repo}:/workspace:ro" `
        $cometImage `
        -c "sh /workspace/deploy/cometbft/configure-state-sync.sh"

    docker compose -f $compose up -d --no-deps ukc-app-1
    docker compose -f $compose up -d --no-deps cometbft-1
    $deadline = (Get-Date).AddSeconds(180)
    do {
        try {
            $source = Get-Status 26657
            $joining = Get-Status 26658
            $sourceHeight = [long]$source.result.sync_info.latest_block_height
            $joiningHeight = [long]$joining.result.sync_info.latest_block_height
            $sourceHash = [string]$source.result.sync_info.latest_block_hash
            $joiningHash = [string]$joining.result.sync_info.latest_block_hash
            $earliestHeight = [long]$joining.result.sync_info.earliest_block_height
            if (($earliestHeight -gt $trustHeight) -and ($joiningHeight -gt $trustHeight) -and ($joiningHeight -eq $sourceHeight) -and ($joiningHash -eq $sourceHash) -and (-not [bool]$joining.result.sync_info.catching_up)) {
                Write-Host "State sync succeeded: snapshot starts at $earliestHeight; all nodes converge at $joiningHeight with hash $joiningHash"
                return
            }
        }
        catch {
            # The joining node may not expose RPC until ABCI state sync starts.
        }
        Start-Sleep -Seconds 3
    } while ((Get-Date) -lt $deadline)

    throw "The joining validator did not converge through state sync before the timeout."
}
finally {
    Pop-Location
}
