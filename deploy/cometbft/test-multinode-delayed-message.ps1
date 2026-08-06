$ErrorActionPreference = "Stop"

$compose = Join-Path $PSScriptRoot "docker-compose.multinode.yml"

function Get-Status([int]$port) {
    Invoke-RestMethod "http://localhost:$port/status"
}

function Wait-Converged([int]$timeoutSeconds = 120) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        try {
            $ports = 26657, 26658, 26659, 26660
            $statuses = @($ports | ForEach-Object { (Get-Status $_).result.sync_info })
            $heights = @($statuses | ForEach-Object { [long]$_.latest_block_height })
            $hashes = @($statuses | ForEach-Object { $_.latest_block_hash })
            if (($heights | Select-Object -Unique).Count -eq 1 -and ($hashes | Select-Object -Unique).Count -eq 1) {
                return $statuses[0]
            }
        }
        catch {
            # Nodes can be temporarily unavailable while the delayed messages drain.
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "The validators did not reconverge before the timeout."
}

Push-Location (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
try {
    docker compose -f $compose down -v --remove-orphans
    docker compose -f $compose up --build -d
    $before = Wait-Converged
    $validator1 = docker compose -f $compose ps -q cometbft-1
    $network = "cometbft_default"
    if ([string]::IsNullOrWhiteSpace($validator1)) {
        throw "Could not resolve validator 1."
    }

    docker network disconnect $network $validator1
    Start-Sleep -Seconds 20
    docker network connect $network $validator1
    $after = Wait-Converged

    if ([long]$after.latest_block_height -le [long]$before.latest_block_height) {
        throw "The source quorum did not finalize blocks during the partition."
    }

    Write-Host "Delayed-message recovery succeeded: source $($before.latest_block_height) -> $($after.latest_block_height), hash $($after.latest_block_hash)"
}
finally {
    docker compose -f $compose down -v --remove-orphans
    Pop-Location
}
