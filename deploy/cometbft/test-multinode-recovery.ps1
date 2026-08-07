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
            # Temporary offline during restart
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "The validators did not reconverge after restart/recovery before the timeout."
}

Push-Location (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
try {
    Write-Host "Setting up fresh 4-node network..."
    docker compose -f $compose down -v --remove-orphans
    docker compose -f $compose up --build -d
    
    Write-Host "Waiting for network convergence..."
    $info = Wait-Converged
    Write-Host "Network converged at height $($info.latest_block_height)"

    # Scenario 1: Kill Application only
    Write-Host "[RECOVERY TEST] Scenario 1: Kill ukc-app-1 (C# ABCI Process only)..."
    docker compose -f $compose stop ukc-app-1
    Start-Sleep -Seconds 5
    Write-Host "[RECOVERY TEST] Restarting ukc-app-1 (and ensuring cometbft-1 is running)..."
    docker compose -f $compose start ukc-app-1 cometbft-1
    $info1 = Wait-Converged
    Write-Host "Scenario 1 SUCCESS: Network converged at height $($info1.latest_block_height)"

    # Scenario 2: Kill CometBFT only
    Write-Host "[RECOVERY TEST] Scenario 2: Kill cometbft-1 process only..."
    docker compose -f $compose stop cometbft-1
    Start-Sleep -Seconds 5
    Write-Host "[RECOVERY TEST] Restarting cometbft-1 (and ensuring ukc-app-1 is running)..."
    docker compose -f $compose start cometbft-1 ukc-app-1
    $info2 = Wait-Converged
    Write-Host "Scenario 2 SUCCESS: Network converged at height $($info2.latest_block_height)"

    # Scenario 3: Kill both processes (cometbft-1 and ukc-app-1)
    Write-Host "[RECOVERY TEST] Scenario 3: Kill both cometbft-1 and ukc-app-1..."
    docker compose -f $compose stop cometbft-1 ukc-app-1
    Start-Sleep -Seconds 5
    Write-Host "[RECOVERY TEST] Restarting both..."
    docker compose -f $compose start ukc-app-1 cometbft-1
    $info3 = Wait-Converged
    Write-Host "Scenario 3 SUCCESS: Network converged at height $($info3.latest_block_height) with AppHash $($info3.latest_block_hash)"

    Write-Host "Recovery Test Succeeded."
}
finally {
    docker compose -f $compose down -v --remove-orphans
    Pop-Location
}
