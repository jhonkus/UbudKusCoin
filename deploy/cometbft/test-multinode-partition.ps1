$ErrorActionPreference = "Stop"

$compose = Join-Path $PSScriptRoot "docker-compose.multinode.yml"
$network = "cometbft_default"

function Get-Status([int]$port) {
    return Invoke-RestMethod "http://localhost:$port/status"
}

function Wait-Synchronized([int]$timeoutSeconds = 60) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        try {
            $node0 = Get-Status 26657
            $node1 = Get-Status 26658
            $height0 = [long]$node0.result.sync_info.latest_block_height
            $height1 = [long]$node1.result.sync_info.latest_block_height
            $hash0 = [string]$node0.result.sync_info.latest_block_hash
            $hash1 = [string]$node1.result.sync_info.latest_block_hash
            if ($height0 -gt 0 -and $height0 -eq $height1 -and $hash0 -eq $hash1) {
                return [pscustomobject]@{ Height = $height0; Hash = $hash0 }
            }
        }
        catch {
            # Nodes can be temporarily unavailable while the partition heals.
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "The validators did not converge before the timeout."
}

Push-Location (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
try {
    docker compose -f $compose up --build -d
    $before = Wait-Synchronized
    Write-Host "Before partition: height $($before.Height), hash $($before.Hash)"

    $validator1 = docker compose -f $compose ps -q cometbft-1
    if ([string]::IsNullOrWhiteSpace($validator1)) {
        throw "Could not resolve the second CometBFT container."
    }

    docker network disconnect $network $validator1
    try {
        Start-Sleep -Seconds 8
        Write-Host "Partition injected: validator 1 disconnected from $network"
    }
    finally {
        docker network connect $network $validator1 2>$null
        Write-Host "Partition healed: validator 1 reconnected"
    }

    $after = Wait-Synchronized
    Write-Host "After recovery: height $($after.Height), hash $($after.Hash)"
}
finally {
    Pop-Location
}
