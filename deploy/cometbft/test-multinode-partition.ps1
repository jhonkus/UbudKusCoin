$ErrorActionPreference = "Stop"

$compose = Join-Path $PSScriptRoot "docker-compose.multinode.yml"
$network = "ukc-multinode_default"

function Get-Status([int]$port) {
    return Invoke-RestMethod "http://localhost:$port/status"
}

function Wait-Synchronized([int]$timeoutSeconds = 60) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        try {
            $ports = @(26657, 26658, 26659, 26660)
            $statuses = @($ports | ForEach-Object { Get-Status $_ })
            $heights = @($statuses | ForEach-Object { [long]$_.result.sync_info.latest_block_height })
            $hashes = @($statuses | ForEach-Object { [string]$_.result.sync_info.latest_block_hash })
            if ($heights[0] -gt 0 -and ($heights | Sort-Object -Unique).Count -eq 1 -and ($hashes | Sort-Object -Unique).Count -eq 1) {
                return [pscustomobject]@{ Height = $heights[0]; Hash = $hashes[0] }
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
    docker compose -f $compose down --remove-orphans
    docker compose -f $compose up --build -d
    $before = Wait-Synchronized
    Write-Host "Before partition: height $($before.Height), hash $($before.Hash)"

    $validator1 = docker compose -f $compose ps -q cometbft-1
    if ([string]::IsNullOrWhiteSpace($validator1)) {
        throw "Could not resolve validator 1."
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
